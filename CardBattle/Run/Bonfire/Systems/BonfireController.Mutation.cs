using System;
using UnityEngine;

namespace CardBattle.Core
{
    // Immutable presentation result, also retained throughout an unsuccessful final save.
    public sealed class UpgradeResolution
    {
        public string RunCardInstanceId { get; }
        public string MutationId { get; }
        public float Chance { get; }
        public bool MutationTriggered => !string.IsNullOrEmpty(MutationId);
        public UpgradeResolution(string cardId, string mutationId, float chance)
        { RunCardInstanceId = cardId; MutationId = mutationId ?? string.Empty; Chance = chance; }
    }

    public partial class BonfireController
    {
        [SerializeField, Range(0f, 1f)] private float baseMutationChance = .35f;
        private UpgradeResolution resolvedUpgrade;
        public UpgradeResolution ResolvedUpgrade => resolvedUpgrade;
        public event Action<UpgradeResolution> OnUpgradeResolved;
        public event Action<UpgradeResolution> OnUpgradeCompleted;

        // Future location/character providers may supply an additive modifier here.
        // No modifier system or special Bonfire is activated by this revision.
        protected virtual float GetAdditionalMutationChance() => 0f;
        public float MutationChance
        {
            get
            {
                if (!TryGetUpgradeCardData(selectedRunCardInstanceId, out var data, out _) ||
                    BonusUpgradeOfferGenerator.GetEligibleBonusIds(data, cardUpgradeCatalog).Count == 0) return 0f;
                float chance = baseMutationChance + GetAdditionalMutationChance();
                return float.IsNaN(chance) ? 0f : Mathf.Clamp01(chance);
            }
        }

        public bool CanSelectMutation => IsWaitingForMutationChoice && !isApplyingChoice;

        public bool TryGetOfferedMutation(string id, out BonusUpgradeDefinition mutation)
        {
            mutation = null;
            return IsWaitingForMutationChoice &&
                sessionRun.pendingCardUpgrade.offeredBonusUpgradeIds.Contains(id) &&
                cardUpgradeCatalog.TryGetBonus(id, out mutation);
        }

        public bool TryResolveOfferedMutation(string id, out CardData data,
            out BonusUpgradeDefinition mutation, out CardInstance resolved)
        {
            data = null;
            mutation = null;
            resolved = null;
            if (!TryGetOfferedMutation(id, out mutation)) return false;
            var pending = sessionRun.pendingCardUpgrade;
            if (!ResolveRunManager().TryGetCardSnapshot(pending.runCardInstanceId, out var record) ||
                !cardCatalog.TryGetCard(record.cardId, out data)) return false;
            record.upgradeLevel = 1;
            record.selectedBonusUpgradeId = id;
            return RunCardResolver.TryResolve(record, data, cardUpgradeCatalog, out resolved);
        }

        public bool TryConfirmUpgradeCard()
        {
            if (!isActiveAndEnabled || !CanConfirmUpgradeCard || resolvedUpgrade != null) return false;
            isApplyingChoice = true;
            try
            {
                // All validation is before the roll. Preview/Back/Retry never enter here.
                if (!IsPendingSessionValid() || !IsEligibleUpgradeCard(selectedRunCardInstanceId) ||
                    !TryGetUpgradeCardData(selectedRunCardInstanceId, out var data, out _)) return false;
                string ownedId = selectedRunCardInstanceId;
                var pool = BonusUpgradeOfferGenerator.GetEligibleBonusIds(data, cardUpgradeCatalog);
                float chance = MutationChance;
                double roll = upgradeRandom.NextDouble(); // Exactly one chance roll per confirmed Upgrade.
                choiceCommitted = true;
                upgradeSelecting = false;

                if (pool.Count > 0 && roll < chance)
                {
                    if (!ResolveRunManager().TryCommitMutationOffers(sessionNodeId, ownedId, cardCatalog,
                        cardUpgradeCatalog, upgradeRandom, chance))
                        return RejectUpgrade("Mutation triggered but its offers could not be committed. The roll is locked and was not repeated.");
                    if (!IsPendingSessionValid()) return false;
                    selectedRunCardInstanceId = string.Empty;
                    upgradeSavePending = true;
                    SaveCurrentStage();
                    return true;
                }

                var result = new UpgradeResolution(ownedId, string.Empty, chance);
                resolvedUpgrade = result; // Freeze before any callback or save.
                if (!ResolveRunManager().TryApplyResolvedUpgrade(ownedId, string.Empty, cardCatalog,
                    cardUpgradeCatalog, CompleteUpgradeNode, out var applied))
                    return RejectUpgrade("Guaranteed Upgrade could not be applied. Result is locked; no Mutation was rerolled.");
                if (!IsSessionValid()) return false;
                appliedUpgrade = applied;
                selectedRunCardInstanceId = string.Empty;
                PublishUpgrade(OnUpgradeResolved, result);
                SaveCurrentStage();
                return true;
            }
            finally { isApplyingChoice = false; RefreshState(); }
        }

        // Selecting a frozen offer is the final transaction boundary. There is no second Confirm.
        public bool TrySelectMutation(string id)
        {
            if (!isActiveAndEnabled || !CanSelectMutation || !TryGetOfferedMutation(id, out _))
                return false;

            isApplyingChoice = true;
            try
            {
                OnStateChanged?.Invoke();
                if (!IsPendingSessionValid() || !IsPendingUpgradeValid() ||
                    !sessionRun.pendingCardUpgrade.mutationTriggered ||
                    !sessionRun.pendingCardUpgrade.offeredBonusUpgradeIds.Contains(id) ||
                    !cardUpgradeCatalog.TryGetBonus(id, out _))
                    return RejectUpgrade("Mutation is not a valid frozen offer for this Upgrade.");

                string ownedId = sessionRun.pendingCardUpgrade.runCardInstanceId;
                float chance = Mathf.Clamp01(sessionRun.pendingCardUpgrade.mutationChance);
                if (!ResolveRunManager().TryApplyPendingUpgrade(sessionNodeId, id, cardCatalog,
                    cardUpgradeCatalog, CompleteUpgradeNode, out var applied))
                    return RejectUpgrade("Mutation Upgrade could not be applied; the frozen choice remains pending.");
                if (!IsSessionValid()) return false;

                var result = new UpgradeResolution(ownedId, id, chance);
                resolvedUpgrade = result;
                appliedUpgrade = applied;
                selectedBonusId = id;
                choiceCommitted = true;
                upgradeSavePending = false;
                PublishUpgrade(OnUpgradeResolved, result);
                SaveCurrentStage();
                return true;
            }
            finally { isApplyingChoice = false; RefreshState(); }
        }

        private void PublishUpgrade(Action<UpgradeResolution> handlers, UpgradeResolution result)
        {
            if (handlers == null) return;
            foreach (Action<UpgradeResolution> handler in handlers.GetInvocationList())
                try { handler(result); } catch (Exception error) { Debug.LogException(error); }
        }

        public bool TryResolveUpgradeCard(string id, out CardInstance card)
        {
            card = null;
            return IsActive && ResolveRunManager().TryGetCardSnapshot(id, out var record) &&
                cardCatalog != null && cardCatalog.TryGetCard(record.cardId, out var data) &&
                RunCardResolver.TryResolve(record, data, cardUpgradeCatalog, out card);
        }

        public int? GetAppliedUpgradeApCost()
        {
            return HasAppliedUpgrade && cardCatalog.TryGetCard(appliedUpgrade.cardId, out var data) &&
                RunCardResolver.TryResolve(appliedUpgrade, data, cardUpgradeCatalog, out var card)
                ? card.EffectiveApCost : (int?)null;
        }
    }
}
