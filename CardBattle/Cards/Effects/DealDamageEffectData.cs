using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "DealDamageEffect", menuName = "Card Battle/Effects/Deal Damage")]
    public class DealDamageEffectData : CardEffectData
    {
        [SerializeField] private int damage = 3;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, damage);
            return $"Deal <color=#FF6B6B>{value} damage</color>";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context == null || executionContext == null)
                return;

            int bonus = context.Player != null ? context.Player.ConsumeDamageBonus() : 0;
            int totalDamage = Mathf.Max(0, damage + bonus);
            if (totalDamage <= 0 || executionContext.EnemyTargets == null)
                return;

            for (int i = 0; i < executionContext.EnemyTargets.Count; i++)
            {
                var target = executionContext.EnemyTargets[i];
                if (target == null || !target.IsAlive)
                    continue;

                bool wasAliveBeforeHit = target.IsAlive;
                int hpDamage = target.TakeDamage(totalDamage);

                if (wasAliveBeforeHit)
                {
                    if (!target.IsAlive)
                        target.View?.PlayDead();
                    else if (hpDamage > 0)
                        target.View?.PlayHurt();
                }
            }
        }
    }
}
