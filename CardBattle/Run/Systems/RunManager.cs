using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        public RunState CurrentRun { get; private set; }
        private bool applyingPendingUpgrade;

        public bool HasActiveRun =>
            CurrentRun != null &&
            CurrentRun.isActive;

        public bool IsPrimaryInstance => Instance == this;

        public event Action<RunState> OnRunStarted;
        public event Action<RunState> OnRunChanged;
        public event Action OnRunCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"Duplicate RunManager destroyed on {gameObject.scene.name}.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (CurrentRun == null)
                CurrentRun = new RunState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool StartNewRun(
            string runId,
            int seed,
            string playerClassId,
            int startingMaxHp,
            IEnumerable<RunCardRecord> starterDeck)
        {
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(playerClassId))
                return false;

            var run = new RunState();
            run.InitializeNewRun(runId, seed, playerClassId, startingMaxHp, starterDeck);
            CurrentRun = run;

            OnRunStarted?.Invoke(CurrentRun);
            NotifyRunChanged();
            return true;
        }

        public bool SetCurrentHp(int value)
        {
            if (!HasActiveRun)
                return false;

            int previousHp = CurrentRun.currentHp;
            CurrentRun.SetCurrentHp(value);

            if (CurrentRun.currentHp != previousHp)
                NotifyRunChanged();

            return true;
        }

        public bool SetMaxHp(int value, bool refillToMax = false)
        {
            if (!HasActiveRun)
                return false;

            int previousMaxHp = CurrentRun.maxHp;
            int previousCurrentHp = CurrentRun.currentHp;
            CurrentRun.SetMaxHp(value, refillToMax);

            if (CurrentRun.maxHp != previousMaxHp || CurrentRun.currentHp != previousCurrentHp)
                NotifyRunChanged();

            return true;
        }

        public bool AddGold(int amount)
        {
            if (!HasActiveRun || amount <= 0)
                return false;

            CurrentRun.AddGold(amount);
            NotifyRunChanged();
            return true;
        }

        public bool SpendGold(int amount)
        {
            if (!HasActiveRun)
                return false;

            if (amount == 0)
                return true;

            if (!CurrentRun.SpendGold(amount))
                return false;

            NotifyRunChanged();
            return true;
        }

        public bool AddCard(string cardId, int upgradeLevel = 0)
        {
            if (!HasActiveRun || string.IsNullOrWhiteSpace(cardId))
                return false;

            CurrentRun.AddCard(cardId, upgradeLevel);
            NotifyRunChanged();
            return true;
        }

        public bool AddCard(RunCardRecord record)
        {
            if (!HasActiveRun || record == null || string.IsNullOrWhiteSpace(record.cardId))
                return false;

            CurrentRun.AddCard(record);
            NotifyRunChanged();
            return true;
        }

        public bool RemoveCardAt(int index)
        {
            if (!HasActiveRun)
                return false;

            if (!CurrentRun.RemoveCardAt(index))
                return false;

            NotifyRunChanged();
            return true;
        }

        public bool ClearRun()
        {
            if (CurrentRun == null || IsRunAlreadyCleared())
                return false;

            CurrentRun.ClearRun();
            OnRunCleared?.Invoke();
            return true;
        }

        // Return a copy so readers do not mutate persistent ownership through this query.
        public bool TryGetCardSnapshot(string runCardInstanceId, out RunCardRecord card)
        {
            card = null;
            if (!HasActiveRun || string.IsNullOrWhiteSpace(runCardInstanceId) ||
                CurrentRun.currentDeck == null)
                return false;

            foreach (var record in CurrentRun.currentDeck)
            {
                if (record == null || record.runCardInstanceId != runCardInstanceId)
                    continue;
                if (card != null)
                {
                    card = null;
                    return false; // Ambiguous ownership must not resolve to an arbitrary copy.
                }
                card = record.Clone();
            }
            return card != null;
        }

        public bool TryCommitUpgradeOffers(string nodeId, string cardInstanceId,
            CardCatalog cards, CardUpgradeCatalog upgrades, System.Random random)
        {
            if (applyingPendingUpgrade || !HasActiveRun || string.IsNullOrWhiteSpace(nodeId) || cards == null ||
                !TryGetCardSnapshot(cardInstanceId, out var card) ||
                !cards.TryGetCard(card.cardId, out var baseCard)) return false;

            var existing = CurrentRun.pendingCardUpgrade;
            if (existing != null && existing.isCommitted)
                return existing.nodeId == nodeId && existing.runCardInstanceId == cardInstanceId &&
                    BonusUpgradeOfferGenerator.TryValidatePending(existing, card, baseCard, upgrades);

            if (!RunCardPersistenceValidation.TryValidate(CurrentRun, false, out _) ||
                card.upgradeLevel != 0 || !string.IsNullOrEmpty(card.selectedBonusUpgradeId) ||
                !BonusUpgradeOfferGenerator.TryGenerate(baseCard, upgrades, random, out var offers)) return false;

            var pending = new PendingCardUpgradeState
            {
                isCommitted = true, nodeId = nodeId, runCardInstanceId = cardInstanceId,
                offeredBonusUpgradeIds = offers
            };
            if (!BonusUpgradeOfferGenerator.TryValidatePending(pending, card, baseCard, upgrades)) return false;
            CurrentRun.pendingCardUpgrade = pending.Clone();
            NotifyRunChanged();
            return true;
        }

        // Bonfire supplies its existing map completion API. Publish run changes only after
        // the owned card, node, and pending record have reached a consistent final state.
        internal bool TryApplyPendingUpgrade(string nodeId, string bonusId, CardCatalog cards,
            CardUpgradeCatalog upgrades, Func<bool> completeRestNode, out RunCardRecord applied)
        {
            applied = null;
            if (applyingPendingUpgrade || !HasActiveRun || completeRestNode == null || cards == null) return false;
            var run = CurrentRun;
            var pending = run.pendingCardUpgrade;
            if (pending == null || pending.nodeId != nodeId ||
                !TryGetCardSnapshot(pending.runCardInstanceId, out var snapshot) ||
                !cards.TryGetCard(snapshot.cardId, out var data) ||
                !BonusUpgradeOfferGenerator.TryValidatePending(pending, snapshot, data, upgrades) ||
                !pending.offeredBonusUpgradeIds.Contains(bonusId)) return false;
            var record = run.currentDeck.Find(c => c != null && c.runCardInstanceId == pending.runCardInstanceId);
            applyingPendingUpgrade = true;
            try
            {
                record.upgradeLevel = 1;
                record.selectedBonusUpgradeId = bonusId;
                if (!completeRestNode())
                {
                    record.upgradeLevel = snapshot.upgradeLevel;
                    record.selectedBonusUpgradeId = snapshot.selectedBonusUpgradeId;
                    return false;
                }
                // Session replacement from a map callback must never change the new run.
                if (CurrentRun != run || !run.isActive || run.pendingCardUpgrade != pending) return false;
                run.pendingCardUpgrade = null;
                applied = record.Clone();
                NotifyRunChanged();
                return true;
            }
            finally { applyingPendingUpgrade = false; }
        }

        public PendingCardUpgradeState GetPendingCardUpgradeSnapshot() => CurrentRun?.pendingCardUpgrade?.Clone();

        public RunState GetSnapshot()
        {
            if (CurrentRun == null)
                return null;

            return CurrentRun.Clone();
        }

        public bool RestoreRun(RunState restoredRun)
        {
            if (restoredRun == null)
                return false;

            if (!RunCardPersistenceValidation.TryValidate(restoredRun, false, out string error))
            {
                Debug.LogWarning($"[RunManager] Restore rejected: {error}");
                return false;
            }

            CurrentRun = restoredRun.Clone();

            if (CurrentRun.isActive)
                OnRunStarted?.Invoke(CurrentRun);
            else
                NotifyRunChanged();

            Debug.Log(
                $"[RunManager] Restored run. Active={HasActiveRun} | Class={CurrentRun.playerClassId}");

            return true;
        }

        private void NotifyRunChanged()
        {
            if (CurrentRun == null)
                return;

            OnRunChanged?.Invoke(CurrentRun);
        }

        private bool IsRunAlreadyCleared()
        {
            if (CurrentRun == null)
                return true;

            if (CurrentRun.isActive)
                return false;

            if (!string.IsNullOrEmpty(CurrentRun.runId))
                return false;

            if (CurrentRun.runSeed != 0)
                return false;

            if (!string.IsNullOrEmpty(CurrentRun.playerClassId))
                return false;

            if (CurrentRun.currentHp != 0 || CurrentRun.maxHp != 0 || CurrentRun.gold != 0)
                return false;

            if (CurrentRun.pendingCardUpgrade != null)
                return false;

            if (CurrentRun.currentDeck == null)
                return true;

            return CurrentRun.currentDeck.Count == 0;
        }
    }
}
