using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Executes <see cref="CardData.Effects"/> one-at-a-time in array order,
    /// yielding for draw presentation and manual hand selection.
    /// </summary>
    public class CardEffectSequenceRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private BattleDrawSequenceController battleDrawSequenceController;
        [SerializeField] private HandCardSelectionController handCardSelectionController;
        [SerializeField] private DeckController deckController;

        [Header("Options")]
        [SerializeField] private bool verboseLogs;

        public IEnumerator ExecuteEffectsSequentially(CardPlayContext context)
        {
            if (context?.Card?.Data == null || context.Player == null)
                yield break;

            foreach (var modifier in context.Card.Modifiers)
            {
                if (modifier != null && !modifier.PreResolve(context))
                    context.ApplyBaseCardLogic = false;
            }

            if (!context.ApplyBaseCardLogic)
            {
                foreach (var modifier in context.Card.Modifiers)
                    modifier?.PostResolve(context);
                yield break;
            }

            var effects = context.Card.Data.Effects;
            if (!HasAnyValidEffect(effects))
            {
                Debug.LogWarning(
                    $"[CardEffectSequence] Card '{context.Card.Data.CardId}' has no valid effects. " +
                    "No gameplay effect was applied.");

                foreach (var modifier in context.Card.Modifiers)
                    modifier?.PostResolve(context);
                yield break;
            }

            var enemyTargets = TargetResolver.ResolveEnemyTargets(context, context.Card.Data.TargetMode);
            var executionContext = new CardEffectExecutionContext(enemyTargets);

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                    continue;

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[CardEffectSequence] [{i}] {effect.name} ({effect.ExecutionKind})");
                }

                switch (effect.ExecutionKind)
                {
                    case CardEffectExecutionKind.DrawPresentation:
                        yield return ExecuteDrawPresentation(effect);
                        break;

                    case CardEffectExecutionKind.ManualHandSelection:
                        yield return ExecuteManualHandSelection(effect);
                        break;

                    case CardEffectExecutionKind.Immediate:
                    default:
                        if (cardResolver != null)
                            cardResolver.ResolveSingleEffect(context, effect, executionContext);
                        else
                            effect.Apply(context, executionContext);
                        break;
                }
            }

            foreach (var modifier in context.Card.Modifiers)
                modifier?.PostResolve(context);
        }

        private IEnumerator ExecuteDrawPresentation(CardEffectData effect)
        {
            int amount = 0;
            if (effect is DrawCardsEffectData drawEffect)
                amount = drawEffect.Amount;

            if (amount <= 0)
                yield break;

            if (battleDrawSequenceController != null)
            {
                yield return battleDrawSequenceController.DrawCardsRoutine(amount);
                yield break;
            }

            Debug.LogError(
                "[CardEffectSequence] BattleDrawSequenceController missing. " +
                "Falling back to immediate DrawCards.");

            if (deckController != null)
                deckController.DrawCards(amount);
        }

        private IEnumerator ExecuteManualHandSelection(CardEffectData effect)
        {
            int amount = 0;
            if (effect is DiscardCardsEffectData discardEffect)
                amount = discardEffect.Amount;

            if (amount <= 0)
                yield break;

            if (handCardSelectionController == null)
            {
                Debug.LogError(
                    "[CardEffectSequence] HandCardSelectionController missing. " +
                    "Skipping discard selection.");
                yield break;
            }

            yield return handCardSelectionController.SelectAndDiscardRoutine(amount);
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
