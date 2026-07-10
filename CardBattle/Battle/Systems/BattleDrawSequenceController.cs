using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Owns presentation-driven drawing: one card at a time from the current deck,
    /// optional graveyard-to-deck VFX + reshuffle, then flush pending overflow.
    /// Relies on HandUIController incremental SyncHandViews (via OnPilesChanged).
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
        [Tooltip("Optional short pause after an overflow draw (no hand entry animation).")]
        [SerializeField] private float overflowDrawCadence = 0.05f;
        [Tooltip("Extra settle time after a card is added to hand and deal unlock finishes.")]
        [SerializeField] private float handEntrySettleDelay = 0.12f;

        [Header("Diagnostics")]
        [SerializeField] private bool verboseLogs;

        /// <summary>
        /// Draws up to <paramref name="requestedCount"/> cards one at a time with the same
        /// reshuffle presentation rules used at player round start.
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

            if (handUIController == null)
            {
                Debug.LogWarning(
                    "[BattleDrawSequence] HandUIController is missing. " +
                    "Draw logic will still complete without hand presentation waits.");
            }

            int drawn = 0;
            int addedToHand = 0;
            int overflowed = 0;
            int reshuffled = 0;
            bool drawStarted = false;

            try
            {
                for (int i = 0; i < requested; i++)
                {
                    if (deckController.GetDeckCount() == 0)
                    {
                        int graveCount = deckController.GetGraveyardCount();
                        if (graveCount <= 0)
                            break;

                        // Reshuffle uses the current graveyard only — pending overflow stays out.
                        if (graveyardToDeckVfx != null)
                            yield return graveyardToDeckVfx.PlayReshuffleVfx(graveCount);

                        reshuffled += deckController.ReshuffleGraveyardIntoDeckImmediate();
                        drawStarted = true;

                        if (postReshuffleDrawDelay > 0f)
                            yield return new WaitForSeconds(postReshuffleDrawDelay);

                        if (deckController.GetDeckCount() == 0)
                            break;
                    }

                    drawStarted = true;
                    var step = deckController.DrawCardsFromDeckImmediate(1);
                    if (step.DrawnCount <= 0)
                        break;

                    drawn += step.DrawnCount;
                    addedToHand += step.AddedToHandCount;
                    overflowed += step.OverflowedToGraveyardCount;

                    if (step.AddedToHandCount > 0)
                    {
                        if (handUIController != null)
                            yield return handUIController.WaitForDealPresentationComplete();

                        if (handEntrySettleDelay > 0f)
                            yield return new WaitForSeconds(handEntrySettleDelay);
                    }
                    else if (overflowDrawCadence > 0f)
                    {
                        yield return new WaitForSeconds(overflowDrawCadence);
                    }
                }
            }
            finally
            {
                if (drawStarted || deckController.PendingOverflowCount > 0)
                    deckController.FlushPendingOverflowToGraveyard();

                // Incremental sync only — never RefreshHandUI during normal draw.
                if (handUIController != null)
                    handUIController.SyncHandViewsExternal();

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
