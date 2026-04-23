using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing definition of a card. Runtime copies are <see cref="CardInstance"/>.
    /// Keep numeric hooks here; layer modifiers via <see cref="ICardModifier"/> on instances later.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Card Battle/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField] private CardType cardType = CardType.Attack;
        [Tooltip("AP spent when the card is played successfully.")]
        [SerializeField] private int apCost = 1;

        [Header("Attack")]
        [SerializeField] private int attackDamage = 3;

        [Header("Heal")]
        [SerializeField] private int healAmount = 2;

        [Header("Buff")]
        [Tooltip("Generic potency for buffs (e.g. extra damage on next attack, block, etc.). Wired in CardResolver / player hooks.")]
        [SerializeField] private int buffPotency = 1;

        [Header("Defend")]
        [SerializeField] private int blockAmount = 5;

        [Header("Effect System (Phase 1)")]
        [SerializeField] private CardTargetMode targetMode = CardTargetMode.None;
        [SerializeField] private CardEffectData[] effects;

        [Header("Visuals")]
        [SerializeField] private Sprite artwork;

        public string CardId => string.IsNullOrEmpty(cardId) ? name : cardId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public CardType CardType => cardType;
        public int ApCost => Mathf.Max(0, apCost);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public int HealAmount => Mathf.Max(0, healAmount);
        public int BuffPotency => buffPotency;
        public int BlockAmount => Mathf.Max(0, blockAmount);
        public CardTargetMode TargetMode => targetMode;
        public IReadOnlyList<CardEffectData> Effects => effects;
        public bool HasEffects => effects != null && effects.Length > 0;
        public Sprite Artwork => artwork;
    }
}
