using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public static class RunCardResolver
    {
        public static bool TryResolve(RunCardRecord record, CardData baseCard,
            CardUpgradeCatalog upgradeCatalog, out CardInstance instance)
        {
            instance = null;
            if (record == null || baseCard == null || record.cardId != baseCard.CardId)
            {
                Debug.LogError("[RunCardResolver] Missing or mismatched base card.");
                return false;
            }
            if (record.upgradeLevel == 0)
            {
                instance = new CardInstance(baseCard, runCardInstanceId: record.runCardInstanceId);
                return true;
            }
            if (record.upgradeLevel != 1 || upgradeCatalog == null ||
                !upgradeCatalog.TryGetUpgrade(baseCard, out var definition) || !definition.HasValidSequence)
            {
                Debug.LogError($"[RunCardResolver] Cannot resolve '{record.cardId}' " +
                    $"(owned={record.runCardInstanceId}, level={record.upgradeLevel}): " +
                    "requires a unique, valid Level 1 Upgrade definition. Battle deck creation aborted.");
                return false;
            }
            var effects = new List<CardEffectData>(definition.GuaranteedEffects);
            int? effectiveApCost = null;
            if (!string.IsNullOrEmpty(record.selectedBonusUpgradeId))
            {
                if (!upgradeCatalog.TryGetBonus(record.selectedBonusUpgradeId, out var bonus) || !bonus.IsCompatibleWith(baseCard))
                {
                    Debug.LogError($"[RunCardResolver] Unresolvable/incompatible saved Bonus '{record.selectedBonusUpgradeId}' on '{record.cardId}'.");
                    return false;
                }
                if (bonus.Effects != null) effects.AddRange(bonus.Effects);
                if (bonus.OverrideApCost) effectiveApCost = bonus.ApCostOverride;
            }
            instance = new CardInstance(baseCard, effectiveEffects: effects,
                runCardInstanceId: record.runCardInstanceId, effectiveApCost: effectiveApCost);
            return true;
        }
    }
}
