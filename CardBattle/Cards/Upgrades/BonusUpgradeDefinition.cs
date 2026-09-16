using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "BonusUpgrade", menuName = "Card Battle/Bonus Upgrade Definition")]
    public class BonusUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string bonusId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private CardEffectData[] effects;
        public string BonusId => bonusId;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<CardEffectData> Effects => effects;

        // Effects retain the base card's target context; this is not a retargeting system.
        public bool IsCompatibleWith(CardData card)
        {
            if (card == null || string.IsNullOrWhiteSpace(bonusId) || effects == null || effects.Length == 0)
                return false;
            bool enemyTarget = card.TargetMode == CardTargetMode.SingleEnemy || card.TargetMode == CardTargetMode.AllEnemies;
            foreach (var effect in effects)
            {
                if (effect == null) return false;
                if (effect is DealDamageEffectData && !enemyTarget) return false;
                if (effect is ApplyStatusEffectData status && !status.ForceApplyToPlayer)
                {
                    if (card.TargetMode == CardTargetMode.None) return false;
                    if ((status.StatusType == StatusEffectType.Weak || status.StatusType == StatusEffectType.Vulnerable) && !enemyTarget)
                        return false;
                }
            }
            return true;
        }
    }
}
