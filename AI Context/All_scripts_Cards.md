## FILE: CardData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Data/CardData.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing definition of a card. Runtime copies are <see cref="CardInstance"/>.
    /// Keep numeric hooks here; layer modifiers via <see cref="ICardModifier"/> on instances later.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Card Battle/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField] private CardType cardType = CardType.Attack;
        [Tooltip("AP spent when the card is played successfully.")]
        [SerializeField] private int apCost = 1;

        [Header("Attack")]
        [SerializeField] private int attackDamage = 3;

        [Header("Heal")]
        [SerializeField] private int healAmount = 2;

        [Header("Buff")]
        [Tooltip("Generic potency for buffs (e.g. extra damage on next attack, block, etc.). Wired in CardResolver / player hooks.")]
        [SerializeField] private int buffPotency = 1;

        [Header("Defend")]
        [SerializeField] private int blockAmount = 5;

        [Header("Effect System (Phase 1)")]
        [SerializeField] private CardTargetMode targetMode = CardTargetMode.None;
        [SerializeField] private CardEffectData[] effects;

        [Header("Visuals")]
        [SerializeField] private Sprite artwork;

        public string CardId => string.IsNullOrEmpty(cardId) ? name : cardId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public CardType CardType => cardType;
        public int ApCost => Mathf.Max(0, apCost);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public int HealAmount => Mathf.Max(0, healAmount);
        public int BuffPotency => buffPotency;
        public int BlockAmount => Mathf.Max(0, blockAmount);
        public CardTargetMode TargetMode => targetMode;
        public IReadOnlyList<CardEffectData> Effects => effects;
        public bool HasEffects => effects != null && effects.Length > 0;
        public Sprite Artwork => artwork;
    }
}
```

## FILE: CardTargetMode.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Data/CardTargetMode.cs`
```csharp
namespace CardBattle.Core
{
    public enum CardTargetMode
    {
        None,
        Self,
        SingleEnemy,
        AllEnemies
    }
}
```

## FILE: CardType.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Data/CardType.cs`
```csharp
namespace CardBattle.Core
{
    /// Primary card families; extend with new enum values or parallel systems as content grows.
    public enum CardType
    {
        Attack,
        Buff,
        Heal,
        Defend
    }
}
```

## FILE: DeckDrawDebugTest.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Debug/DeckDrawDebugTest.cs`
```csharp
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
                $"PendingOverflow={deckController.PendingOverflowCount}\n" +
                $"Total={total}");
        }

        private int GetVisibleCardTotal()
        {
            return deckController.Deck.Count
                   + deckController.Hand.Count
                   + deckController.Graveyard.Count
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
```

## FILE: DrawCardsEffectDebugTest.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Debug/DrawCardsEffectDebugTest.cs`
```csharp
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
```

## FILE: GainApEffectDebugTest.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Debug/GainApEffectDebugTest.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Lightweight Play Mode harness for Gain AP effect / API validation.
    /// </summary>
    public class GainApEffectDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[GainApDebug]";

        [Header("References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private CardData testCard;

        [Header("Test Values")]
        [SerializeField] private int testAmount = 2;
        [SerializeField] private bool verboseLogs = true;

        [ContextMenu("Debug/Print AP State")]
        public void DebugPrintApState()
        {
            if (!ValidatePlayer())
                return;

            Debug.Log(
                $"{LogPrefix}\n" +
                $"CurrentAp={player.CurrentAp}\n" +
                $"ApPerRound={player.ApPerRound}\n" +
                $"IsAlive={player.IsAlive}\n" +
                $"HasCommittedTurn={player.HasCommittedTurn}\n" +
                $"CanAct={player.CanAct}");
        }

        [ContextMenu("Debug/Gain AP Direct API")]
        public void DebugGainApDirectApi()
        {
            if (!ValidatePlayer())
                return;

            int before = player.CurrentAp;
            int requested = testAmount;
            int gained = player.GainAp(requested);
            int after = player.CurrentAp;

            Debug.Log(
                $"{LogPrefix}\n" +
                $"Before={before}\n" +
                $"Requested={requested}\n" +
                $"ActualGained={gained}\n" +
                $"After={after}\n" +
                $"ApPerRound={player.ApPerRound}");
        }

        [ContextMenu("Debug/Resolve Gain AP Test Card")]
        public void DebugResolveGainApTestCard()
        {
            if (!ValidatePlayer() || !ValidateResolver() || testCard == null)
            {
                if (testCard == null)
                    Debug.LogError($"{LogPrefix} Test CardData reference is missing.");
                return;
            }

            int before = player.CurrentAp;
            var card = new CardInstance(testCard);
            var enemies = enemyActionSystem != null ? enemyActionSystem.Enemies : null;
            var context = new CardPlayContext(player, card, enemies);
            var result = cardResolver.Resolve(context);

            Debug.Log(
                $"{LogPrefix} Resolve test card '{testCard.DisplayName}'\n" +
                $"Before={before}\n" +
                $"After={player.CurrentAp}\n" +
                $"UsedEffectsPipeline={result.UsedEffectsPipeline}\n" +
                $"RequestedDraw={result.RequestedDrawCount}");
        }

        [ContextMenu("Debug/Validate Gain AP Arithmetic")]
        public void DebugValidateGainApArithmetic()
        {
            if (!ValidatePlayer())
                return;

            int before = player.CurrentAp;
            int apPerRound = player.ApPerRound;
            int requested = Mathf.Max(0, testAmount);
            int gained = player.GainAp(requested);
            int after = player.CurrentAp;
            int expected = before + gained;

            bool canGain = player.IsAlive && !player.HasCommittedTurn && requested > 0;
            bool pass = after == expected &&
                        ((canGain && gained > 0 && gained <= requested) || (!canGain && gained == 0));

            if (verboseLogs)
            {
                Debug.Log(
                    $"{LogPrefix} Arithmetic\n" +
                    $"Before={before}\n" +
                    $"Requested={requested}\n" +
                    $"ActualGained={gained}\n" +
                    $"After={after}\n" +
                    $"ApPerRound={apPerRound}\n" +
                    $"AboveApPerRound={(after > apPerRound)}");
            }

            if (pass)
                Debug.Log($"{LogPrefix} PASS — Gain AP arithmetic valid");
            else
                Debug.LogError($"{LogPrefix} FAIL — Gain AP arithmetic mismatch");
        }

        private bool ValidatePlayer()
        {
            if (player != null)
                return true;

            Debug.LogError($"{LogPrefix} PlayerBattleUnit reference is missing.");
            return false;
        }

        private bool ValidateResolver()
        {
            if (cardResolver != null)
                return true;

            Debug.LogError($"{LogPrefix} CardResolver reference is missing.");
            return false;
        }
    }
}
```

## FILE: CardEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/CardEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing base class for effect-driven card behavior.
    /// </summary>
    public abstract class CardEffectData : ScriptableObject
    {
        public abstract string GetDescriptionText();
        public abstract void Apply(CardPlayContext context, CardEffectExecutionContext executionContext);
    }
}
```

## FILE: AddBlockEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/AddBlockEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "AddBlockEffect", menuName = "Card Battle/Effects/Add Block")]
    public class AddBlockEffectData : CardEffectData
    {
        [SerializeField] private int blockAmount = 5;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, blockAmount);
            return $"Gain <color=#B0966E>{value} Block</color>";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            context.Player.AddBlock(Mathf.Max(0, blockAmount));
        }
    }
}
```

## FILE: ApplyStatusEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/ApplyStatusEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "Card Battle/Effects/Apply Status")]
    public class ApplyStatusEffectData : CardEffectData
    {
        [SerializeField] private StatusEffectType statusType = StatusEffectType.Vulnerable;
        [SerializeField] private int amount = 1;
        [SerializeField] private StatusDurationType durationType = StatusDurationType.Turn;
        [SerializeField] private int duration = 1;

        [Header("Target Override")]
        [SerializeField] private bool forceApplyToPlayer = false;

        [Header("Turn Timing")]
        [SerializeField] private bool skipNextTurnTick = false;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        public override string GetDescriptionText()
        {
            if (IsSelfTarget())
            {
                return statusType switch
                {
                    StatusEffectType.Strength =>
                        $"Gain <color=#B0966E>Strength {amount}</color>",
                    StatusEffectType.NextAttackBonus =>
                        $"Gain <color=#B0966E>Next Attack +{amount}</color>",
                    _ =>
                        $"Gain <color=#B0966E>{BuildStatusLabel()}</color> {BuildDurationText()}"
                };
            }

            return $"Apply <color=#B0966E>{statusType}</color> {BuildDurationText()}";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context == null)
                return;

            if (amount <= 0 && statusType != StatusEffectType.Weak && statusType != StatusEffectType.Vulnerable)
                return;

            if (forceApplyToPlayer)
            {
                ApplyToUnit(context.Player);
                return;
            }

            var targetMode = context.Card?.Data != null
                ? context.Card.Data.TargetMode
                : CardTargetMode.None;

            switch (targetMode)
            {
                case CardTargetMode.Self:
                    ApplyToUnit(context.Player);
                    break;

                case CardTargetMode.SingleEnemy:
                case CardTargetMode.AllEnemies:
                    if (executionContext?.EnemyTargets == null)
                        return;

                    for (int i = 0; i < executionContext.EnemyTargets.Count; i++)
                        ApplyToUnit(executionContext.EnemyTargets[i]);
                    break;

                case CardTargetMode.None:
                default:
                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            $"ApplyStatusEffectData: Target mode {targetMode} cannot apply status {statusType}.",
                            this);
                    }
                    break;
            }
        }

        private void ApplyToUnit(BattleUnit unit)
        {
            if (unit == null || !unit.IsAlive)
                return;

            int resolvedAmount = ResolveAmount();

            if (resolvedAmount <= 0)
                return;

            unit.ApplyStatus(
                statusType,
                resolvedAmount,
                durationType,
                ResolveDuration(),
                skipNextTurnTick);

            if (verboseLogs)
            {
                string skipNote = skipNextTurnTick && durationType == StatusDurationType.Turn
                    ? " (delayed tick)"
                    : string.Empty;

                Debug.Log(
                    $"ApplyStatusEffectData: Applied {statusType} {resolvedAmount} to {unit.name}{skipNote}.",
                    this);
            }
        }
       
        private int ResolveAmount()
        {
            switch (statusType)
            {
                case StatusEffectType.Weak:
                case StatusEffectType.Vulnerable:
                    return Mathf.Max(1, amount);

                default:
                    return Mathf.Max(0, amount);
            }
        }

        private int ResolveDuration()
        {
            if (durationType == StatusDurationType.Encounter)
                return Mathf.Max(0, duration);

            return Mathf.Max(1, duration);
        }

        private bool IsSelfTarget()
        {
            return forceApplyToPlayer
                || statusType == StatusEffectType.Strength
                || statusType == StatusEffectType.NextAttackBonus;
        }

        private string BuildDurationText()
        {
            int resolvedDuration = ResolveDuration();

            return durationType switch
            {
                StatusDurationType.Encounter => "for this encounter",
                StatusDurationType.Turn => resolvedDuration == 1
                    ? "for 1 turn"
                    : $"for {resolvedDuration} turns",
                StatusDurationType.UseCount => resolvedDuration == 1
                    ? "for 1 use"
                    : $"for {resolvedDuration} uses",
                StatusDurationType.OwnerAction => resolvedDuration == 1
                    ? "for 1 action"
                    : $"for {resolvedDuration} actions",
                _ => string.Empty
            };
        }

        private string BuildStatusLabel()
        {
            return statusType switch
            {
                StatusEffectType.Strength => $"Strength {amount}",
                StatusEffectType.NextAttackBonus => $"Next Attack +{amount}",
                _ => statusType.ToString()
            };
        }
    }
}
```

## FILE: DealDamageEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/DealDamageEffectData.cs`
```csharp
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
            return $"Deal <color=#B0966E>{value} damage</color>";
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
                int hpDamage = target.TakeAttackDamage(context.Player, totalDamage);

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
```

## FILE: DrawCardsEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/DrawCardsEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "DrawCardsEffect", menuName = "Card Battle/Effects/Draw Cards")]
    public class DrawCardsEffectData : CardEffectData
    {
        [SerializeField] private int amount = 2;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, amount);
            if (value == 1)
                return "Draw 1 card";

            return $"Draw {value} cards";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (executionContext == null)
                return;

            executionContext.RequestDraw(Mathf.Max(0, amount));
        }
    }
}
```

## FILE: GainApEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/GainApEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "GainApEffect",
        menuName = "Card Battle/Effects/Gain AP")]
    public class GainApEffectData : CardEffectData
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private bool verboseLogs;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, amount);
            return $"Gain <color=#B0966E>{value} AP</color>";
        }

        public override void Apply(
            CardPlayContext context,
            CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            int requested = Mathf.Max(0, amount);
            if (requested <= 0)
                return;

            int gained = context.Player.GainAp(requested);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[GainApEffect] Requested={requested} | " +
                    $"Gained={gained} | CurrentAP={context.Player.CurrentAp}");
            }
        }
    }
}
```

## FILE: HealEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/HealEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Card Battle/Effects/Heal")]
    public class HealEffectData : CardEffectData
    {
        [SerializeField] private int healAmount = 2;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, healAmount);
            return $"Heal <color=#B0966E>{value}</color>";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            context.Player.Heal(Mathf.Max(0, healAmount));
        }
    }
}
```

## FILE: CardDrawResult.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardDrawResult.cs`
```csharp
namespace CardBattle.Core
{
    /// <summary>
    /// Outcome of a draw operation against the current draw pile / hand capacity rules.
    /// </summary>
    public readonly struct CardDrawResult
    {
        /// <summary>How many cards the caller asked to draw.</summary>
        public int RequestedCount { get; }

        /// <summary>How many cards were actually removed from the draw pile.</summary>
        public int DrawnCount { get; }

        /// <summary>How many of the drawn cards entered the hand.</summary>
        public int AddedToHandCount { get; }

        /// <summary>
        /// How many drawn cards exceeded hand capacity and were routed to overflow
        /// (pending or already committed to the graveyard, depending on the API).
        /// </summary>
        public int OverflowedToGraveyardCount { get; }

        public CardDrawResult(
            int requestedCount,
            int drawnCount,
            int addedToHandCount,
            int overflowedToGraveyardCount)
        {
            RequestedCount = requestedCount;
            DrawnCount = drawnCount;
            AddedToHandCount = addedToHandCount;
            OverflowedToGraveyardCount = overflowedToGraveyardCount;
        }

        public static CardDrawResult Empty(int requestedCount)
        {
            return new CardDrawResult(requestedCount, 0, 0, 0);
        }
    }
}
```

## FILE: CardEffectExecutionContext.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardEffectExecutionContext.cs`
```csharp
using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Runtime execution payload containing resolved targets for one effect application.
    /// Also accumulates deferred draw requests for the battle runner to present later.
    /// </summary>
    public class CardEffectExecutionContext
    {
        private int requestedDrawCount;

        public IReadOnlyList<EnemyBattleUnit> EnemyTargets { get; }
        public int RequestedDrawCount => requestedDrawCount;
        public bool HasDrawRequest => requestedDrawCount > 0;

        public CardEffectExecutionContext(IReadOnlyList<EnemyBattleUnit> enemyTargets)
        {
            EnemyTargets = enemyTargets;
            requestedDrawCount = 0;
        }

        /// <summary>Accumulates a deferred draw request. Ignores amount &lt;= 0.</summary>
        public void RequestDraw(int amount)
        {
            if (amount <= 0)
                return;

            requestedDrawCount += amount;
        }
    }
}
```

## FILE: CardInstance.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardInstance.cs`
```csharp
using System;
using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Runtime card in a pile (deck / hand / graveyard). Holds a reference to static <see cref="CardData"/>
    /// plus optional modifiers for upgrades and temporary effects.
    /// </summary>
    public class CardInstance
    {
        public CardData Data { get; }
        public Guid InstanceId { get; }

        private readonly List<ICardModifier> _modifiers = new List<ICardModifier>();

        public IReadOnlyList<ICardModifier> Modifiers => _modifiers;

        public CardInstance(CardData data, Guid? instanceId = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            InstanceId = instanceId ?? Guid.NewGuid();
        }

        public void AddModifier(ICardModifier modifier)
        {
            if (modifier != null)
                _modifiers.Add(modifier);
        }

        public void ClearModifiers() => _modifiers.Clear();
    }
}
```

## FILE: CardPlayContext.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardPlayContext.cs`
```csharp
using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>Mutable snapshot passed through resolution so modifiers can read/write shared battle state.</summary>
    public class CardPlayContext
    {
        public PlayerBattleUnit Player { get; }
        public CardInstance Card { get; }
        public IReadOnlyList<EnemyBattleUnit> Enemies { get; }
        public EnemyBattleUnit PrimaryTarget { get; set; }

        /// <summary>Set to false by modifiers to skip default type handling (e.g. replaced entirely by an upgrade).</summary>
        public bool ApplyBaseCardLogic { get; set; } = true;

        public CardPlayContext(PlayerBattleUnit player, CardInstance card, IReadOnlyList<EnemyBattleUnit> enemies, EnemyBattleUnit primaryTarget = null)
        {
            Player = player;
            Card = card;
            Enemies = enemies;
            PrimaryTarget = primaryTarget;
        }
    }
}
```

## FILE: CardResolutionResult.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardResolutionResult.cs`
```csharp
namespace CardBattle.Core
{
    /// <summary>
    /// Immutable summary of one <see cref="CardResolver"/> resolution pass.
    /// </summary>
    public readonly struct CardResolutionResult
    {
        public bool UsedEffectsPipeline { get; }
        public int RequestedDrawCount { get; }
        public bool HasDrawRequest => RequestedDrawCount > 0;

        public CardResolutionResult(bool usedEffectsPipeline, int requestedDrawCount)
        {
            UsedEffectsPipeline = usedEffectsPipeline;
            RequestedDrawCount = requestedDrawCount < 0 ? 0 : requestedDrawCount;
        }

        public static CardResolutionResult Empty { get; } = new CardResolutionResult(false, 0);
    }
}
```

## FILE: ICardModifier.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/ICardModifier.cs`
```csharp
namespace CardBattle.Core
{
    /// <summary>
    /// Hook for future upgrades, relics, or temporary effects that alter how a card resolves.
    /// CardResolver can iterate modifiers before/after base resolution.
    /// </summary>
    public interface ICardModifier
    {
        /// <summary>Called before base card logic; return false to cancel further resolution for this play.</summary>
        bool PreResolve(CardPlayContext context);

        /// <summary>Called after base card logic.</summary>
        void PostResolve(CardPlayContext context);
    }
}
```

## FILE: CardResolver.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Systems/CardResolver.cs`
```csharp
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
```

## FILE: DeckController.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Systems/DeckController.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Owns the three piles (deck, hand, graveyard) and draw/discard/shuffle rules.
    /// When the deck is empty during a draw, the graveyard is shuffled back into the deck.
    /// Draw respects <see cref="MaxHandSize"/>; excess cards overflow to the graveyard
    /// (deferred via pending overflow when a multi-step draw may still reshuffle).
    /// </summary>
    public class DeckController : MonoBehaviour
    {
        [Tooltip("Optional designer list consumed by BuildFromCardDataList / BuildFromInspectorBlueprint at battle setup.")]
        [SerializeField] private List<CardData> starterDeckBlueprint = new List<CardData>();

        [Tooltip("Maximum number of cards allowed in hand. Drawn cards beyond this overflow to the graveyard.")]
        [SerializeField] private int maxHandSize = 10;

        private readonly List<CardInstance> _deck = new List<CardInstance>();
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly List<CardInstance> _graveyard = new List<CardInstance>();

        /// <summary>
        /// Cards drawn while the hand was full, held out of the graveyard until the
        /// current draw operation finishes so they cannot be reshuffled and redrawn
        /// in the same operation.
        /// </summary>
        private readonly List<CardInstance> _pendingOverflow = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Deck => _deck;
        public IReadOnlyList<CardInstance> Hand => _hand;
        public IReadOnlyList<CardInstance> Graveyard => _graveyard;

        public int MaxHandSize => Mathf.Max(0, maxHandSize);
        public int AvailableHandSpace => Mathf.Max(0, MaxHandSize - _hand.Count);
        public bool IsHandFull => AvailableHandSpace <= 0;

        /// <summary>Cards drawn past hand capacity that are not yet committed to the graveyard.</summary>
        public int PendingOverflowCount => _pendingOverflow.Count;

        /// <summary>Fired after any pile mutation so UI or VFX can subscribe later.</summary>
        public event Action OnPilesChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxHandSize = Mathf.Max(0, maxHandSize);
        }
#endif

        /// <summary>Replace runtime piles using blueprint assets (one <see cref="CardInstance"/> per entry).</summary>
        public void BuildFromCardDataList(IEnumerable<CardData> cards)
        {
            ClearAllPiles();
            if (cards == null)
                return;

            foreach (var data in cards)
            {
                if (data != null)
                    _deck.Add(new CardInstance(data));
            }

            ShuffleDeck();
            NotifyChanged();
        }

        /// <summary>Uses the serialized starter blueprint when no explicit list is provided.</summary>
        public void BuildFromInspectorBlueprint()
        {
            BuildFromCardDataList(starterDeckBlueprint);
        }

        public void ClearAllPiles()
        {
            _deck.Clear();
            _hand.Clear();
            _graveyard.Clear();
            _pendingOverflow.Clear();
            NotifyChanged();
        }

        public bool IsInHand(CardInstance card) => card != null && _hand.Contains(card);

        public int GetDeckCount()
        {
            return _deck.Count;
        }

        public int GetGraveyardCount()
        {
            return _graveyard.Count;
        }

        /// <summary>
        /// Draw up to <paramref name="count"/> cards, reshuffling graveyard into deck as needed.
        /// Overflow is held pending during the operation, then committed to the graveyard once.
        /// </summary>
        public CardDrawResult DrawCards(int count)
        {
            int requested = Mathf.Max(0, count);
            if (requested == 0)
                return CardDrawResult.Empty(0);

            int drawn = 0;
            int addedToHand = 0;
            int overflowed = 0;

            for (var i = 0; i < requested; i++)
            {
                if (_deck.Count == 0)
                    ReshuffleGraveyardIntoDeck();

                if (_deck.Count == 0)
                    break;

                if (!TryDrawTopCardFromDeck(out _, out var placedInHand))
                    break;

                drawn++;
                if (placedInHand)
                    addedToHand++;
                else
                    overflowed++;
            }

            // Commit overflow only after reshuffles for this operation are finished.
            FlushPendingOverflowToGraveyard(notify: false);
            NotifyChanged();

            return new CardDrawResult(requested, drawn, addedToHand, overflowed);
        }

        /// <summary>
        /// Draws directly from the current deck without auto-reshuffle, respecting hand capacity.
        /// Overflow is flushed to the graveyard immediately (safe default for simple callers).
        /// For presentation-driven two-phase draws, use <see cref="DrawCardsFromDeckImmediate"/>
        /// and call <see cref="FlushPendingOverflowToGraveyard"/> after the full sequence.
        /// </summary>
        public List<CardInstance> DrawCardsImmediate(int count)
        {
            DrawFromCurrentDeckCore(count, collectDrawn: true, out var drawnCards);
            FlushPendingOverflowToGraveyard(notify: false);
            NotifyChanged();
            return drawnCards;
        }

        /// <summary>
        /// Low-level draw from the current deck only: no reshuffle, respects hand limit.
        /// Overflow cards stay in pending overflow (not graveyard) so a later reshuffle
        /// in the same multi-step draw cannot pick them up.
        /// </summary>
        public CardDrawResult DrawCardsFromDeckImmediate(int count)
        {
            var result = DrawFromCurrentDeckCore(count, collectDrawn: false, out _);
            NotifyChanged();
            return result;
        }

        /// <summary>
        /// Moves pending overflow cards into the graveyard in one batch.
        /// Call after a multi-step draw that may reshuffle between immediate draws.
        /// </summary>
        public int FlushPendingOverflowToGraveyard()
        {
            return FlushPendingOverflowToGraveyard(notify: true);
        }

        /// <summary>Moves all graveyard cards into deck and shuffles. Returns moved card count.</summary>
        public int ReshuffleGraveyardIntoDeckImmediate()
        {
            int moved = _graveyard.Count;
            if (moved <= 0)
                return 0;

            _deck.AddRange(_graveyard);
            _graveyard.Clear();
            ShuffleListInPlace(_deck);
            NotifyChanged();
            return moved;
        }

        /// <summary>Move every card from hand to graveyard (end of player turn).</summary>
        public void DiscardEntireHand()
        {
            for (var i = _hand.Count - 1; i >= 0; i--)
                MoveToGraveyard(_hand[i]);

            NotifyChanged();
        }

        /// <summary>Play resolution: remove from hand and send to graveyard.</summary>
        public void PlayCardFromHand(CardInstance card)
        {
            if (card == null || !_hand.Remove(card))
                return;

            _graveyard.Add(card);
            NotifyChanged();
        }

        public void ShuffleDeck()
        {
            ShuffleListInPlace(_deck);
            NotifyChanged();
        }

        private CardDrawResult DrawFromCurrentDeckCore(
            int count,
            bool collectDrawn,
            out List<CardInstance> drawnCards)
        {
            int requested = Mathf.Max(0, count);
            drawnCards = collectDrawn ? new List<CardInstance>(requested) : null;

            if (requested == 0)
                return CardDrawResult.Empty(0);

            int drawn = 0;
            int addedToHand = 0;
            int overflowed = 0;

            for (int i = 0; i < requested; i++)
            {
                if (_deck.Count == 0)
                    break;

                if (!TryDrawTopCardFromDeck(out var card, out var placedInHand))
                    break;

                drawn++;
                if (placedInHand)
                    addedToHand++;
                else
                    overflowed++;

                if (collectDrawn)
                    drawnCards.Add(card);
            }

            return new CardDrawResult(requested, drawn, addedToHand, overflowed);
        }

        /// <summary>
        /// Removes the top deck card and places it in hand or pending overflow.
        /// Does not reshuffle, notify, or flush overflow.
        /// </summary>
        private bool TryDrawTopCardFromDeck(out CardInstance card, out bool placedInHand)
        {
            card = null;
            placedInHand = false;
            if (_deck.Count == 0)
                return false;

            int index = _deck.Count - 1;
            card = _deck[index];
            _deck.RemoveAt(index);

            if (_hand.Count < MaxHandSize)
            {
                _hand.Add(card);
                placedInHand = true;
            }
            else
            {
                _pendingOverflow.Add(card);
            }

            return true;
        }

        private int FlushPendingOverflowToGraveyard(bool notify)
        {
            int moved = _pendingOverflow.Count;
            if (moved <= 0)
                return 0;

            for (int i = 0; i < _pendingOverflow.Count; i++)
            {
                var overflowCard = _pendingOverflow[i];
                if (overflowCard != null && !_graveyard.Contains(overflowCard))
                    _graveyard.Add(overflowCard);
            }

            _pendingOverflow.Clear();

            if (notify)
                NotifyChanged();

            return moved;
        }

        private void ReshuffleGraveyardIntoDeck()
        {
            // Intentionally ignores _pendingOverflow so overflow from this draw
            // cannot re-enter the deck mid-operation.
            if (_graveyard.Count == 0)
                return;

            _deck.AddRange(_graveyard);
            _graveyard.Clear();
            ShuffleListInPlace(_deck);
        }

        private static void ShuffleListInPlace(IList<CardInstance> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void MoveToGraveyard(CardInstance card)
        {
            if (card == null)
                return;

            _hand.Remove(card);
            _deck.Remove(card);
            _pendingOverflow.Remove(card);
            if (!_graveyard.Contains(card))
                _graveyard.Add(card);
        }

        private void NotifyChanged() => OnPilesChanged?.Invoke();
    }
}
```

## FILE: CardDescriptionBuilder.cs
**Path:** `Assets/Scripts/CardBattle/Cards/UI/CardDescriptionBuilder.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public static class CardDescriptionBuilder
    {
        public static string Build(CardData data)
        {
            if (data == null)
                return string.Empty;

            if (data.HasEffects)
            {
                var lines = new List<string>();
                var effects = data.Effects;

                if (effects != null)
                {
                    for (int i = 0; i < effects.Count; i++)
                    {
                        var effect = effects[i];
                        if (effect == null)
                            continue;

                        string line = effect.GetDescriptionText();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        lines.Add(line);
                    }
                }

                if (lines.Count > 0)
                    return string.Join("\n", lines);
            }

            return BuildLegacy(data);
        }

        private static string BuildLegacy(CardData data)
        {
            if (data == null)
                return string.Empty;

            switch (data.CardType)
            {
                case CardType.Attack:
                    return $"Deal <color=#D4AB6B>{Mathf.Max(0, data.AttackDamage)} damage</color>";
                case CardType.Heal:
                    return $"Heal <color=#D4AB6B>{Mathf.Max(0, data.HealAmount)}</color>";
                case CardType.Buff:
                    return $"Gain <color=#D4AB6B>+{data.BuffPotency}</color>";
                case CardType.Defend:
                    return $"Gain <color=#D4AB6B>{Mathf.Max(0, data.BlockAmount)} Block</color>";
                default:
                    return string.Empty;
            }
        }
    }
}
```

## FILE: CardTypeBadgeSet.cs
**Path:** `Assets/Scripts/CardBattle/Cards/UI/CardTypeBadgeSet.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "CardTypeBadgeSet", menuName = "Card Battle/Visuals/Card Type Badge Set")]
    public class CardTypeBadgeSet : ScriptableObject
    {
        [Header("Card Type Badges")]
        [SerializeField] private Sprite attackBadge;
        [SerializeField] private Sprite defendBadge;
        [SerializeField] private Sprite healBadge;
        [SerializeField] private Sprite buffBadge;

        public Sprite GetBadge(CardType type)
        {
            switch (type)
            {
                case CardType.Attack:
                    return attackBadge;

                case CardType.Defend:
                    return defendBadge;

                case CardType.Heal:
                    return healBadge;

                case CardType.Buff:
                    return buffBadge;

                default:
                    return null;
            }
        }
    }
}
```

## FILE: CardViewUI.cs
**Path:** `Assets/Scripts/CardBattle/Cards/UI/CardViewUI.cs`
```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class CardViewUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum CardVisualState
        {
            Normal,
            Hovered,
            Selected,
            Disabled
        }

        [Header("UI References")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image typeBadgeImage;
        [SerializeField] private CardTypeBadgeSet typeBadgeSet;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Core References")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private RectTransform guideStartAnchor;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button button;

        [Header("Pointer Hit Area")]
        [Tooltip("Transparent Image on the CardViewUI root. Stays fixed in the fan slot while VisualRoot moves.")]
        [SerializeField] private Graphic pointerHitTarget;

        [Header("Visual Tuning")]
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float hoveredScale = 1.08f;
        [SerializeField] private float selectedScale = 1.14f;
        [SerializeField] private float disabledScale = 0.96f;

        [SerializeField] private float normalAlpha = 1f;
        [SerializeField] private float disabledAlpha = 0.5f;

        [SerializeField] private float hoveredYOffset = 25f;
        [SerializeField] private float selectedYOffset = 40f;
        private float hoveredXOffset;
        private float selectedXOffset;

        [SerializeField] private float scaleLerpSpeed = 12f;
        [SerializeField] private float moveLerpSpeed = 12f;
        [SerializeField] private float layoutLerpSpeed = 12f;
        [SerializeField] private float rotationLerpSpeed = 14f;

        [Header("Deal-In Presentation")]
        [SerializeField] private bool useDealFadeIn = true;
        [SerializeField] private float dealSpawnAlpha = 0f;
        [SerializeField] private float dealFadeDuration = 0.12f;

        private CardInstance boundCard;
        private CardVisualState currentState = CardVisualState.Normal;

        private RectTransform _rectTransform;
        private Vector2 targetLayoutAnchoredPos;
        private float _layoutRotationZ;
        private float targetRotationZ;

        private Vector3 baseScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;

        private Vector3 baseLocalPosition = Vector3.zero;
        private Vector3 targetLocalPosition = Vector3.zero;

        private bool isInteractable = true;
        private bool isSelected = false;
        private bool isPointerOver;
        private bool layoutMovementBlocked;
        private bool pendingDealFadeIn;
        private Coroutine dealFadeRoutine;

        public CardInstance BoundCard => boundCard;
        /// <summary>Root layout rect (anchored fan position). Used by presentation VFX.</summary>
        public RectTransform LayoutRect => _rectTransform;
        public RectTransform GuideStartAnchor => guideStartAnchor;
        public bool IsSelected => isSelected;
        public bool IsInteractable => isInteractable;
        public bool IsPointerOver => isPointerOver;
        public bool IsDealPresentationPending => layoutMovementBlocked || pendingDealFadeIn || dealFadeRoutine != null;

        public event System.Action<CardViewUI> OnHoverStarted;
        public event System.Action<CardViewUI> OnHoverEnded;

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform.Find("VisualRoot") as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (button == null)
                button = GetComponent<Button>();

            if (visualRoot != null)
            {
                baseScale = visualRoot.localScale;
                targetScale = baseScale * normalScale;

                baseLocalPosition = visualRoot.localPosition;
                targetLocalPosition = baseLocalPosition;

                targetRotationZ = visualRoot.localEulerAngles.z;
                _layoutRotationZ = targetRotationZ;
            }

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
                targetLayoutAnchoredPos = _rectTransform.anchoredPosition;

            // Keep pointer hits on the fixed fan slot (root). Visual raise/scale must not
            // expand the raycast area and block adjacent cards in large hands.
            ConfigureStablePointerHitTarget();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (pointerHitTarget == null)
                pointerHitTarget = GetComponent<Graphic>();

            if (pointerHitTarget == null)
            {
                Debug.LogWarning(
                    "[CardViewUI] Pointer hit target is missing. " +
                    "Assign a transparent Image on the CardViewUI root.",
                    this);
            }
        }
#endif

        /// <summary>
        /// Disables raycasts on VisualRoot graphics and routes hover/click through
        /// a dedicated root pointerHitTarget that does not move with hover presentation.
        /// </summary>
        private void ConfigureStablePointerHitTarget()
        {
            if (pointerHitTarget == null)
                pointerHitTarget = GetComponent<Graphic>();

            if (visualRoot != null)
            {
                Graphic[] visualGraphics = visualRoot.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < visualGraphics.Length; i++)
                {
                    Graphic graphic = visualGraphics[i];
                    if (graphic != null && graphic != pointerHitTarget)
                        graphic.raycastTarget = false;
                }
            }

            if (pointerHitTarget == null)
            {
                Debug.LogError(
                    "[CardViewUI] Pointer hit target is missing. " +
                    "Assign a transparent Image on the CardViewUI root.",
                    this);
                return;
            }

            pointerHitTarget.raycastTarget = true;

            if (button != null)
                button.targetGraphic = pointerHitTarget;
        }

        private void OnDisable()
        {
            StopDealFadeRoutine();
        }

        /// <summary>Tuning from <see cref="HandUIController"/> so hand and card share one layout motion speed.</summary>
        public void SetLayoutLerpSpeed(float speed)
        {
            layoutLerpSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>Targets root layout position and idle fan rotation; motion is smoothed in <see cref="Update"/>.</summary>
        public void SetLayoutPose(Vector2 anchoredPos, float rotationZ)
        {
            targetLayoutAnchoredPos = anchoredPos;
            _layoutRotationZ = rotationZ;
            SyncRotationTargetToState();
        }

        /// <summary>
        /// Sets an immediate spawn pose before the normal layout lerp pulls this card toward its target slot.
        /// </summary>
        public void PrepareForDealIn(Vector2 startAnchoredPos, float startRotationZ, float startScale)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
                _rectTransform.anchoredPosition = startAnchoredPos;

            if (visualRoot != null)
            {
                float clampedScale = Mathf.Max(0.01f, startScale);
                visualRoot.localScale = baseScale * clampedScale;
                visualRoot.localEulerAngles = new Vector3(0f, 0f, startRotationZ);
            }

            StopDealFadeRoutine();
            pendingDealFadeIn = useDealFadeIn && canvasGroup != null;
            if (pendingDealFadeIn && canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(dealSpawnAlpha);
        }

        public void SetLayoutMovementBlocked(bool value)
        {
            bool wasBlocked = layoutMovementBlocked;
            layoutMovementBlocked = value;

            if (layoutMovementBlocked)
            {
                StopDealFadeRoutine();
                return;
            }

            if (wasBlocked && pendingDealFadeIn)
                StartDealFadeIn();
        }

        public void ForceCompleteDealPresentation()
        {
            layoutMovementBlocked = false;
            pendingDealFadeIn = false;

            StopDealFadeRoutine();

            if (canvasGroup != null)
                canvasGroup.alpha = ResolveTargetAlphaForCurrentState();

            if (visualRoot != null)
            {
                visualRoot.localScale = targetScale;
                visualRoot.localPosition = targetLocalPosition;
                visualRoot.localEulerAngles = new Vector3(0f, 0f, targetRotationZ);
            }
        }

        private void SyncRotationTargetToState()
        {
            if (currentState == CardVisualState.Hovered || currentState == CardVisualState.Selected)
                targetRotationZ = 0f;
            else
                targetRotationZ = _layoutRotationZ;
        }

        private void Update()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
            {
                if (!layoutMovementBlocked)
                {
                    _rectTransform.anchoredPosition = Vector2.Lerp(
                        _rectTransform.anchoredPosition,
                        targetLayoutAnchoredPos,
                        Time.deltaTime * layoutLerpSpeed
                    );
                }
            }

            if (visualRoot == null)
                return;

            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                targetScale,
                Time.deltaTime * scaleLerpSpeed
            );

            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                targetLocalPosition,
                Time.deltaTime * moveLerpSpeed
            );

            float currentZ = visualRoot.localEulerAngles.z;
            float newZ = Mathf.LerpAngle(currentZ, targetRotationZ, Time.deltaTime * rotationLerpSpeed);
            visualRoot.localEulerAngles = new Vector3(0f, 0f, newZ);
        }

        public void Bind(CardInstance card)
        {
            boundCard = card;

            if (card?.Data == null)
                return;

            var data = card.Data;

            if (costText != null)
                costText.text = data.ApCost.ToString();

            if (nameText != null)
                nameText.text = data.DisplayName;

            if (typeBadgeImage != null)
            {
                Sprite badge = typeBadgeSet != null ? typeBadgeSet.GetBadge(data.CardType) : null;

                typeBadgeImage.sprite = badge;
                typeBadgeImage.enabled = badge != null;
            }

            if (artworkImage != null)
                artworkImage.sprite = data.Artwork;

            if (descriptionText != null)
                descriptionText.text = CardDescriptionBuilder.Build(data);

            ApplyStateVisuals();
        }

        /// <summary>Artwork sprite for flying ghost VFX (presentation only).</summary>
        public Sprite GetArtworkSnapshotForVfx()
        {
            return artworkImage != null ? artworkImage.sprite : null;
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;

            if (button != null)
                button.interactable = value;

            if (!isInteractable)
            {
                isSelected = false;
                currentState = CardVisualState.Disabled;
            }
            else
            {
                currentState = isSelected
                    ? CardVisualState.Selected
                    : (isPointerOver ? CardVisualState.Hovered : CardVisualState.Normal);
            }

            ApplyStateVisuals();
        }

        public void SetClickAction(UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();

            if (action != null)
            {
                button.onClick.AddListener(() =>
                {
                    Select();
                    action.Invoke();
                });
            }
        }

        public void Select()
        {
            if (!isInteractable)
                return;

            isSelected = true;
            currentState = CardVisualState.Selected;
            ApplyStateVisuals();
        }

        public void Deselect()
        {
            isSelected = false;

            if (!isInteractable)
                currentState = CardVisualState.Disabled;
            else if (isPointerOver)
            {
                currentState = CardVisualState.Hovered;
                OnHoverStarted?.Invoke(this);
            }
            else
                currentState = CardVisualState.Normal;

            ApplyStateVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;

            if (!isInteractable || isSelected)
                return;

            OnHoverStarted?.Invoke(this);

            currentState = CardVisualState.Hovered;
            ApplyStateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;

            if (!isInteractable)
                return;

            if (isSelected)
                return;

            OnHoverEnded?.Invoke(this);

            currentState = CardVisualState.Normal;
            ApplyStateVisuals();
        }

        /// <summary>
        /// Applies hand-level hover/selected presentation tuning without recreating the view.
        /// X offsets move VisualRoot only — the root pointer hit area stays fixed.
        /// </summary>
        public void ApplyHandPresentationTuning(
            float hoverRaiseY,
            float hoverScaleMul,
            float selectedRaiseY,
            float selectedScaleMul,
            float hoverOffsetX = 0f,
            float selectedOffsetX = 0f)
        {
            hoveredYOffset = Mathf.Max(0f, hoverRaiseY);
            hoveredScale = Mathf.Max(1f, hoverScaleMul);
            selectedYOffset = Mathf.Max(0f, selectedRaiseY);
            selectedScale = Mathf.Max(1f, selectedScaleMul);
            hoveredXOffset = hoverOffsetX;
            selectedXOffset = selectedOffsetX;
            ApplyStateVisuals();
        }

        private void ApplyStateVisuals()
        {
            if (visualRoot == null)
                return;

            SyncRotationTargetToState();

            switch (currentState)
            {
                case CardVisualState.Normal:
                    targetScale = baseScale * normalScale;
                    targetLocalPosition = baseLocalPosition;
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Hovered:
                    targetScale = baseScale * hoveredScale;
                    targetLocalPosition = baseLocalPosition + new Vector3(
                        hoveredXOffset,
                        hoveredYOffset,
                        0f);
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Selected:
                    targetScale = baseScale * selectedScale;
                    targetLocalPosition = baseLocalPosition + new Vector3(
                        selectedXOffset,
                        selectedYOffset,
                        0f);
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Disabled:
                    targetScale = baseScale * disabledScale;
                    targetLocalPosition = baseLocalPosition;
                    SetAlpha(disabledAlpha);
                    break;
            }
        }

        private void SetAlpha(float value)
        {
            if (canvasGroup != null)
            {
                if (layoutMovementBlocked && pendingDealFadeIn)
                    canvasGroup.alpha = Mathf.Min(Mathf.Clamp01(value), Mathf.Clamp01(dealSpawnAlpha));
                else
                    canvasGroup.alpha = value;
            }
        }

        private void StartDealFadeIn()
        {
            if (canvasGroup == null)
            {
                pendingDealFadeIn = false;
                return;
            }

            StopDealFadeRoutine();
            dealFadeRoutine = StartCoroutine(CoDealFadeIn(ResolveDealFadeTargetAlpha()));
        }

        private IEnumerator CoDealFadeIn(float targetAlpha)
        {
            float duration = Mathf.Max(0.01f, dealFadeDuration);
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            float endAlpha = Mathf.Clamp01(targetAlpha);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            pendingDealFadeIn = false;
            dealFadeRoutine = null;
        }

        private float ResolveTargetAlphaForCurrentState()
        {
            return currentState == CardVisualState.Disabled ? disabledAlpha : normalAlpha;
        }

        private float ResolveDealFadeTargetAlpha()
        {
            return normalAlpha;
        }

        private void StopDealFadeRoutine()
        {
            if (dealFadeRoutine != null)
            {
                StopCoroutine(dealFadeRoutine);
                dealFadeRoutine = null;
            }
        }

    }
}
```

## FILE: HandUIController.cs
**Path:** `Assets/Scripts/CardBattle/Cards/UI/HandUIController.cs`
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class HandUIController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;
        [SerializeField] private BattleActionRunner battleActionRunner;

        [Header("UI References")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private CardViewUI cardViewPrefab;
        [SerializeField] private RectTransform drawSpawnAnchor;

        [Header("Audio")]
        [SerializeField] private CardSFXController cardSfx;

        [Header("Options")]
        [SerializeField] private bool autoRefreshOnStart = true;
        [SerializeField] private bool disableUnplayableCards = true;
        [SerializeField] private bool verboseLogs = false;
        [SerializeField] private bool useDealPresentation = true;
        [SerializeField] private bool dealLeftToRight = true;
        [SerializeField] private float dealStagger = 0.05f;
        [SerializeField] private float newCardSpawnRotationZ = 0f;
        [SerializeField] private float newCardSpawnScale = 0.92f;

        [Header("Responsive Card Size")]
        [SerializeField] private Vector2 baseCardSize = new Vector2(200f, 300f);
        [SerializeField] private float maxCardScale = 1.1f; // 200 -> 220
        [SerializeField] private float minCardScale = 0.9f;
        [SerializeField] private int maxScaleCardCount = 5;
        [SerializeField] private int minScaleCardCount = 10;
        [SerializeField] private bool scaleSpacingWithCard = true;

        [Header("Fan layout")]
        [SerializeField] private float spacing = 135f;
        [SerializeField] private float curveHeight = 14f;
        [SerializeField] private float rotationStep = 6f;
        [SerializeField] private float hoverGap = 60f;
        [SerializeField] private float layoutLerpSpeed = 12f;

        [Header("Adaptive Fan Layout")]
        [SerializeField] private bool useAdaptiveFanLayout = true;
        [SerializeField] private float maxHandWidth = 1080f;
        [SerializeField] private float minimumSpacing = 90f;
        [SerializeField] private float maximumSpacing = 145f;
        [SerializeField] private float maxEdgeRotation = 8f;
        [SerializeField] private float maxEdgeDrop = 30f;
        [SerializeField] private float largeHandRaise = 55f;
        [SerializeField] private int largeHandStartCount = 8;

        [Header("Fixed Size Fan Layout")]
        [SerializeField] private bool useFixedSizeAdaptiveFan = true;
        [SerializeField] private float fixedCardScale = 1f;
        [SerializeField] private float preferredCenterSpacing = 140f;
        [SerializeField] private float minimumCenterSpacing = 80f;
        [SerializeField] private float hoverRaise = 150f;
        [SerializeField] private float hoverScale = 1.08f;

        [Header("Fan Strength By Hand Count")]
        [SerializeField] private bool useCustomFanStrengthByCount = true;
        [Tooltip("Index = card count. Values may exceed 1.0 (e.g. 1.12 = 112% of max edge drop/rotation).")]
        [SerializeField] private float[] fanStrengthByCardCount =
        {
            0f,    // 0
            0f,    // 1
            0.15f, // 2
            0.35f, // 3
            0.65f, // 4
            1.00f, // 5 baseline
            1.02f, // 6
            1.05f, // 7
            1.08f, // 8
            1.10f, // 9
            1.12f  // 10
        };

        [Header("Large Hand Hover")]
        [SerializeField] private int largeHandHoverStartCount = 8;
        [SerializeField] private float largeHandHoverGap = 85f;
        [SerializeField] private float largeHandHoverRaise = 175f;
        [SerializeField] private float largeHandHoverScale = 1.10f;
        [SerializeField] private float neighborPushFalloff = 0.55f;
        [SerializeField] private bool keepHoveredCardOnTop = true;
        [SerializeField] private float edgeHoverInwardOffset = 30f;

        private readonly List<CardViewUI> spawnedCards = new List<CardViewUI>();
        private CardViewUI selectedView;
        private CardViewUI hoveredCardView;
        private Coroutine dealRoutine;

        private int lastLoggedLayoutCount = -1;
        private float lastLoggedCardScale;
        private float lastLoggedCardWidth;
        private float lastLoggedPreferredSpacing;
        private float lastLoggedFitSpacing;
        private float lastLoggedSpacing;
        private float lastLoggedTotalSpan;
        private float lastLoggedTotalWidth;
        private float lastLoggedEdgeDrop;
        private float lastLoggedHandRaise;
        private float lastLoggedEdgeRotation;
        private float lastLoggedFanStrength;
        private float lastLoggedHoverGap;
        private float lastLoggedHoverRaise;
        private float lastLoggedHoverScale;
        private int lastLoggedHoveredIndex = -1;

        private void OnEnable()
        {
            if (deckController != null)
                deckController.OnPilesChanged += SyncHandViews;

            if (player != null)
            {
                player.OnApChangedEvent += HandlePlayerApChanged;
                player.OnTurnStateChanged += HandlePlayerTurnStateChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged += HandleBusyStateChanged;
        }

        private void OnDisable()
        {
            if (deckController != null)
                deckController.OnPilesChanged -= SyncHandViews;

            if (player != null)
            {
                player.OnApChangedEvent -= HandlePlayerApChanged;
                player.OnTurnStateChanged -= HandlePlayerTurnStateChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;

            if (dealRoutine != null)
                StopDealRoutineAndReleaseLocks();
        }

        private void Start()
        {
            if (autoRefreshOnStart)
                RefreshHandUI();
        }

        private float GetResponsiveCardScale(int count)
        {
            float t = Mathf.InverseLerp(maxScaleCardCount, minScaleCardCount, count);
            return Mathf.Lerp(maxCardScale, minCardScale, t);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxHandWidth = Mathf.Max(1f, maxHandWidth);
            minimumSpacing = Mathf.Max(0.01f, minimumSpacing);
            maximumSpacing = Mathf.Max(minimumSpacing, maximumSpacing);
            maxEdgeRotation = Mathf.Max(0f, maxEdgeRotation);
            maxEdgeDrop = Mathf.Max(0f, maxEdgeDrop);
            largeHandRaise = Mathf.Max(0f, largeHandRaise);
            largeHandStartCount = Mathf.Max(2, largeHandStartCount);
            minCardScale = Mathf.Max(0.01f, minCardScale);
            maxCardScale = Mathf.Max(minCardScale, maxCardScale);
            spacing = Mathf.Max(0.01f, spacing);
            hoverGap = Mathf.Max(0f, hoverGap);
            curveHeight = Mathf.Max(0f, curveHeight);
            layoutLerpSpeed = Mathf.Max(0f, layoutLerpSpeed);

            fixedCardScale = Mathf.Max(0.01f, fixedCardScale);
            preferredCenterSpacing = Mathf.Max(0.01f, preferredCenterSpacing);
            minimumCenterSpacing = Mathf.Max(0.01f, minimumCenterSpacing);
            if (minimumCenterSpacing > preferredCenterSpacing)
                minimumCenterSpacing = preferredCenterSpacing;
            hoverRaise = Mathf.Max(0f, hoverRaise);
            hoverScale = Mathf.Max(1f, hoverScale);

            largeHandHoverStartCount = Mathf.Max(2, largeHandHoverStartCount);
            largeHandHoverGap = Mathf.Max(hoverGap, largeHandHoverGap);
            largeHandHoverRaise = Mathf.Max(hoverRaise, largeHandHoverRaise);
            largeHandHoverScale = Mathf.Max(hoverScale, largeHandHoverScale);
            neighborPushFalloff = Mathf.Clamp01(neighborPushFalloff);
            edgeHoverInwardOffset = Mathf.Max(0f, edgeHoverInwardOffset);

            EnsureFanStrengthArraySize();
            if (fanStrengthByCardCount != null)
            {
                for (int i = 0; i < fanStrengthByCardCount.Length; i++)
                    fanStrengthByCardCount[i] = Mathf.Max(0f, fanStrengthByCardCount[i]);
            }
        }
#endif

        private void EnsureFanStrengthArraySize()
        {
            const int needed = 11; // indices 0..10
            if (fanStrengthByCardCount != null && fanStrengthByCardCount.Length >= needed)
                return;

            float[] defaults =
            {
                0f, 0f, 0.15f, 0.35f, 0.65f, 1.00f, 1.02f, 1.05f, 1.08f, 1.10f, 1.12f
            };

            if (fanStrengthByCardCount == null || fanStrengthByCardCount.Length == 0)
            {
                fanStrengthByCardCount = defaults;
                return;
            }

            var expanded = new float[needed];
            for (int i = 0; i < needed; i++)
            {
                if (i < fanStrengthByCardCount.Length)
                    expanded[i] = Mathf.Max(0f, fanStrengthByCardCount[i]);
                else
                    expanded[i] = defaults[i];
            }

            fanStrengthByCardCount = expanded;
        }

        [ContextMenu("Debug/Print Adaptive Hand Layout")]
        private void DebugPrintAdaptiveHandLayout()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[AdaptiveHandLayout] No cards in hand.");
                return;
            }

            LayoutCards();
            LogAdaptiveHandLayout(force: true);
        }

        [ContextMenu("Debug/Print Fixed Fan Layout")]
        private void DebugPrintFixedFanLayout()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[FixedFanLayout] No cards in hand.");
                return;
            }

            LayoutCards();
            LogFixedFanLayout(force: true);
        }

        [ContextMenu("Debug/Print Fan Strength")]
        private void DebugPrintFanStrength()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[FanStrength] No cards in hand.");
                return;
            }

            LayoutCards();
            LogFanStrength(force: true);
        }

        [ContextMenu("Debug/Print Current Fan And Hover Settings")]
        private void DebugPrintCurrentFanAndHoverSettings()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[HandPresentation] No cards in hand.");
                return;
            }

            LayoutCards();
            LogHandPresentation(force: true);
        }

        [ContextMenu("Refresh Hand UI")]
        public void RefreshHandUI()
        {
            if (!ValidateReferences())
                return;

            ClearSpawnedCards();
            selectedView = null;
            hoveredCardView = null;

            var hand = deckController.Hand;
            var newlyCreatedViews = new List<CardViewUI>(hand.Count);
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                var view = CreateCardView(card);
                spawnedCards.Add(view);
                newlyCreatedViews.Add(view);
            }

            bool shouldPlayDeal = useDealPresentation && newlyCreatedViews.Count > 0;
            if (shouldPlayDeal)
                PrepareNewCardsForDeal(newlyCreatedViews);

            RefreshCardInteractivity();
            LayoutCards();

            if (shouldPlayDeal)
                dealRoutine = StartCoroutine(CoDealInNewCards(newlyCreatedViews));
        }

        /// <summary>
        /// Incremental sync wrapper for external systems. Preserves existing CardViews
        /// and only creates views for newly added hand cards.
        /// </summary>
        public void SyncHandViewsExternal()
        {
            SyncHandViews();
        }

        public bool IsDealPresentationRunning => dealRoutine != null;

        /// <summary>
        /// Waits until the current deal-in presentation finishes.
        /// Returns immediately when no deal is running or this component is disabled.
        /// </summary>
        public IEnumerator WaitForDealPresentationComplete()
        {
            while (dealRoutine != null && isActiveAndEnabled)
                yield return null;
        }

        /// <summary>Syncs list of card views with the deck hand without rebuilding views for cards that are still in hand.</summary>
        private void SyncHandViews()
        {
            if (!ValidateReferences())
                return;

            StopDealRoutineAndReleaseLocks();

            for (int i = spawnedCards.Count - 1; i >= 0; i--)
            {
                var view = spawnedCards[i];
                if (view == null)
                {
                    spawnedCards.RemoveAt(i);
                    continue;
                }

                var bound = view.BoundCard;
                if (bound == null || bound.Data == null || !deckController.IsInHand(bound))
                    RemoveView(view);
            }

            var hand = deckController.Hand;
            var used = new HashSet<CardViewUI>();
            var newOrder = new List<CardViewUI>(hand.Count);
            var newlyCreatedViews = new List<CardViewUI>();

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                CardViewUI view = null;
                for (int j = 0; j < spawnedCards.Count; j++)
                {
                    var candidate = spawnedCards[j];
                    if (candidate != null && !used.Contains(candidate) && candidate.BoundCard == card)
                    {
                        view = candidate;
                        break;
                    }
                }

                bool createdNew = false;
                if (view != null)
                {
                    used.Add(view);
                }
                else
                {
                    view = CreateCardView(card);
                    newlyCreatedViews.Add(view);
                    createdNew = true;
                }

                newOrder.Add(view);

                if (verboseLogs && createdNew)
                {
                    Debug.Log(
                        "[IncrementalHandDraw]\n" +
                        $"Card={card.Data.DisplayName}\n" +
                        "CreatedNewView=True\n" +
                        $"HandIndex={newOrder.Count - 1}\n" +
                        $"VisibleViews={newOrder.Count}");
                }
            }

            spawnedCards.Clear();
            spawnedCards.AddRange(newOrder);

            bool shouldPlayDeal = useDealPresentation && newlyCreatedViews.Count > 0;
            if (shouldPlayDeal)
                PrepareNewCardsForDeal(newlyCreatedViews);

            RefreshCardInteractivity();
            LayoutCards();

            if (shouldPlayDeal)
                dealRoutine = StartCoroutine(CoDealInNewCards(newlyCreatedViews));
        }

        private bool HasViewForCard(CardInstance card)
        {
            if (card == null)
                return false;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null && v.BoundCard == card)
                    return true;
            }

            return false;
        }

        /// <summary>Returns the visible hand view for a card, if any (for presentation VFX before pile sync removes it).</summary>
        public CardViewUI GetViewForCard(CardInstance card)
        {
            if (card == null)
                return null;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null && v.BoundCard == card)
                    return v;
            }

            return null;
        }

        /// <summary>Copy of current hand views for batch graveyard VFX (call before discard removes them).</summary>
        public List<CardViewUI> GetCurrentHandViewsSnapshot()
        {
            var list = new List<CardViewUI>(spawnedCards.Count);
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null)
                    list.Add(v);
            }

            return list;
        }

        private CardViewUI CreateCardView(CardInstance card)
        {
            var view = Instantiate(cardViewPrefab, handContainer);

            view.Bind(card);
            view.SetLayoutLerpSpeed(layoutLerpSpeed);
            if (useFixedSizeAdaptiveFan)
                ApplyFixedSizePresentationTuning(view, hoverRaise, hoverScale, 0f);
            SetupCardView(view, card);

            view.OnHoverStarted += HandleCardHoverStarted;
            view.OnHoverEnded += HandleCardHoverEnded;

            return view;
        }

        private void RemoveView(CardViewUI view)
        {
            if (view == null)
                return;

            view.OnHoverStarted -= HandleCardHoverStarted;
            view.OnHoverEnded -= HandleCardHoverEnded;

            if (hoveredCardView == view)
                hoveredCardView = null;

            if (selectedView == view)
            {
                selectedView = null;
                view.Deselect();
            }

            spawnedCards.Remove(view);
            Destroy(view.gameObject);
        }

        private void SetupCardView(CardViewUI view, CardInstance card)
        {
            if (view == null || card?.Data == null)
                return;

            view.SetClickAction(() =>
            {
                if (!view.IsInteractable)
                    return;

                SelectView(view);
                TryPlayCardFromView(card);
            });
        }

        private void HandleCardHoverStarted(CardViewUI view)
        {
            if (view == null || hoveredCardView == view)
                return;

            hoveredCardView = view;
            cardSfx?.PlayHover();
            LayoutCards();
        }

        private void HandleCardHoverEnded(CardViewUI view)
        {
            if (hoveredCardView == view)
                hoveredCardView = null;

            LayoutCards();
        }

        private CardViewUI GetFocusedCardView()
        {
            // Selected > Hovered > Normal
            if (selectedView != null && spawnedCards.Contains(selectedView))
                return selectedView;

            if (hoveredCardView != null &&
                spawnedCards.Contains(hoveredCardView) &&
                hoveredCardView.IsPointerOver)
            {
                return hoveredCardView;
            }

            return null;
        }

        private int GetFocusedCardIndex()
        {
            var focused = GetFocusedCardView();
            return focused != null ? spawnedCards.IndexOf(focused) : -1;
        }

        /// <summary>Places cards in a fan; opens a gap at the focused card (hover takes priority over selection).</summary>
        private void LayoutCards()
        {
            var container = handContainer as RectTransform;
            if (container == null || spawnedCards.Count == 0)
                return;

            if (useFixedSizeAdaptiveFan)
                LayoutCardsFixedSize();
            else if (useAdaptiveFanLayout)
                LayoutCardsAdaptive();
            else
                LayoutCardsLegacy();

            UpdateHandSiblingOrder();
        }

        private void LayoutCardsLegacy()
        {
            int count = spawnedCards.Count;
            float cardScale = GetResponsiveCardScale(count);

            float resolvedSpacing = scaleSpacingWithCard ? spacing * cardScale : spacing;
            float resolvedHoverGap = scaleSpacingWithCard ? hoverGap * cardScale : hoverGap;
            float resolvedCurveHeight = scaleSpacingWithCard ? curveHeight * cardScale : curveHeight;

            float centerIndex = count > 1 ? (count - 1) * 0.5f : 0f;
            int focusedIndex = GetFocusedCardIndex();

            for (int i = 0; i < count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                var rt = view.LayoutRect;
                if (rt != null)
                {
                    rt.sizeDelta = baseCardSize;
                    rt.localScale = Vector3.one * cardScale;
                }

                float relative = i - centerIndex;

                float x = relative * resolvedSpacing;
                float y = -resolvedCurveHeight * relative * relative;

                if (focusedIndex >= 0)
                {
                    if (i < focusedIndex)
                        x -= resolvedHoverGap * 0.5f;
                    else if (i > focusedIndex)
                        x += resolvedHoverGap * 0.5f;
                }

                float rotZ = -relative * rotationStep;
                view.SetLayoutPose(new Vector2(x, y), rotZ);
            }
        }

        private void LayoutCardsFixedSize()
        {
            int count = spawnedCards.Count;
            float cardScale = Mathf.Max(0.01f, fixedCardScale);
            float cardWidth = ResolveFixedCardWidth(cardScale);

            float preferredSpacing = Mathf.Max(0.01f, preferredCenterSpacing);
            float fitSpacing = count > 1
                ? Mathf.Max(0f, (maxHandWidth - cardWidth) / (count - 1))
                : preferredSpacing;

            // Small hands keep preferred spacing and stay grouped (do not stretch to maxHandWidth).
            // Large hands compress only as far as needed to fit.
            float resolvedSpacing = count <= 1
                ? 0f
                : Mathf.Min(preferredSpacing, fitSpacing);

            if (fitSpacing >= minimumCenterSpacing)
                resolvedSpacing = Mathf.Max(minimumCenterSpacing, resolvedSpacing);

            resolvedSpacing = Mathf.Max(0f, resolvedSpacing);

            int maxHandSize = deckController != null ? Mathf.Max(5, deckController.MaxHandSize) : 10;
            float largeHandT = Mathf.InverseLerp(largeHandHoverStartCount, maxHandSize, count);
            float resolvedHoverGap = Mathf.Lerp(hoverGap, largeHandHoverGap, largeHandT);
            float resolvedHoverRaise = Mathf.Lerp(hoverRaise, largeHandHoverRaise, largeHandT);
            float resolvedHoverScale = Mathf.Lerp(hoverScale, largeHandHoverScale, largeHandT);

            float centerIndex = count > 1 ? (count - 1) * 0.5f : 0f;
            int focusedIndex = GetFocusedCardIndex();

            float fanStrength = GetFanStrength(count);
            float resolvedEdgeDrop = maxEdgeDrop * fanStrength;
            float resolvedEdgeRotation = maxEdgeRotation * fanStrength;

            float totalSpan = count > 1 ? resolvedSpacing * (count - 1) : 0f;
            float totalWidth = cardWidth + totalSpan;

            lastLoggedCardScale = cardScale;
            lastLoggedCardWidth = cardWidth;
            lastLoggedPreferredSpacing = preferredSpacing;
            lastLoggedFitSpacing = fitSpacing;
            lastLoggedSpacing = resolvedSpacing;
            lastLoggedTotalSpan = totalSpan;
            lastLoggedTotalWidth = totalWidth;
            lastLoggedEdgeDrop = resolvedEdgeDrop;
            lastLoggedHandRaise = 0f;
            lastLoggedEdgeRotation = resolvedEdgeRotation;
            lastLoggedFanStrength = fanStrength;
            lastLoggedHoverGap = resolvedHoverGap;
            lastLoggedHoverRaise = resolvedHoverRaise;
            lastLoggedHoverScale = resolvedHoverScale;
            lastLoggedHoveredIndex = focusedIndex;

            for (int i = 0; i < count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                float edgeHoverOffsetX = 0f;
                if (focusedIndex >= 0 &&
                    i == focusedIndex &&
                    count > 1 &&
                    edgeHoverInwardOffset > 0f)
                {
                    if (i == 0)
                        edgeHoverOffsetX = edgeHoverInwardOffset;
                    else if (i == count - 1)
                        edgeHoverOffsetX = -edgeHoverInwardOffset;
                }

                // Edge inward correction moves VisualRoot only — root hit slot stays fixed.
                ApplyFixedSizePresentationTuning(
                    view,
                    resolvedHoverRaise,
                    resolvedHoverScale,
                    edgeHoverOffsetX);

                var rt = view.LayoutRect;
                if (rt != null)
                {
                    rt.sizeDelta = baseCardSize;
                    rt.localScale = Vector3.one * cardScale;
                }

                float relative = i - centerIndex;
                float normalized = centerIndex > 0f ? relative / centerIndex : 0f;
                float edgeFactor = normalized * normalized;

                float x = relative * resolvedSpacing;
                float y = -edgeFactor * resolvedEdgeDrop;

                if (focusedIndex >= 0 && i != focusedIndex)
                {
                    int distance = Mathf.Abs(i - focusedIndex);
                    float distanceMultiplier = distance <= 1
                        ? 1f
                        : Mathf.Pow(neighborPushFalloff, distance - 1);
                    float push = resolvedHoverGap * 0.5f * distanceMultiplier;

                    if (i < focusedIndex)
                        x -= push;
                    else
                        x += push;
                }

                float rotZ = -normalized * resolvedEdgeRotation;
                view.SetLayoutPose(new Vector2(x, y), rotZ);
            }

            if (verboseLogs && count != lastLoggedLayoutCount)
                LogFixedFanLayout(force: false);

            lastLoggedLayoutCount = count;
        }

        private float GetFanStrength(int cardCount)
        {
            if (!useCustomFanStrengthByCount)
                return 1f;

            if (cardCount <= 0)
                return 0f;

            if (fanStrengthByCardCount == null || fanStrengthByCardCount.Length == 0)
                return 1f;

            int index = Mathf.Clamp(cardCount, 0, fanStrengthByCardCount.Length - 1);
            return Mathf.Max(0f, fanStrengthByCardCount[index]);
        }

        private float ResolveFixedCardWidth(float cardScale)
        {
            float width = baseCardSize.x;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                var rt = view != null ? view.LayoutRect : null;
                if (rt == null)
                    continue;

                float rectWidth = rt.rect.width;
                if (rectWidth > 1f)
                {
                    width = rectWidth;
                    break;
                }

                if (rt.sizeDelta.x > 1f)
                {
                    width = rt.sizeDelta.x;
                    break;
                }
            }

            if (width <= 1f && cardViewPrefab != null)
            {
                var prefabRt = cardViewPrefab.LayoutRect != null
                    ? cardViewPrefab.LayoutRect
                    : cardViewPrefab.GetComponent<RectTransform>();
                if (prefabRt != null && prefabRt.sizeDelta.x > 1f)
                    width = prefabRt.sizeDelta.x;
            }

            if (width <= 1f)
                width = 200f;

            return width * Mathf.Max(0.01f, cardScale);
        }

        private void ApplyFixedSizePresentationTuning(
            CardViewUI view,
            float resolvedHoverRaise,
            float resolvedHoverScale,
            float edgeHoverOffsetX)
        {
            if (view == null)
                return;

            float selectedRaise = Mathf.Max(resolvedHoverRaise, resolvedHoverRaise + 20f);
            float selectedScaleMul = Mathf.Max(resolvedHoverScale, resolvedHoverScale + 0.04f);
            // Same inward X for hover and selected — ApplyStateVisuals uses one state at a time.
            view.ApplyHandPresentationTuning(
                resolvedHoverRaise,
                resolvedHoverScale,
                selectedRaise,
                selectedScaleMul,
                edgeHoverOffsetX,
                edgeHoverOffsetX);
        }

        private void UpdateHandSiblingOrder()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view != null)
                    view.transform.SetSiblingIndex(i);
            }

            if (!keepHoveredCardOnTop)
                return;

            // Selected > Hovered > Normal fan order for draw order only (not DeckController.Hand).
            var focused = GetFocusedCardView();
            if (focused != null)
                focused.transform.SetAsLastSibling();
        }

        private void LayoutCardsAdaptive()
        {
            int count = spawnedCards.Count;
            float cardScale = GetResponsiveCardScale(count);

            float preferredSpacing = scaleSpacingWithCard ? spacing * cardScale : spacing;
            preferredSpacing = Mathf.Clamp(preferredSpacing, minimumSpacing, maximumSpacing);

            float widthLimitedSpacing = count > 1
                ? maxHandWidth / (count - 1)
                : preferredSpacing;

            // Prefer fitting within maxHandWidth; never allow zero/negative spacing.
            float resolvedSpacing = Mathf.Max(0.01f, Mathf.Min(preferredSpacing, widthLimitedSpacing));

            float preferredHoverGap = scaleSpacingWithCard ? hoverGap * cardScale : hoverGap;
            float safeHoverGap = preferredHoverGap;

            int maxHandSize = deckController != null ? Mathf.Max(largeHandStartCount, deckController.MaxHandSize) : 10;
            if (count >= largeHandStartCount)
            {
                float compression = Mathf.InverseLerp(largeHandStartCount, maxHandSize, count);
                safeHoverGap = Mathf.Lerp(preferredHoverGap, preferredHoverGap * 0.65f, compression);
            }

            float centerIndex = count > 1 ? (count - 1) * 0.5f : 0f;
            int focusedIndex = GetFocusedCardIndex();

            float preferredEdgeDrop = curveHeight * centerIndex * centerIndex;
            if (scaleSpacingWithCard)
                preferredEdgeDrop *= cardScale;
            float resolvedEdgeDrop = Mathf.Min(maxEdgeDrop, preferredEdgeDrop);

            float preferredEdgeRotation = rotationStep * centerIndex;
            float resolvedEdgeRotation = Mathf.Min(maxEdgeRotation, preferredEdgeRotation);

            float largeHandT = Mathf.InverseLerp(largeHandStartCount, maxHandSize, count);
            float handRaise = Mathf.Lerp(0f, largeHandRaise, largeHandT);

            float totalSpan = count > 1 ? resolvedSpacing * (count - 1) : 0f;

            lastLoggedCardScale = cardScale;
            lastLoggedSpacing = resolvedSpacing;
            lastLoggedTotalSpan = totalSpan;
            lastLoggedEdgeDrop = resolvedEdgeDrop;
            lastLoggedHandRaise = handRaise;
            lastLoggedEdgeRotation = resolvedEdgeRotation;

            for (int i = 0; i < count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                var rt = view.LayoutRect;
                if (rt != null)
                {
                    rt.sizeDelta = baseCardSize;
                    rt.localScale = Vector3.one * cardScale;
                }

                float relative = i - centerIndex;
                float normalized = centerIndex > 0f ? relative / centerIndex : 0f;
                float edgeFactor = normalized * normalized;

                float x = relative * resolvedSpacing;
                float y = -edgeFactor * resolvedEdgeDrop + handRaise;

                if (focusedIndex >= 0)
                {
                    if (i < focusedIndex)
                        x -= safeHoverGap * 0.5f;
                    else if (i > focusedIndex)
                        x += safeHoverGap * 0.5f;
                }

                float rotZ = -normalized * resolvedEdgeRotation;
                view.SetLayoutPose(new Vector2(x, y), rotZ);
            }

            if (verboseLogs && count != lastLoggedLayoutCount)
                LogAdaptiveHandLayout(force: false);

            lastLoggedLayoutCount = count;
        }

        private void LogAdaptiveHandLayout(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[AdaptiveHandLayout]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"CardScale={lastLoggedCardScale:0.##}\n" +
                $"Spacing={lastLoggedSpacing:0.##}\n" +
                $"TotalSpan={lastLoggedTotalSpan:0.##}\n" +
                $"EdgeDrop={lastLoggedEdgeDrop:0.##}\n" +
                $"HandRaise={lastLoggedHandRaise:0.##}\n" +
                $"EdgeRotation={lastLoggedEdgeRotation:0.##}");
        }

        private void LogFixedFanLayout(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[FixedFanLayout]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"CardWidth={lastLoggedCardWidth:0.##}\n" +
                $"Scale={lastLoggedCardScale:0.##}\n" +
                $"PreferredSpacing={lastLoggedPreferredSpacing:0.##}\n" +
                $"FitSpacing={lastLoggedFitSpacing:0.##}\n" +
                $"ResolvedSpacing={lastLoggedSpacing:0.##}\n" +
                $"TotalWidth={lastLoggedTotalWidth:0.##}\n" +
                $"FanStrength={lastLoggedFanStrength:0.##}\n" +
                $"EdgeDrop={lastLoggedEdgeDrop:0.##}\n" +
                $"EdgeRotation={lastLoggedEdgeRotation:0.##}");
        }

        private void LogFanStrength(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[FanStrength]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"Strength={lastLoggedFanStrength:0.##}\n" +
                $"ResolvedEdgeRotation={lastLoggedEdgeRotation:0.##}\n" +
                $"ResolvedEdgeDrop={lastLoggedEdgeDrop:0.##}");
        }

        private void LogHandPresentation(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[HandPresentation]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"FanStrength={lastLoggedFanStrength:0.##}\n" +
                $"ResolvedEdgeDrop={lastLoggedEdgeDrop:0.##}\n" +
                $"ResolvedEdgeRotation={lastLoggedEdgeRotation:0.##}\n" +
                $"HoverGap={lastLoggedHoverGap:0.##}\n" +
                $"HoverRaise={lastLoggedHoverRaise:0.##}\n" +
                $"HoverScale={lastLoggedHoverScale:0.##}\n" +
                $"HoveredIndex={lastLoggedHoveredIndex}");
        }

        private void SelectView(CardViewUI view)
        {
            if (view == null)
                return;

            if (selectedView != null && selectedView != view)
                selectedView.Deselect();

            selectedView = view;
            selectedView.Select();
            LayoutCards();
        }

        public void DeselectCurrentCard()
        {
            if (selectedView != null)
            {
                var previouslySelected = selectedView;
                previouslySelected.Deselect();
                selectedView = null;

                if (hoveredCardView == previouslySelected && !previouslySelected.IsPointerOver)
                    hoveredCardView = null;
            }

            LayoutCards();
        }

        /// <summary>Whether this card needs the single-enemy target selection UI before play.</summary>
        private bool RequiresManualEnemyTarget(CardData data)
        {
            if (data == null)
                return false;

            if (data.HasEffects)
                return data.TargetMode == CardTargetMode.SingleEnemy;

            return data.CardType == CardType.Attack;
        }

        /// <summary>Primary target for immediate <see cref="BattleActionRunner.TryPlayCard"/> when not entering target selection.</summary>
        private EnemyBattleUnit ResolveImmediateDefaultTarget(CardData data)
        {
            if (data == null)
                return null;

            if (data.HasEffects)
                return null;

            if (data.CardType == CardType.Attack)
                return GetDefaultAliveEnemy();

            return null;
        }

        private void TryPlayCardFromView(CardInstance card)
        {
            if (card?.Data == null || battleActionRunner == null)
                return;

            if (RequiresManualEnemyTarget(card.Data))
            {
                if (targetSelectionSystem != null)
                {
                    targetSelectionSystem.BeginTargetSelection(card);

                    if (verboseLogs)
                        Debug.Log($"[HandUI] Waiting for target selection: {card.Data.DisplayName} | TargetMode: {card.Data.TargetMode}");

                    // สำคัญ: อย่า RefreshHandUI ตรงนี้
                    // เพื่อให้ selected state ค้างอยู่
                    return;
                }
            }

            EnemyBattleUnit target = ResolveImmediateDefaultTarget(card.Data);
            battleActionRunner.TryPlayCard(card, target);

            if (verboseLogs)
            {
                string targetName = target != null ? target.name : "None";
                string modeNote = card.Data.HasEffects ? $"TargetMode: {card.Data.TargetMode}" : $"CardType: {card.Data.CardType}";
                Debug.Log($"[HandUI] Clicked {card.Data.DisplayName} | Immediate resolve | {modeNote} | Target: {targetName}");
            }
        }

        private void HandlePlayerApChanged(int currentAp, int maxAp)
        {
            RefreshCardInteractivity();
        }

        private void HandlePlayerTurnStateChanged(bool canAct)
        {
            RefreshCardInteractivity();
        }

        private void HandleBusyStateChanged(bool isBusy)
        {
            RefreshCardInteractivity();
        }

        private void RefreshCardInteractivity()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view == null || view.BoundCard?.Data == null)
                    continue;

                var card = view.BoundCard;

                bool canPlay = player != null &&
                               player.CanAct &&
                               player.CanSpendAp(card.Data.ApCost) &&
                               deckController != null &&
                               deckController.IsInHand(card);

                if (battleActionRunner != null)
                    canPlay = canPlay && battleActionRunner.CanAcceptInput;

                if (disableUnplayableCards)
                    view.SetInteractable(canPlay);
                else
                    view.SetInteractable(true);
            }
        }

        public void RefreshInteractivityExternal()
        {
            RefreshCardInteractivity();
        }

        /// <summary>Clears hand UI selection and spawned views before deck rebuild for a new battle.</summary>
        public void ResetHandRuntimeStateForNewBattle()
        {
            if (dealRoutine != null)
                StopDealRoutineAndReleaseLocks();

            DeselectCurrentCard();
            hoveredCardView = null;
            ClearSpawnedCards();
        }

        private EnemyBattleUnit GetDefaultAliveEnemy()
        {
            if (enemyActionSystem == null)
                return null;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return enemies[i];
            }

            return null;
        }

        private void ClearSpawnedCards()
        {
            hoveredCardView = null;

            if (dealRoutine != null)
                StopDealRoutineAndReleaseLocks();

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null)
                {
                    v.OnHoverStarted -= HandleCardHoverStarted;
                    v.OnHoverEnded -= HandleCardHoverEnded;
                    Destroy(v.gameObject);
                }
            }

            spawnedCards.Clear();
        }

        private IEnumerator CoDealInNewCards(List<CardViewUI> newViews)
        {
            if (newViews == null || newViews.Count == 0)
            {
                dealRoutine = null;
                yield break;
            }

            newViews.Sort((a, b) =>
            {
                int ia = spawnedCards.IndexOf(a);
                int ib = spawnedCards.IndexOf(b);
                return ia.CompareTo(ib);
            });

            if (!dealLeftToRight)
                newViews.Reverse();

            float stagger = Mathf.Max(0f, dealStagger);

            for (int i = 0; i < newViews.Count; i++)
            {
                var view = newViews[i];
                if (view != null)
                {
                    view.SetLayoutMovementBlocked(false);
                    cardSfx?.PlayDraw();
                }

                if (stagger > 0f && i < newViews.Count - 1)
                    yield return new WaitForSeconds(stagger);
            }

            dealRoutine = null;
            RefreshCardInteractivity();
        }

        private void PrepareNewCardsForDeal(List<CardViewUI> newViews)
        {
            Vector2 spawnPos = GetDealSpawnAnchoredPosition();
            for (int i = 0; i < newViews.Count; i++)
            {
                var view = newViews[i];
                if (view == null)
                    continue;

                view.SetLayoutMovementBlocked(true);
                view.PrepareForDealIn(spawnPos, newCardSpawnRotationZ, newCardSpawnScale);
            }
        }

        private void StopDealRoutineAndReleaseLocks()
        {
            if (dealRoutine != null)
            {
                StopCoroutine(dealRoutine);
                dealRoutine = null;
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view != null)
                {
                    if (view.IsDealPresentationPending)
                        view.ForceCompleteDealPresentation();
                    else
                        view.SetLayoutMovementBlocked(false);
                }
            }
        }

        private Vector2 GetDealSpawnAnchoredPosition()
        {
            var containerRect = handContainer as RectTransform;
            if (containerRect == null)
                return Vector2.zero;

            if (drawSpawnAnchor == null)
                return containerRect.rect.center;

            Canvas canvas = containerRect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, drawSpawnAnchor.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, screenPoint, cam, out var local))
                return local;

            return containerRect.rect.center;
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (deckController == null)
            {
                Debug.LogError("HandUIController: DeckController reference is missing.");
                valid = false;
            }

            if (player == null)
            {
                Debug.LogError("HandUIController: PlayerBattleUnit reference is missing.");
                valid = false;
            }

            if (enemyActionSystem == null)
            {
                Debug.LogError("HandUIController: EnemyActionSystem reference is missing.");
                valid = false;
            }

            if (handContainer == null)
            {
                Debug.LogError("HandUIController: Hand container reference is missing.");
                valid = false;
            }

            if (cardViewPrefab == null)
            {
                Debug.LogError("HandUIController: CardView prefab reference is missing.");
                valid = false;
            }

            return valid;
        }
    }
}
```