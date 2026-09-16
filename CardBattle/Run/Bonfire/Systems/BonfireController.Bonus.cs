using System;

namespace CardBattle.Core
{
    public partial class BonfireController
    {
        public bool CanSelectBonus => IsActive && !isApplyingChoice && !nodeCompleted &&
            !upgradeSavePending && checkpointSaved && IsPendingUpgradeValid();
        public bool CanApplySelectedBonus => CanSelectBonus &&
            sessionRun.pendingCardUpgrade.offeredBonusUpgradeIds.Contains(selectedBonusId);

        public bool TryGetOfferedBonus(string id, out BonusUpgradeDefinition bonus)
        {
            bonus = null;
            return IsPendingUpgradeValid() && sessionRun.pendingCardUpgrade.offeredBonusUpgradeIds.Contains(id) &&
                cardUpgradeCatalog.TryGetBonus(id, out bonus);
        }

        public bool TrySelectBonus(string id)
        {
            if (!isActiveAndEnabled || !CanSelectBonus) return false;
            if (!TryGetOfferedBonus(id, out _)) return RejectUpgrade("Bonus is not a valid persisted offer.");
            selectedBonusId = id;
            lastError = string.Empty;
            OnStateChanged?.Invoke();
            return true;
        }

        // True means final choice applied. A failed final save is reported through CanRetrySave.
        public bool TryApplySelectedBonus()
        {
            if (!isActiveAndEnabled || !CanApplySelectedBonus) return false;
            string bonusId = selectedBonusId;
            isApplyingChoice = true;
            try
            {
                OnStateChanged?.Invoke();
                if (!IsPendingSessionValid() || !IsPendingUpgradeValid() ||
                    !sessionRun.pendingCardUpgrade.offeredBonusUpgradeIds.Contains(bonusId)) return false;
                if (!ResolveRunManager().TryApplyPendingUpgrade(sessionNodeId, bonusId, cardCatalog,
                    cardUpgradeCatalog, CompleteUpgradeNode, out var applied))
                    return RejectUpgrade("Upgrade could not be applied; check the locked card and persisted Bonus content.");
                if (!IsSessionValid()) return false;
                appliedUpgrade = applied;
                choiceCommitted = true;
                upgradeSavePending = false;
                SaveCurrentStage();
                return true;
            }
            finally { isApplyingChoice = false; RefreshState(); }
        }

        private bool CompleteUpgradeNode()
        {
            if (!IsPendingSessionValid() || !mapRuntimeController.TryCompleteSelectedNode()) return false;
            nodeCompleted = true;
            return IsSessionValid();
        }

        public string GetAppliedUpgradeDescription()
        {
            return HasAppliedUpgrade && cardCatalog.TryGetCard(appliedUpgrade.cardId, out var data) &&
                RunCardResolver.TryResolve(appliedUpgrade, data, cardUpgradeCatalog, out var card)
                ? CardDescriptionBuilder.BuildForInstance(card) : string.Empty;
        }
    }
}
