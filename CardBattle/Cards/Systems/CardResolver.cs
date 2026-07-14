using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Resolves immediate card effects. Presentation/interactive effects are owned by
    /// <see cref="CardEffectSequenceRunner"/> in the production play path.
    /// </summary>
    public class CardResolver : MonoBehaviour
    {
        [SerializeField] private bool logResolution;

        /// <summary>
        /// Debug/legacy sync resolve of every effect via Apply.
        /// Draw effects still accumulate RequestedDrawCount; discard Apply is a no-op.
        /// Production play uses <see cref="CardEffectSequenceRunner"/> instead.
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
                    $"Resolved {context.Card.Data.DisplayName} via sync Effects Apply. " +
                    $"RequestedDraw={requestedDrawCount}");
            }

            return new CardResolutionResult(requestedDrawCount);
        }

        /// <summary>
        /// Applies one immediate effect using a shared execution context.
        /// Does not iterate the card Effects array.
        /// </summary>
        public void ResolveSingleEffect(
            CardPlayContext context,
            CardEffectData effect,
            CardEffectExecutionContext executionContext)
        {
            if (context?.Card?.Data == null || context.Player == null || effect == null)
                return;

            if (effect.ExecutionKind != CardEffectExecutionKind.Immediate)
            {
                Debug.LogWarning(
                    $"[CardResolver] ResolveSingleEffect called for non-immediate effect " +
                    $"'{effect.name}' ({effect.ExecutionKind}). Ignoring.");
                return;
            }

            effect.Apply(context, executionContext);

            if (logResolution)
                Debug.Log($"[CardResolver] Immediate effect applied: {effect.name}");
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

        private static bool HasAnyValidEffect(IReadOnlyList<CardEffectData> effects)
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
