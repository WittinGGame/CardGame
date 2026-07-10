using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Central place for card effect execution. Add branching per <see cref="CardType"/> here,
    /// and let <see cref="ICardModifier"/> adjust <see cref="CardPlayContext"/> before/after.
    /// </summary>
    public class CardResolver : MonoBehaviour
    {
        [SerializeField] private bool logResolution;

        /// <summary>
        /// Resolves the card synchronously and returns deferred requests (e.g. draw)
        /// for the battle runner to present afterward.
        /// </summary>
        public CardResolutionResult Resolve(CardPlayContext context)
        {
            if (context?.Card?.Data == null || context.Player == null)
                return CardResolutionResult.Empty;

            foreach (var modifier in context.Card.Modifiers)
            {
                if (modifier != null && !modifier.PreResolve(context))
                    context.ApplyBaseCardLogic = false;
            }

            bool usedEffectsPipeline = false;
            int requestedDrawCount = 0;

            if (context.ApplyBaseCardLogic)
            {
                if (context.Card.Data.HasEffects)
                {
                    requestedDrawCount = ApplyEffectCardLogic(context);
                    usedEffectsPipeline = true;
                }
                else
                {
                    ApplyCoreCardLogic(context);
                }
            }

            foreach (var modifier in context.Card.Modifiers)
                modifier?.PostResolve(context);

            if (logResolution)
            {
                string path = usedEffectsPipeline ? "Effects pipeline" : "Legacy CardType pipeline";
                Debug.Log(
                    $"Resolved {context.Card.Data.DisplayName} via {path}. " +
                    $"RequestedDraw={requestedDrawCount}");
            }

            return new CardResolutionResult(usedEffectsPipeline, requestedDrawCount);
        }

        private static int ApplyEffectCardLogic(CardPlayContext context)
        {
            if (context?.Card?.Data == null)
                return 0;

            var data = context.Card.Data;
            var enemyTargets = TargetResolver.ResolveEnemyTargets(context, data.TargetMode);
            var executionContext = new CardEffectExecutionContext(enemyTargets);

            var effects = data.Effects;
            if (effects == null)
                return 0;

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                    continue;

                effect.Apply(context, executionContext);
            }

            return executionContext.RequestedDrawCount;
        }

        private static void ApplyCoreCardLogic(CardPlayContext context)
        {
            var data = context.Card.Data;
            switch (data.CardType)
            {
                case CardType.Attack:
                    ResolveAttack(context, data);
                    break;
                case CardType.Heal:
                    context.Player.Heal(data.HealAmount);
                    break;
                case CardType.Buff:
                    context.Player.ApplyBuffFromCard(data);
                    break;
                case CardType.Defend:
                    context.Player.AddBlock(data.BlockAmount);
                    break;
                default:
                    Debug.LogWarning($"Unhandled card type {data.CardType}.");
                    break;
            }
        }

        private static void ResolveAttack(CardPlayContext context, CardData data)
        {
            var target = ChooseAttackTarget(context);
            if (target == null || !target.IsAlive)
                return;

            var bonus = context.Player.ConsumeDamageBonus();
            var total = data.AttackDamage + bonus;
            bool wasAliveBeforeHit = target.IsAlive;

            int hpDamage = target.TakeAttackDamage(context.Player, total);

            if (wasAliveBeforeHit)
            {
                if (!target.IsAlive)
                    target.View?.PlayDead();
                else if (hpDamage > 0)
                    target.View?.PlayHurt();
            }
        }

        private static EnemyBattleUnit ChooseAttackTarget(CardPlayContext context)
        {
            if (context.PrimaryTarget != null && context.PrimaryTarget.IsAlive)
                return context.PrimaryTarget;

            if (context.Enemies == null)
                return null;

            foreach (var enemy in context.Enemies)
            {
                if (enemy != null && enemy.IsAlive)
                    return enemy;
            }

            return null;
        }
    }
}
