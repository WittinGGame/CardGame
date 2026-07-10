using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "DrawCardsEffect", menuName = "Card Battle/Effects/Draw Cards")]
    public class DrawCardsEffectData : CardEffectData
    {
        [SerializeField] private int amount = 2;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, amount);
            if (value == 1)
                return "Draw 1 card";

            return $"Draw {value} cards";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (executionContext == null)
                return;

            executionContext.RequestDraw(Mathf.Max(0, amount));
        }
    }
}
