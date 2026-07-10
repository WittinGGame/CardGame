using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Lightweight Play Mode harness for presentation-driven draws.
    /// </summary>
    public class DrawCardsEffectDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[DrawCardsEffectDebugTest]";

        [Header("References")]
        [SerializeField] private BattleDrawSequenceController battleDrawSequenceController;
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Test Values")]
        [SerializeField] private int customDrawAmount = 2;
        [SerializeField] private bool verboseLogs = true;

        private Coroutine runningRoutine;

        [ContextMenu("Debug/Draw 2 With Presentation")]
        public void DebugDraw2WithPresentation()
        {
            StartDrawRoutine(2);
        }

        [ContextMenu("Debug/Draw 2 Incrementally")]
        public void DebugDraw2Incrementally()
        {
            StartDrawRoutine(2);
        }

        [ContextMenu("Debug/Draw Custom Amount With Presentation")]
        public void DebugDrawCustomAmountWithPresentation()
        {
            StartDrawRoutine(customDrawAmount);
        }

        [ContextMenu("Debug/Print Draw State")]
        public void DebugPrintDrawState()
        {
            if (!ValidateDeck())
                return;

            Debug.Log(
                $"{LogPrefix}\n" +
                $"Deck={deckController.Deck.Count}\n" +
                $"Hand={deckController.Hand.Count}/{deckController.MaxHandSize}\n" +
                $"AvailableHandSpace={deckController.AvailableHandSpace}\n" +
                $"Graveyard={deckController.Graveyard.Count}\n" +
                $"PendingOverflow={deckController.PendingOverflowCount}");
        }

        [ContextMenu("Debug/Validate Pending Overflow Empty")]
        public void DebugValidatePendingOverflowEmpty()
        {
            if (!ValidateDeck())
                return;

            int pending = deckController.PendingOverflowCount;
            if (pending == 0)
                Debug.Log($"{LogPrefix} PASS — PendingOverflowCount=0");
            else
                Debug.LogError($"{LogPrefix} FAIL — PendingOverflowCount={pending}");
        }

        [ContextMenu("Debug/Validate Hand View Identity")]
        public void DebugValidateHandViewIdentity()
        {
            if (!ValidateDeck())
                return;

            if (handUIController == null)
            {
                Debug.LogError($"{LogPrefix} HandUIController reference is missing.");
                return;
            }

            var hand = deckController.Hand;
            var views = handUIController.GetCurrentHandViewsSnapshot();
            var fail = new StringBuilder();

            int validHandCount = 0;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i]?.Data != null)
                    validHandCount++;
            }

            if (views.Count != validHandCount)
            {
                fail.Append("VisibleViews=").Append(views.Count)
                    .Append(" ValidHandCards=").Append(validHandCount).Append(". ");
            }

            var seenCards = new HashSet<CardInstance>();
            for (int i = 0; i < views.Count; i++)
            {
                var view = views[i];
                if (view == null)
                {
                    fail.Append("Null view in snapshot. ");
                    continue;
                }

                var bound = view.BoundCard;
                if (bound == null || bound.Data == null)
                {
                    fail.Append("View has null BoundCard. ");
                    continue;
                }

                if (!deckController.IsInHand(bound))
                    fail.Append("Stale view for ").Append(bound.Data.DisplayName).Append(". ");

                if (!seenCards.Add(bound))
                    fail.Append("Duplicate view binding for ").Append(bound.Data.DisplayName).Append(". ");
            }

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                if (handUIController.GetViewForCard(card) == null)
                    fail.Append("Missing view for ").Append(card.Data.DisplayName).Append(". ");
            }

            if (fail.Length == 0)
                Debug.Log($"{LogPrefix} PASS — Hand view identity valid (Views={views.Count})");
            else
                Debug.LogError($"{LogPrefix} FAIL — Hand view identity\n{fail}");
        }

        private void StartDrawRoutine(int amount)
        {
            if (battleDrawSequenceController == null)
            {
                Debug.LogError($"{LogPrefix} BattleDrawSequenceController reference is missing.");
                return;
            }

            if (runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
                runningRoutine = null;
            }

            runningRoutine = StartCoroutine(CoDraw(amount));
        }

        private IEnumerator CoDraw(int amount)
        {
            if (verboseLogs)
                Debug.Log($"{LogPrefix} Starting DrawCardsRoutine({amount})");

            yield return battleDrawSequenceController.DrawCardsRoutine(amount);

            if (handUIController != null)
                handUIController.RefreshInteractivityExternal();

            DebugPrintDrawState();
            DebugValidatePendingOverflowEmpty();
            DebugValidateHandViewIdentity();
            runningRoutine = null;
        }

        private bool ValidateDeck()
        {
            if (deckController != null)
                return true;

            Debug.LogError($"{LogPrefix} DeckController reference is missing.");
            return false;
        }
    }
}
