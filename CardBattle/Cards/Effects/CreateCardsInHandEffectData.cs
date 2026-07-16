using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "CreateCardsInHandEffect",
        menuName = "Card Battle/Effects/Create Cards In Hand")]
    public class CreateCardsInHandEffectData : CardEffectData
    {
        [Tooltip("Card definition used to create new runtime CardInstances.")]
        [SerializeField] private CardData cardToCreate;

        [Min(0)]
        [SerializeField] private int amount = 1;

        public CardData CardToCreate => cardToCreate;
        public int Amount => Mathf.Max(0, amount);

        public override string GetDescriptionText()
        {
            int value = Amount;
            if (cardToCreate == null || value <= 0)
                return string.Empty;

            if (value == 1)
                return $"Create 1 {cardToCreate.DisplayName} in your hand.";

            return $"Create {value} {cardToCreate.DisplayName} cards in your hand.";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player?.DeckController == null || cardToCreate == null)
                return;

            int value = Amount;
            if (value <= 0)
                return;

            context.Player.DeckController.CreateCardsInHand(cardToCreate, value);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cardToCreate == null)
            {
                Debug.LogWarning(
                    $"[CreateCardsInHandEffect] '{name}' is missing Card To Create.",
                    this);
            }

            if (amount <= 0)
            {
                Debug.LogWarning(
                    $"[CreateCardsInHandEffect] '{name}' Amount should be greater than 0.",
                    this);
            }
        }
#endif
    }
}
