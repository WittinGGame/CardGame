using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Play Mode harness for manual discard selection and Cycle-style sequences.
    /// </summary>
    public class ManualDiscardEffectDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[ManualDiscardDebug]";

        [Header("References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandCardSelectionController handCardSelectionController;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private CardData cycleTestCard;

        [Header("Options")]
        [SerializeField] private bool verboseLogs = true;

        [ContextMenu("Debug/Print Selection State")]
        public void DebugPrintSelectionState()
        {
            if (handCardSelectionController == null || deckController == null)
            {
                Debug.LogError($"{LogPrefix} Missing selection or deck reference.");
                return;
            }

            Debug.Log(
                $"{LogPrefix}\n" +
                $"IsSelecting={handCardSelectionController.IsSelecting}\n" +
                $"Requested={handCardSelectionController.RequestedCount}\n" +
                $"Required={handCardSelectionController.RequiredCount}\n" +
                $"Selected={handCardSelectionController.SelectedCount}\n" +
                $"Hand={deckController.Hand.Count}\n" +
                $"Graveyard={deckController.Graveyard.Count}\n" +
                $"PendingOverflow={deckController.PendingOverflowCount}\n" +
                $"RunnerBusy={(battleActionRunner != null && battleActionRunner.IsBusy)}");
        }

        [ContextMenu("Debug/Begin Discard 1")]
        public void DebugBeginDiscard1()
        {
            BeginDiscardFromCurrentHand(1);
        }

        [ContextMenu("Debug/Begin Discard 2 With One Card")]
        public void DebugBeginDiscard2WithOneCard()
        {
            if (!ValidateSelectionRefs())
                return;

            int handCount = CountValidHandCards();
            handCardSelectionController.BeginSelection(
                HandCardSelectionPurpose.ManualDiscard,
                BuildHandCandidates(),
                2,
                "Select cards to discard");

            int required = handCardSelectionController.RequiredCount;
            bool pass = required == Mathf.Min(2, handCount);

            if (pass)
                Debug.Log($"{LogPrefix} PASS — Requested=2 Hand={handCount} Required={required}");
            else
                Debug.LogError($"{LogPrefix} FAIL — Requested=2 Hand={handCount} Required={required}");

            DebugPrintSelectionState();
        }

        [ContextMenu("Debug/Begin Discard With Empty Hand")]
        public void DebugBeginDiscardWithEmptyHand()
        {
            if (!ValidateSelectionRefs())
                return;

            // Intentionally request discard against current hand (may not be empty — prints result).
            int beforeBusy = battleActionRunner != null && battleActionRunner.IsBusy ? 1 : 0;

            handCardSelectionController.BeginSelection(
                HandCardSelectionPurpose.ManualDiscard,
                BuildHandCandidates(),
                2,
                "Select cards to discard");

            bool isSelecting = handCardSelectionController.IsSelecting;
            int required = handCardSelectionController.RequiredCount;

            if (CountValidHandCards() == 0)
            {
                bool pass = !isSelecting && required <= 0 && beforeBusy == 0;
                if (pass)
                    Debug.Log($"{LogPrefix} PASS — Empty hand skips selection, overlay stays hidden, runner not locked by debug begin.");
                else
                    Debug.LogError($"{LogPrefix} FAIL — Empty hand selection IsSelecting={isSelecting} Required={required}");
            }
            else
            {
                Debug.LogWarning(
                    $"{LogPrefix} Hand is not empty (count={CountValidHandCards()}). " +
                    "Empty-hand pass requires 0 valid cards in Hand.");
            }

            DebugPrintSelectionState();
        }

        [ContextMenu("Debug/Validate Discard Move")]
        public void DebugValidateDiscardMove()
        {
            if (!ValidateSelectionRefs() || deckController == null)
                return;

            var hand = deckController.Hand;
            CardInstance pick = null;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i]?.Data != null)
                {
                    pick = hand[i];
                    break;
                }
            }

            if (pick == null)
            {
                Debug.LogError($"{LogPrefix} No hand card available to discard.");
                return;
            }

            var id = pick.InstanceId;
            int totalBefore = deckController.Deck.Count + deckController.Hand.Count +
                              deckController.Graveyard.Count + deckController.PendingOverflowCount;

            bool moved = deckController.DiscardCardFromHand(pick);
            handUIController?.SyncHandViewsExternal();

            bool stillInHand = deckController.IsInHand(pick);
            int gyCount = 0;
            for (int i = 0; i < deckController.Graveyard.Count; i++)
            {
                if (deckController.Graveyard[i] != null &&
                    deckController.Graveyard[i].InstanceId == id)
                {
                    gyCount++;
                }
            }

            int totalAfter = deckController.Deck.Count + deckController.Hand.Count +
                             deckController.Graveyard.Count + deckController.PendingOverflowCount;

            bool noDupAcrossPiles = !HasDuplicateInstanceIdsAcrossPiles();

            bool pass = moved && !stillInHand && gyCount == 1 &&
                        totalBefore == totalAfter && noDupAcrossPiles;

            if (pass)
            {
                Debug.Log(
                    $"{LogPrefix} PASS — Discard move conserved. InstanceId={id} Total={totalAfter}");
            }
            else
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL — Discard move\n" +
                    $"Moved={moved} StillInHand={stillInHand} GyMatches={gyCount}\n" +
                    $"TotalBefore={totalBefore} TotalAfter={totalAfter} NoDup={noDupAcrossPiles}");
            }
        }

        [ContextMenu("Debug/Validate Cycle Sequence Checklist")]
        public void DebugValidateCycleSequenceChecklist()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{LogPrefix} Cycle sequence checklist (manual Play Mode validation):");
            sb.AppendLine("1. Play Cycle (cost 1) — AP decreases by exactly 1.");
            sb.AppendLine("2. Cycle moves to Graveyard once.");
            sb.AppendLine("3. Draw 2 presentations finish before discard overlay appears.");
            sb.AppendLine("4. Newly drawn cards are selectable in discard selection.");
            sb.AppendLine("5. Confirm discard moves selected card to Graveyard (no AP, no countdown).");
            sb.AppendLine("6. Enemy successful-card-play / countdown occurs exactly once after discard.");
            sb.AppendLine("7. PendingOverflowCount == 0 after sequence.");
            sb.AppendLine("8. BattleActionRunner unlocks after enemy response.");

            if (cycleTestCard != null)
            {
                sb.AppendLine($"Cycle asset: {cycleTestCard.DisplayName} (id={cycleTestCard.CardId})");
                sb.AppendLine($"Description:\n{CardDescriptionBuilder.Build(cycleTestCard)}");
            }
            else
            {
                sb.AppendLine("Assign cycleTestCard to print generated description.");
            }

            Debug.Log(sb.ToString());
            DebugPrintSelectionState();
        }

        private void BeginDiscardFromCurrentHand(int requested)
        {
            if (!ValidateSelectionRefs())
                return;

            handCardSelectionController.BeginSelection(
                HandCardSelectionPurpose.ManualDiscard,
                BuildHandCandidates(),
                requested,
                "Select cards to discard");

            if (verboseLogs)
                DebugPrintSelectionState();
        }

        private List<CardInstance> BuildHandCandidates()
        {
            var list = new List<CardInstance>();
            if (deckController == null)
                return list;

            var hand = deckController.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i]?.Data != null)
                    list.Add(hand[i]);
            }

            return list;
        }

        private int CountValidHandCards()
        {
            if (deckController == null)
                return 0;

            int count = 0;
            var hand = deckController.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i]?.Data != null)
                    count++;
            }

            return count;
        }

        private bool HasDuplicateInstanceIdsAcrossPiles()
        {
            var seen = new HashSet<System.Guid>();
            return HasDupInPile(deckController.Deck, seen) ||
                   HasDupInPile(deckController.Hand, seen) ||
                   HasDupInPile(deckController.Graveyard, seen);
        }

        private static bool HasDupInPile(IReadOnlyList<CardInstance> pile, HashSet<System.Guid> seen)
        {
            if (pile == null)
                return false;

            for (int i = 0; i < pile.Count; i++)
            {
                var card = pile[i];
                if (card == null)
                    continue;
                if (!seen.Add(card.InstanceId))
                    return true;
            }

            return false;
        }

        private bool ValidateSelectionRefs()
        {
            if (handCardSelectionController != null && deckController != null)
                return true;

            Debug.LogError($"{LogPrefix} HandCardSelectionController or DeckController missing.");
            return false;
        }
    }
}
