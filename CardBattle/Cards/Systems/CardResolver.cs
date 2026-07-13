using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Central place for card effect execution via <see cref="CardData.Effects"/>.
    /// Modifiers may still skip base resolution through <see cref="CardPlayContext.ApplyBaseCardLogic"/>.
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

            int requestedDrawCount = 0;

            if (context.ApplyBaseCardLogic)
                requestedDrawCount = ApplyEffectCardLogic(context);

            foreach (var modifier in context.Card.Modifiers)
                modifier?.PostResolve(context);

            if (logResolution)
            {
                Debug.Log(
                    $"Resolved {context.Card.Data.DisplayName} via Effects pipeline. " +
                    $"RequestedDraw={requestedDrawCount}");
            }

            return new CardResolutionResult(requestedDrawCount);
        }

        private static int ApplyEffectCardLogic(CardPlayContext context)
        {
            if (context?.Card?.Data == null)
                return 0;

            var data = context.Card.Data;
            var effects = data.Effects;

            if (!HasAnyValidEffect(effects))
            {
                Debug.LogWarning(
                    $"[CardResolver] Card '{data.CardId}' has no valid effects. " +
                    "No gameplay effect was applied.");
                return 0;
            }

            var enemyTargets = TargetResolver.ResolveEnemyTargets(context, data.TargetMode);
            var executionContext = new CardEffectExecutionContext(enemyTargets);

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                    continue;

                effect.Apply(context, executionContext);
            }

            return executionContext.RequestedDrawCount;
        }

        private static bool HasAnyValidEffect(System.Collections.Generic.IReadOnlyList<CardEffectData> effects)
        {
            if (effects == null || effects.Count == 0)
                return false;

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null)
                    return true;
            }

            return false;
        }
    }
}
