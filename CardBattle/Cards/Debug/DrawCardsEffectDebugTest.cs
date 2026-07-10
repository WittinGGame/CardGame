using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Lightweight Play Mode harness for presentation-driven draws (Phase 8B-1B).
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
