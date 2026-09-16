using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public partial class BonfireController
    {
        public enum SessionState { Inactive, Choice, UpgradeCardSelection, UpgradeCommitted, CompletedButUnsaved, InvalidUpgrade }
        [SerializeField] private CardCatalog cardCatalog;
        [SerializeField] private CardUpgradeCatalog cardUpgradeCatalog;
        private readonly System.Random upgradeRandom = new System.Random();
        private string selectedBonusId = string.Empty;
        private RunCardRecord appliedUpgrade;
        public bool HasAppliedUpgrade => IsActive && appliedUpgrade != null;
        public string SelectedBonusId => IsActive ? selectedBonusId : string.Empty;
        private bool upgradeSelecting;
        private bool upgradeSavePending;
        private string selectedRunCardInstanceId = string.Empty;
        private bool HasPendingUpgrade => sessionRun?.pendingCardUpgrade?.isCommitted == true;

        public SessionState State => !IsActive ? SessionState.Inactive :
            HasPendingUpgrade ? (IsPendingUpgradeValid() ? SessionState.UpgradeCommitted : SessionState.InvalidUpgrade) :
            nodeCompleted ? SessionState.CompletedButUnsaved :
            upgradeSelecting ? SessionState.UpgradeCardSelection : SessionState.Choice;
        public string SelectedRunCardInstanceId => IsActive ? selectedRunCardInstanceId : string.Empty;
        public bool CanBeginUpgrade => IsActive && checkpointSaved && !isApplyingChoice && !choiceCommitted &&
            !upgradeSelecting && !HasPendingUpgrade && GetEligibleUpgradeCards().Count > 0;
        public bool CanBackFromUpgrade => IsActive && upgradeSelecting && !choiceCommitted && !HasPendingUpgrade && !isApplyingChoice;
        public bool CanConfirmUpgradeCard => CanBackFromUpgrade && IsEligibleUpgradeCard(selectedRunCardInstanceId);

        public IReadOnlyList<RunCardRecord> GetEligibleUpgradeCards()
        {
            var result = new List<RunCardRecord>();
            if (!IsActive || choiceCommitted || HasPendingUpgrade || cardCatalog == null || cardUpgradeCatalog == null || sessionRun.currentDeck == null)
                return result;
            foreach (var card in sessionRun.currentDeck)
                if (card != null && IsEligibleUpgradeCard(card.runCardInstanceId)) result.Add(card.Clone());
            return result.AsReadOnly();
        }

        private bool IsEligibleUpgradeCard(string id)
        {
            var manager = ResolveRunManager();
            return IsPendingSessionValid() && manager != null && cardCatalog != null && cardUpgradeCatalog != null &&
                manager.TryGetCardSnapshot(id, out var card) && card.upgradeLevel == 0 && string.IsNullOrEmpty(card.selectedBonusUpgradeId) &&
                cardCatalog.TryGetCard(card.cardId, out var data) &&
                BonusUpgradeOfferGenerator.GetEligibleBonusIds(data, cardUpgradeCatalog).Count > 0;
        }

        public IReadOnlyList<RunCardRecord> GetRunDeckSnapshot()
        {
            var cards = new List<RunCardRecord>();
            if (IsActive && sessionRun.currentDeck != null)
                foreach (var card in sessionRun.currentDeck) cards.Add(card?.Clone());
            return cards.AsReadOnly();
        }

        public bool TryGetUpgradeCardData(string id, out CardData data, out CardUpgradeDefinition upgrade)
        {
            data = null;
            upgrade = null;
            if (!IsActive || cardCatalog == null || !ResolveRunManager().TryGetCardSnapshot(id, out var card) ||
                !cardCatalog.TryGetCard(card.cardId, out data)) return false;
            if (cardUpgradeCatalog != null) cardUpgradeCatalog.TryGetUpgrade(data, out upgrade);
            return true;
        }

        public bool TryBeginUpgrade()
        {
            if (!isActiveAndEnabled || !CanBeginUpgrade) return false;
            upgradeSelecting = true;
            selectedRunCardInstanceId = string.Empty;
            lastError = string.Empty;
            OnStateChanged?.Invoke();
            return true;
        }

        public bool TrySelectUpgradeCard(string id)
        {
            if (!isActiveAndEnabled || !CanBackFromUpgrade) return false;
            if (!IsEligibleUpgradeCard(id)) return RejectUpgrade("Selected card is no longer eligible for Upgrade.");
            selectedRunCardInstanceId = id;
            lastError = string.Empty;
            OnStateChanged?.Invoke();
            return true;
        }

        public bool TryBackFromUpgrade()
        {
            if (!isActiveAndEnabled || !CanBackFromUpgrade) return false;
            upgradeSelecting = false;
            selectedRunCardInstanceId = string.Empty;
            lastError = string.Empty;
            OnStateChanged?.Invoke();
            return true;
        }

        // True means commitment accepted; CanRetrySave/LastError separately report persistence failure.
        public bool TryConfirmUpgradeCard()
        {
            if (!isActiveAndEnabled || !CanBackFromUpgrade) return false;
            if (!IsEligibleUpgradeCard(selectedRunCardInstanceId))
                return RejectUpgrade("Cannot confirm: card ownership, level, Upgrade definition or Bonus pool changed.");
            isApplyingChoice = true;
            try
            {
                OnStateChanged?.Invoke();
                if (!IsPendingSessionValid() || !IsEligibleUpgradeCard(selectedRunCardInstanceId)) return false;
                var manager = ResolveRunManager();
                if (!manager.TryCommitUpgradeOffers(sessionNodeId, selectedRunCardInstanceId, cardCatalog, cardUpgradeCatalog, upgradeRandom))
                    return RejectUpgrade("Unable to commit this Upgrade card. No replacement card or offers were chosen.");
                // Run notifications may tear down/replace the session. Never save a different run/node.
                if (!IsPendingSessionValid()) return false;
                choiceCommitted = true;
                upgradeSelecting = false;
                selectedRunCardInstanceId = string.Empty;
                upgradeSavePending = true;
                SaveCurrentStage();
                return true;
            }
            finally
            {
                isApplyingChoice = false;
                RefreshState();
            }
        }

        public RunCardRecord GetCommittedUpgradeCardSnapshot()
        {
            if (HasAppliedUpgrade) return appliedUpgrade.Clone();
            return IsPendingUpgradeValid() && ResolveRunManager().TryGetCardSnapshot(sessionRun.pendingCardUpgrade.runCardInstanceId, out var card)
                ? card : null;
        }

        public IReadOnlyList<string> GetOfferedBonusIds()
        {
            return IsPendingUpgradeValid()
                ? new List<string>(sessionRun.pendingCardUpgrade.offeredBonusUpgradeIds).AsReadOnly()
                : new List<string>().AsReadOnly();
        }

        private bool IsPendingUpgradeValid()
        {
            var pending = sessionRun?.pendingCardUpgrade;
            return IsPendingSessionValid() && pending != null && pending.isCommitted && pending.nodeId == sessionNodeId &&
                cardCatalog != null && ResolveRunManager().TryGetCardSnapshot(pending.runCardInstanceId, out var card) &&
                cardCatalog.TryGetCard(card.cardId, out var data) &&
                BonusUpgradeOfferGenerator.TryValidatePending(pending, card, data, cardUpgradeCatalog);
        }

        private bool RejectUpgrade(string message)
        {
            bool changed = lastError != message;
            lastError = message;
            if (changed)
            {
                Debug.LogWarning("[Bonfire] " + message);
                OnStateChanged?.Invoke();
            }
            return false;
        }

        private void ClearTransientUpgrade()
        {
            selectedBonusId = string.Empty;
            appliedUpgrade = null;
            upgradeSelecting = false;
            upgradeSavePending = false;
            selectedRunCardInstanceId = string.Empty;
        }
    }
}
