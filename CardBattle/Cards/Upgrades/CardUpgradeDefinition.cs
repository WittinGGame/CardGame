using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "CardUpgrade", menuName = "Card Battle/Card Upgrade Definition")]
    public class CardUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private CardData baseCard;
        [SerializeField] private CardEffectData[] guaranteedEffects;
        [SerializeField] private BonusUpgradeDefinition[] bonusPool;
        public IReadOnlyList<BonusUpgradeDefinition> BonusPool => bonusPool;

        public CardData BaseCard => baseCard;
        public IReadOnlyList<CardEffectData> GuaranteedEffects => guaranteedEffects;

        public bool HasValidSequence
        {
            get
            {
                if (baseCard == null || guaranteedEffects == null || guaranteedEffects.Length == 0)
                    return false;
                foreach (var effect in guaranteedEffects)
                    if (effect == null) return false;
                return true;
            }
        }
    }
}
