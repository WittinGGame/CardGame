using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "DrawCardsEffect", menuName = "Card Battle/Effects/Draw Cards")]
    public class DrawCardsEffectData : CardEffectData
    {
        [SerializeField] private int amount = 2;

        public int Amount => Mathf.Max(0, amount);

        public override CardEffectExecutionKind ExecutionKind => CardEffectExecutionKind.DrawPresentation;

        public override string GetDescriptionText()
        {
            int value = Amount;
            if (value <= 0)
                return string.Empty;

            if (value == 1)
                return "Draw 1 card.";

            return $"Draw {value} cards.";
        }

        /// <summary>
        /// Legacy/debug sync path only. Production sequential execution draws via presentation.
        /// </summary>
        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (executionContext == null)
                return;

            executionContext.RequestDraw(Amount);
        }
    }
}
