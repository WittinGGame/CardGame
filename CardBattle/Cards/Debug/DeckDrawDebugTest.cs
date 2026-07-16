using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Play Mode / Context Menu harness for Phase 8B-1A hand limit and draw overflow.
    /// Uses only public <see cref="DeckController"/> APIs — does not mutate piles directly.
    /// </summary>
    public class DeckDrawDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[DeckDrawDebugTest]";

        [Header("References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private PileCounterUI pileCounterUI;

        [Header("Test Deck")]
        [SerializeField] private List<CardData> testDeckCards = new List<CardData>();

        [Header("Test Values")]
        [SerializeField] private int desiredHandCount = 5;
        [SerializeField] private int requestedDrawCount = 3;
        [SerializeField] private bool verboseLogs = true;

        // -------------------------------------------------------------------------
        // Context menus
        // -------------------------------------------------------------------------

        [ContextMenu("Debug/Rebuild Test Deck")]
        public void RebuildTestDeck()
        {
            if (!ValidateDeck())
                return;

            if (!HasAnyTestCard())
            {
                Debug.LogError($"{LogPrefix} testDeckCards needs at least one CardData.");
                return;
            }

            deckController.BuildFromCardDataList(testDeckCards);
            RefreshUi();
            PrintPileStateInternal("Rebuild Test Deck");
        }

        [ContextMenu("Debug/Fill Hand To Desired Count")]
        public void FillHandToDesiredCount()
        {
            if (!ValidateDeck())
                return;

            int target = Mathf.Clamp(desiredHandCount, 0, deckController.MaxHandSize);
            int safetyLimit = GetVisibleCardTotal() + 2;
            int iterations = 0;

            while (deckController.Hand.Count < target)
            {
                if (deckController.GetDeckCount() == 0 && deckController.GetGraveyardCount() == 0)
                    break;

                var step = deckController.DrawCards(1);
                if (step.DrawnCount <= 0)
                    break;

                iterations++;
                if (iterations > safetyLimit)
                {
                    Debug.LogError($"{LogPrefix} Fill Hand aborted — safety limit reached.");
                    break;
                }
            }

            RefreshUi();

            if (verboseLogs)
            {
                Debug.Log(
                    $"{LogPrefix} Fill Hand | Target={target} | " +
                    $"Hand={deckController.Hand.Count} | Iterations={iterations}");
            }

            PrintPileStateInternal("After Fill Hand");
        }

        [ContextMenu("Debug/Draw Requested Cards")]
        public void DrawRequestedCards()
        {
            if (!ValidateDeck())
                return;

            // No auto-reshuffle; flush overflow so PendingOverflowCount is 0 after the action.
            var result = deckController.DrawCardsFromDeckImmediate(requestedDrawCount);
            deckController.FlushPendingOverflowToGraveyard();
            RefreshUi();
            LogDrawResult(result);
            PrintPileStateInternal("After Draw Requested Cards");
        }

        [ContextMenu("Debug/Draw Requested Cards With Auto Reshuffle")]
        public void DrawRequestedCardsWithAutoReshuffle()
        {
            if (!ValidateDeck())
                return;

            var result = deckController.DrawCards(requestedDrawCount);
            RefreshUi();
            LogDrawResult(result);
            PrintPileStateInternal("After Draw With Auto Reshuffle");
        }

        [ContextMenu("Debug/Force Reshuffle")]
        public void ForceReshuffle()
        {
            if (!ValidateDeck())
                return;

            int moved = deckController.ReshuffleGraveyardIntoDeckImmediate();
            RefreshUi();

            if (verboseLogs)
                Debug.Log($"{LogPrefix} Force Reshuffle | Moved={moved}");

            PrintPileStateInternal("After Force Reshuffle");
        }

        [ContextMenu("Debug/Print Pile State")]
        public void PrintPileState()
        {
            if (!ValidateDeck())
                return;

            PrintPileStateInternal("Print Pile State");
        }

        [ContextMenu("Debug/Validate Card Conservation")]
        public void ValidateCardConservationMenu()
        {
            if (!ValidateDeck())
                return;

            if (TryValidateConservation(out string message, requirePendingEmpty: true))
                Debug.Log($"{LogPrefix} PASS — Card conservation valid\n{message}");
            else
                Debug.LogError($"{LogPrefix} FAIL — Card conservation\n{message}");
        }

        [ContextMenu("Debug/Run Basic Hand Limit Tests")]
        public void RunBasicHandLimitTests()
        {
            if (!ValidateDeck())
                return;

            if (!HasEnoughTestCards(13))
            {
                Debug.LogError(
                    $"{LogPrefix} Basic tests need at least 13 non-null cards in testDeckCards " +
                    $"(found {CountNonNullTestCards()}).");
                return;
            }

            int max = deckController.MaxHandSize;
            if (max < 10)
            {
                Debug.LogError(
                    $"{LogPrefix} Basic tests expect MaxHandSize >= 10 (current={max}).");
                return;
            }

            int passed = 0;
            int failed = 0;

            RunCase("Test A: Normal Draw", TestA_NormalDraw, ref passed, ref failed);
            RunCase("Test B: One Free Slot", TestB_OneFreeSlot, ref passed, ref failed);
            RunCase("Test C: Full Hand", TestC_FullHand, ref passed, ref failed);
            RunCase("Test D: Empty Sources", TestD_EmptySources, ref passed, ref failed);

            RefreshUi();
            Debug.Log($"{LogPrefix} === Basic Hand Limit Tests Done | Passed={passed} Failed={failed} ===");
        }

        [ContextMenu("Debug/Run Reshuffle Overflow Test")]
        public void RunReshuffleOverflowTest()
        {
            if (!ValidateDeck())
                return;

            if (!HasEnoughTestCards(13))
            {
                Debug.LogError(
                    $"{LogPrefix} Reshuffle overflow test needs at least 13 non-null cards " +
                    $"(found {CountNonNullTestCards()}).");
                return;
            }

            if (deckController.MaxHandSize < 10)
            {
                Debug.LogError(
                    $"{LogPrefix} Reshuffle overflow test expects MaxHandSize >= 10 " +
                    $"(current={deckController.MaxHandSize}).");
                return;
            }

            int passed = 0;
            int failed = 0;
            RunCase(
                "Test: Reshuffle + Overflow",
                TestReshuffleOverflowConservation,
                ref passed,
                ref failed);
            RefreshUi();
        }

        // -------------------------------------------------------------------------
        // Tests A–E
        // -------------------------------------------------------------------------

        private bool TestA_NormalDraw(StringBuilder fail)
        {
            PrepareFreshDeckAndHand(handCount: 5, out int deckBefore);

            var result = deckController.DrawCards(2);

            return Expect(fail, "Requested", result.RequestedCount, 2)
                   && Expect(fail, "Drawn", result.DrawnCount, 2)
                   && Expect(fail, "AddedToHand", result.AddedToHandCount, 2)
                   && Expect(fail, "Overflow", result.OverflowedToGraveyardCount, 0)
                   && Expect(fail, "Hand", deckController.Hand.Count, 7)
                   && Expect(fail, "Deck", deckController.Deck.Count, deckBefore - 2)
                   && Expect(fail, "Graveyard", deckController.Graveyard.Count, 0)
                   && ExpectPostDrawConservation(fail);
        }

        private bool TestB_OneFreeSlot(StringBuilder fail)
        {
            PrepareFreshDeckAndHand(handCount: 9, out int deckBefore);
            int gyBefore = deckController.Graveyard.Count;

            var result = deckController.DrawCards(3);

            return Expect(fail, "Drawn", result.DrawnCount, 3)
                   && Expect(fail, "AddedToHand", result.AddedToHandCount, 1)
                   && Expect(fail, "Overflow", result.OverflowedToGraveyardCount, 2)
                   && Expect(fail, "Hand", deckController.Hand.Count, 10)
                   && Expect(fail, "Deck", deckController.Deck.Count, deckBefore - 3)
                   && Expect(fail, "Graveyard", deckController.Graveyard.Count, gyBefore + 2)
                   && ExpectPostDrawConservation(fail);
        }

        private bool TestC_FullHand(StringBuilder fail)
        {
            PrepareFreshDeckAndHand(handCount: 10, out int deckBefore);
            int gyBefore = deckController.Graveyard.Count;

            var result = deckController.DrawCards(2);

            return Expect(fail, "Drawn", result.DrawnCount, 2)
                   && Expect(fail, "AddedToHand", result.AddedToHandCount, 0)
                   && Expect(fail, "Overflow", result.OverflowedToGraveyardCount, 2)
                   && Expect(fail, "Hand", deckController.Hand.Count, 10)
                   && Expect(fail, "Deck", deckController.Deck.Count, deckBefore - 2)
                   && Expect(fail, "Graveyard", deckController.Graveyard.Count, gyBefore + 2)
                   && ExpectPostDrawConservation(fail);
        }

        private bool TestD_EmptySources(StringBuilder fail)
        {
            deckController.ClearAllPiles();

            CardDrawResult result;
            try
            {
                result = deckController.DrawCards(2);
            }
            catch (Exception ex)
            {
                fail.Append("Exception: ").Append(ex.Message).Append(' ');
                return false;
            }

            return Expect(fail, "Drawn", result.DrawnCount, 0)
                   && Expect(fail, "AddedToHand", result.AddedToHandCount, 0)
                   && Expect(fail, "Overflow", result.OverflowedToGraveyardCount, 0)
                   && Expect(fail, "Hand", deckController.Hand.Count, 0)
                   && Expect(fail, "Deck", deckController.Deck.Count, 0)
                   && Expect(fail, "Graveyard", deckController.Graveyard.Count, 0)
                   && ExpectPostDrawConservation(fail);
        }

        private bool TestReshuffleOverflowConservation(StringBuilder fail)
        {
            // Public flow: 13 cards → hand 10, deck 3 → overflow draw 2 → hand 10, deck 1, gy 2.
            PrepareFreshDeckAndHand(handCount: 10, out int deckAfterFill);
            if (deckAfterFill < 3)
            {
                fail.Append("Need Deck >= 3 after filling hand to 10. Actual=")
                    .Append(deckAfterFill)
                    .Append(". ");
                return false;
            }

            var overflowSetup = deckController.DrawCards(2);
            if (!Expect(fail, "SetupOverflow.Drawn", overflowSetup.DrawnCount, 2)
                || !Expect(fail, "SetupOverflow.Overflow", overflowSetup.OverflowedToGraveyardCount, 2))
            {
                return false;
            }

            // If deck still > 1 (larger test deck), overflow-trim down to 1.
            int trimSafety = 0;
            while (deckController.GetDeckCount() > 1)
            {
                var trim = deckController.DrawCards(1);
                if (trim.DrawnCount <= 0)
                    break;

                trimSafety++;
                if (trimSafety > 64)
                {
                    fail.Append("Could not trim deck to 1. ");
                    return false;
                }
            }

            int gyBefore = deckController.GetGraveyardCount();
            if (!Expect(fail, "DeckBeforeDraw3", deckController.GetDeckCount(), 1)
                || !Expect(fail, "HandBeforeDraw3", deckController.Hand.Count, 10)
                || gyBefore < 2)
            {
                if (gyBefore < 2)
                    fail.Append("Expected Graveyard >= 2 before Draw 3. Actual=").Append(gyBefore).Append(". ");
                return false;
            }

            var deckList = deckController.Deck;
            if (deckList.Count == 0 || deckList[deckList.Count - 1] == null)
            {
                fail.Append("Missing top deck card before Draw 3. ");
                return false;
            }

            Guid topId = deckList[deckList.Count - 1].InstanceId;
            int totalBefore = GetVisibleCardTotal();

            var result = deckController.DrawCards(3);

            // After: draw 1 (pending) → reshuffle gyBefore → draw 2 (pending) → flush 3 to GY.
            // Deck remaining = gyBefore - 2, Graveyard = 3, Hand = 10.
            int topOccurrences = CountInstanceId(topId);
            if (topOccurrences != 1)
            {
                fail.Append("Top deck card InstanceId expected exactly 1 occurrence, got ")
                    .Append(topOccurrences)
                    .Append(". ");
                return false;
            }

            return Expect(fail, "Drawn", result.DrawnCount, 3)
                   && Expect(fail, "AddedToHand", result.AddedToHandCount, 0)
                   && Expect(fail, "Overflow", result.OverflowedToGraveyardCount, 3)
                   && Expect(fail, "Hand", deckController.Hand.Count, 10)
                   && Expect(fail, "Deck", deckController.Deck.Count, gyBefore - 2)
                   && Expect(fail, "Graveyard", deckController.Graveyard.Count, 3)
                   && Expect(fail, "Total", GetVisibleCardTotal(), totalBefore)
                   && ExpectPostDrawConservation(fail);
        }

        // -------------------------------------------------------------------------
        // Setup helpers (public API only)
        // -------------------------------------------------------------------------

        private void PrepareFreshDeckAndHand(int handCount, out int deckAfterFill)
        {
            deckController.BuildFromCardDataList(testDeckCards);

            int target = Mathf.Clamp(handCount, 0, deckController.MaxHandSize);
            int safetyLimit = GetVisibleCardTotal() + 2;
            int iterations = 0;

            while (deckController.Hand.Count < target)
            {
                if (deckController.GetDeckCount() == 0 && deckController.GetGraveyardCount() == 0)
                    break;

                var step = deckController.DrawCards(1);
                if (step.DrawnCount <= 0)
                    break;

                iterations++;
                if (iterations > safetyLimit)
                    break;
            }

            deckAfterFill = deckController.GetDeckCount();
        }

        // -------------------------------------------------------------------------
        // Validation / logging
        // -------------------------------------------------------------------------

        private bool ExpectPostDrawConservation(StringBuilder fail)
        {
            if (deckController.Hand.Count > deckController.MaxHandSize)
            {
                fail.Append("Hand exceeds MaxHandSize. ");
                return false;
            }

            if (deckController.PendingOverflowCount != 0)
            {
                fail.Append("PendingOverflowCount expected 0 after draw, got ")
                    .Append(deckController.PendingOverflowCount)
                    .Append(". ");
                return false;
            }

            if (!TryValidateConservation(out string message, requirePendingEmpty: true))
            {
                fail.Append(message).Append(' ');
                return false;
            }

            return true;
        }

        private bool TryValidateConservation(out string message, bool requirePendingEmpty)
        {
            var sb = new StringBuilder();
            var seen = new Dictionary<Guid, string>();
            int duplicates = 0;
            int total = 0;

            CountPileIds("Deck", deckController.Deck, seen, sb, ref total, ref duplicates);
            CountPileIds("Hand", deckController.Hand, seen, sb, ref total, ref duplicates);
            CountPileIds("Graveyard", deckController.Graveyard, seen, sb, ref total, ref duplicates);
            CountPileIds("Exhaust", deckController.ExhaustPile, seen, sb, ref total, ref duplicates);
            CountPileIds("Removed", deckController.RemovedCards, seen, sb, ref total, ref duplicates);

            // Pending overflow is count-only (collection is not exposed).
            int pending = deckController.PendingOverflowCount;
            total += pending;

            bool handOk = deckController.Hand.Count <= deckController.MaxHandSize;
            bool pendingOk = !requirePendingEmpty || pending == 0;

            sb.Append("Total=").Append(total)
                .Append(" Deck=").Append(deckController.Deck.Count)
                .Append(" Hand=").Append(deckController.Hand.Count)
                .Append('/').Append(deckController.MaxHandSize)
                .Append(" Graveyard=").Append(deckController.Graveyard.Count)
                .Append(" Exhaust=").Append(deckController.GetExhaustCount())
                .Append(" Removed=").Append(deckController.GetRemovedCount())
                .Append(" PendingOverflow=").Append(pending);

            if (!handOk)
                sb.Append(" | Hand exceeds MaxHandSize");

            if (!pendingOk)
                sb.Append(" | PendingOverflowCount expected 0");

            message = sb.ToString();
            return duplicates == 0 && handOk && pendingOk;
        }

        private static void CountPileIds(
            string pileName,
            IReadOnlyList<CardInstance> pile,
            Dictionary<Guid, string> seen,
            StringBuilder dupBuilder,
            ref int total,
            ref int duplicates)
        {
            if (pile == null)
                return;

            var local = new HashSet<Guid>();

            for (int i = 0; i < pile.Count; i++)
            {
                var card = pile[i];
                if (card == null)
                    continue;

                total++;

                if (!local.Add(card.InstanceId))
                {
                    duplicates++;
                    dupBuilder.Append("Duplicate within ")
                        .Append(pileName)
                        .Append(": ")
                        .Append(card.InstanceId)
                        .Append(". ");
                }

                if (seen.TryGetValue(card.InstanceId, out var otherPile))
                {
                    duplicates++;
                    dupBuilder.Append("Cross-pile duplicate ")
                        .Append(card.InstanceId)
                        .Append(" in ")
                        .Append(otherPile)
                        .Append(" and ")
                        .Append(pileName)
                        .Append(". ");
                }
                else
                {
                    seen.Add(card.InstanceId, pileName);
                }
            }
        }

        private int CountInstanceId(Guid id)
        {
            int count = 0;
            count += CountIdInPile(deckController.Deck, id);
            count += CountIdInPile(deckController.Hand, id);
            count += CountIdInPile(deckController.Graveyard, id);
            count += CountIdInPile(deckController.ExhaustPile, id);
            count += CountIdInPile(deckController.RemovedCards, id);
            return count;
        }

        private static int CountIdInPile(IReadOnlyList<CardInstance> pile, Guid id)
        {
            int count = 0;
            if (pile == null)
                return 0;

            for (int i = 0; i < pile.Count; i++)
            {
                if (pile[i] != null && pile[i].InstanceId == id)
                    count++;
            }

            return count;
        }

        private void LogDrawResult(CardDrawResult result)
        {
            Debug.Log(
                $"{LogPrefix}\n" +
                $"Requested={result.RequestedCount}\n" +
                $"Drawn={result.DrawnCount}\n" +
                $"AddedToHand={result.AddedToHandCount}\n" +
                $"Overflowed={result.OverflowedToGraveyardCount}");
        }

        private void PrintPileStateInternal(string label)
        {
            if (!verboseLogs && label != "Print Pile State")
                return;

            int total = GetVisibleCardTotal();
            Debug.Log(
                $"{LogPrefix} --- {label} ---\n" +
                $"Deck={deckController.Deck.Count}\n" +
                $"Hand={deckController.Hand.Count}/{deckController.MaxHandSize}\n" +
                $"AvailableHandSpace={deckController.AvailableHandSpace}\n" +
                $"Graveyard={deckController.Graveyard.Count}\n" +
                $"Exhaust={deckController.GetExhaustCount()}\n" +
                $"Removed={deckController.GetRemovedCount()}\n" +
                $"PendingOverflow={deckController.PendingOverflowCount}\n" +
                $"Total={total}");
        }

        private int GetVisibleCardTotal()
        {
            return deckController.Deck.Count
                   + deckController.Hand.Count
                   + deckController.Graveyard.Count
                   + deckController.GetExhaustCount()
                   + deckController.GetRemovedCount()
                   + deckController.PendingOverflowCount;
        }

        private void RefreshUi()
        {
            if (handUIController != null)
                handUIController.RefreshHandUI();

            if (pileCounterUI != null)
                pileCounterUI.ForceSyncDisplayedToReal();
        }

        private void RunCase(
            string name,
            Func<StringBuilder, bool> test,
            ref int passed,
            ref int failed)
        {
            var fail = new StringBuilder();
            bool ok;
            try
            {
                ok = test(fail);
            }
            catch (Exception ex)
            {
                ok = false;
                fail.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            }

            if (ok)
            {
                passed++;
                Debug.Log($"{LogPrefix} PASS — {name}");
            }
            else
            {
                failed++;
                Debug.LogError(
                    $"{LogPrefix} FAIL — {name}\n" +
                    $"Expected/Actual details: {fail}\n" +
                    $"Actual state: Deck={deckController.Deck.Count} " +
                    $"Hand={deckController.Hand.Count}/{deckController.MaxHandSize} " +
                    $"GY={deckController.Graveyard.Count} " +
                    $"Removed={deckController.GetRemovedCount()} " +
                    $"Pending={deckController.PendingOverflowCount}");
            }
        }

        private static bool Expect(StringBuilder fail, string label, int actual, int expected)
        {
            if (actual == expected)
                return true;

            fail.Append(label)
                .Append(" Expected=")
                .Append(expected)
                .Append(" Actual=")
                .Append(actual)
                .Append(". ");
            return false;
        }

        private bool ValidateDeck()
        {
            if (deckController != null)
                return true;

            Debug.LogError($"{LogPrefix} DeckController reference is missing.");
            return false;
        }

        private bool HasAnyTestCard()
        {
            return CountNonNullTestCards() > 0;
        }

        private bool HasEnoughTestCards(int minimum)
        {
            return CountNonNullTestCards() >= minimum;
        }

        private int CountNonNullTestCards()
        {
            if (testDeckCards == null)
                return 0;

            int count = 0;
            for (int i = 0; i < testDeckCards.Count; i++)
            {
                if (testDeckCards[i] != null)
                    count++;
            }

            return count;
        }
    }
}
