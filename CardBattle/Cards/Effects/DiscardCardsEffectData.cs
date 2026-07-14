using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Manual discard selection effect. Mutation and player input are owned by the
    /// sequential effect runner / hand selection controller — not by Apply.
    /// </summary>
    [CreateAssetMenu(fileName = "DiscardCardsEffect", menuName = "Card Battle/Effects/Discard Cards")]
    public class DiscardCardsEffectData : CardEffectData
    {
        [SerializeField] private int amount = 1;

        public int Amount => Mathf.Max(0, amount);

        public override CardEffectExecutionKind ExecutionKind => CardEffectExecutionKind.ManualHandSelection;

        public override string GetDescriptionText()
        {
            int value = Amount;
            if (value <= 0)
                return string.Empty;

            if (value == 1)
                return "Discard 1 card.";

            return $"Discard {value} cards.";
        }

        /// <summary>
        /// Intentionally empty. Manual discard requires player input and is executed
        /// by <see cref="CardEffectSequenceRunner"/>.
        /// </summary>
        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
        }
    }
}
