using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Card Battle/Effects/Heal")]
    public class HealEffectData : CardEffectData
    {
        [SerializeField] private int healAmount = 2;

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            context.Player.Heal(Mathf.Max(0, healAmount));
        }
    }
}
