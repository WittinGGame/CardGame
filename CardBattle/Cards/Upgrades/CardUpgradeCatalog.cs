using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "CardUpgradeCatalog", menuName = "Card Battle/Card Upgrade Catalog")]
    public class CardUpgradeCatalog : ScriptableObject
    {
        [SerializeField] private List<CardUpgradeDefinition> upgrades = new List<CardUpgradeDefinition>();

        // Stable registry for saved IDs. Do not remove entries merely because a pool changes.
        [SerializeField] private List<BonusUpgradeDefinition> bonuses = new List<BonusUpgradeDefinition>();

        public bool TryGetBonus(string bonusId, out BonusUpgradeDefinition bonus)
        {
            bonus = null;
            if (string.IsNullOrWhiteSpace(bonusId) || bonuses == null) return false;
            foreach (var entry in bonuses)
            {
                if (entry == null || !string.Equals(entry.BonusId, bonusId, StringComparison.Ordinal)) continue;
                if (bonus != null)
                {
                    Debug.LogError($"[CardUpgradeCatalog] Duplicate Bonus ID '{bonusId}'.");
                    bonus = null;
                    return false;
                }
                bonus = entry;
            }
            return bonus != null;
        }

        // Small authored collection: scan on demand to avoid stale Inspector/cache entries.
        public bool TryGetUpgrade(CardData baseCard, out CardUpgradeDefinition definition)
        {
            definition = null;
            if (baseCard == null || upgrades == null) return false;
            foreach (var entry in upgrades)
            {
                if (entry == null || entry.BaseCard == null ||
                    !string.Equals(entry.BaseCard.CardId, baseCard.CardId, StringComparison.Ordinal))
                    continue;
                if (definition != null)
                {
                    Debug.LogError($"[CardUpgradeCatalog] Duplicate definitions for '{baseCard.CardId}'.");
                    definition = null;
                    return false;
                }
                definition = entry;
            }
            // IDs locate the entry; the authored reference must match the catalog's base asset.
            if (definition != null && definition.BaseCard != baseCard)
            {
                Debug.LogError($"[CardUpgradeCatalog] Base asset mismatch for '{baseCard.CardId}'.");
                definition = null;
            }
            return definition != null;
        }
    }
}
