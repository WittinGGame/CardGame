using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "CardTypeBadgeSet", menuName = "Card Battle/Visuals/Card Type Badge Set")]
    public class CardTypeBadgeSet : ScriptableObject
    {
        [Header("Card Type Badges")]
        [SerializeField] private Sprite attackBadge;
        [SerializeField] private Sprite defendBadge;
        [SerializeField] private Sprite healBadge;
        [SerializeField] private Sprite buffBadge;

        public Sprite GetBadge(CardType type)
        {
            switch (type)
            {
                case CardType.Attack:
                    return attackBadge;

                case CardType.Defend:
                    return defendBadge;

                case CardType.Heal:
                    return healBadge;

                case CardType.Buff:
                    return buffBadge;

                default:
                    return null;
            }
        }
    }
}