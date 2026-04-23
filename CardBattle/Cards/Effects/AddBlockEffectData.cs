using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "AddBlockEffect", menuName = "Card Battle/Effects/Add Block")]
    public class AddBlockEffectData : CardEffectData
    {
        [SerializeField] private int blockAmount = 5;

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            context.Player.AddBlock(Mathf.Max(0, blockAmount));
        }
    }
}
