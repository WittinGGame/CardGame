using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Card Battle/Effects/Heal")]
    public class HealEffectData : CardEffectData
    {
        [SerializeField] private int healAmount = 2;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, healAmount);
            return $"Heal <color=#B0966E>{value}</color>";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            context.Player.Heal(Mathf.Max(0, healAmount));
        }
    }
}
