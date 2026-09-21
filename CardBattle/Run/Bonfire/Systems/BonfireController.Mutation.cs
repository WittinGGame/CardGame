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
                string mutation = pool.Count > 0 && roll < chance
                    ? pool[pool.Count == 1 ? 0 : upgradeRandom.Next(pool.Count)] : string.Empty;
                var result = new UpgradeResolution(ownedId, mutation, chance);
                resolvedUpgrade = result; // Freeze before any callback or save.
                choiceCommitted = true;
                upgradeSelecting = false;
                if (!ResolveRunManager().TryApplyResolvedUpgrade(ownedId, mutation, cardCatalog,
                    cardUpgradeCatalog, CompleteUpgradeNode, out var applied))
                    return RejectUpgrade("Resolved Upgrade could not be applied. Result is locked; no Mutation was rerolled.");
                if (!IsSessionValid()) return false;
                appliedUpgrade = applied;
                selectedRunCardInstanceId = string.Empty;
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
