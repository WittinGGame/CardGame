using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing definition of a card. Runtime copies are <see cref="CardInstance"/>.
    /// Gameplay behavior comes only from <see cref="Effects"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Card Battle/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField] private CardType cardType = CardType.Attack;

        [Header("Play")]
        [Tooltip("AP spent when the card is played successfully.")]
        [SerializeField] private int apCost = 1;
        [SerializeField] private CardTargetMode targetMode = CardTargetMode.None;

        [Header("Keywords")]
        [Tooltip("When true, this card remains in Hand instead of being discarded at the end of the player's turn.")]
        [SerializeField] private bool retain;
        [Tooltip("When true, playing this card sends it to the Exhaust pile instead of the Graveyard.")]
        [SerializeField] private bool exhaustAfterPlay;

        [Header("Effects")]
        [SerializeField] private CardEffectData[] effects;

        [Header("Visuals")]
        [SerializeField] private Sprite artwork;

        public string CardId => string.IsNullOrEmpty(cardId) ? name : cardId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public CardType CardType => cardType;
        public int ApCost => Mathf.Max(0, apCost);
        public CardTargetMode TargetMode => targetMode;
        public bool Retain => retain;
        public bool ExhaustAfterPlay => exhaustAfterPlay;
        public IReadOnlyList<CardEffectData> Effects => effects;
        public bool HasEffects => effects != null && effects.Length > 0;
        public Sprite Artwork => artwork;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (apCost < 0)
                apCost = 0;

            if (effects == null || effects.Length == 0)
            {
                Debug.LogWarning(
                    $"[CardData] '{name}' (CardId={CardId}) has no Effects assigned.",
                    this);
                return;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null)
                    return;
            }

            Debug.LogWarning(
                $"[CardData] '{name}' (CardId={CardId}) Effects array contains no non-null effect.",
                this);
        }
#endif
    }
}
