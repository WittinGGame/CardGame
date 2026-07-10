using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Owns presentation-driven drawing: deck-first draw, optional graveyard-to-deck VFX,
    /// reshuffle, remaining draw, then flush pending overflow.
    /// </summary>
    public class BattleDrawSequenceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private GraveyardToDeckVFXController graveyardToDeckVfx;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private PileCounterUI pileCounterUI;

        [Header("Timing")]
        [SerializeField] private float postReshuffleDrawDelay = 0.08f;

        [Header("Diagnostics")]
        [SerializeField] private bool verboseLogs;

        /// <summary>
        /// Draws up to <paramref name="requestedCount"/> cards with the same two-phase
        /// presentation flow used at player round start.
        /// </summary>
        public IEnumerator DrawCardsRoutine(int requestedCount)
        {
            int requested = Mathf.Max(0, requestedCount);
            if (requested == 0)
                yield break;

            if (deckController == null)
            {
                Debug.LogError("[BattleDrawSequence] DeckController reference is missing.");
                yield break;
            }

            int drawn = 0;
            int addedToHand = 0;
            int overflowed = 0;
            int reshuffled = 0;
            bool drawStarted = false;

            try
            {
                // Phase A — draw from current deck only (overflow stays pending).
                int availableDeck = deckController.GetDeckCount();
                int firstDraw = Mathf.Min(requested, availableDeck);

                if (firstDraw > 0)
                {
                    drawStarted = true;
                    var firstResult = deckController.DrawCardsFromDeckImmediate(firstDraw);
                    drawn += firstResult.DrawnCount;
                    addedToHand += firstResult.AddedToHandCount;
                    overflowed += firstResult.OverflowedToGraveyardCount;
                }

                int remaining = requested - firstDraw;
                if (remaining > 0)
                {
                    // Phase B/C — reshuffle presentation using the pre-overflow graveyard only.
                    int graveCount = deckController.GetGraveyardCount();
                    if (graveCount > 0)
                    {
                        if (graveyardToDeckVfx != null)
                            yield return graveyardToDeckVfx.PlayReshuffleVfx(graveCount);

                        reshuffled = deckController.ReshuffleGraveyardIntoDeckImmediate();
                        drawStarted = true;

                        if (postReshuffleDrawDelay > 0f)
                            yield return new WaitForSeconds(postReshuffleDrawDelay);

                        // Phase E — draw remaining from reshuffled deck.
                        int secondDraw = Mathf.Min(remaining, deckController.GetDeckCount());
                        if (secondDraw > 0)
                        {
                            var secondResult = deckController.DrawCardsFromDeckImmediate(secondDraw);
                            drawn += secondResult.DrawnCount;
                            addedToHand += secondResult.AddedToHandCount;
                            overflowed += secondResult.OverflowedToGraveyardCount;
                        }
                    }
                }
            }
            finally
            {
                // Always flush after any draw work so pending overflow cannot stick
                // and cannot be reshuffled mid-operation.
                if (drawStarted || deckController.PendingOverflowCount > 0)
                    deckController.FlushPendingOverflowToGraveyard();

                if (handUIController != null)
                    handUIController.RefreshHandUI();

                if (pileCounterUI != null)
                    pileCounterUI.ForceSyncDisplayedToReal();

                if (verboseLogs)
                {
                    Debug.Log(
                        "[BattleDrawSequence]\n" +
                        $"Requested={requested}\n" +
                        $"Drawn={drawn}\n" +
                        $"AddedToHand={addedToHand}\n" +
                        $"Overflowed={overflowed}\n" +
                        $"Reshuffled={reshuffled}\n" +
                        $"Deck={deckController.Deck.Count}\n" +
                        $"Hand={deckController.Hand.Count}/{deckController.MaxHandSize}\n" +
                        $"Graveyard={deckController.Graveyard.Count}\n" +
                        $"PendingOverflow={deckController.PendingOverflowCount}");
                }
            }
        }
    }
}
