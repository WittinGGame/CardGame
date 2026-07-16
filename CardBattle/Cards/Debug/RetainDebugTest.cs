using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Play Mode harness for Retain keyword and end-turn hand routing validation.
    /// </summary>
    public class RetainDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[RetainDebug]";

        [Header("References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private PileCounterUI pileCounterUI;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private CardData retainTestCard;
        [SerializeField] private CardData normalTestCard;

        private int? conservationBaseline;

        [ContextMenu("Debug/Print Retain State")]
        public void DebugPrintRetainState()
        {
            if (!ValidateDeck())
                return;

            var sb = new StringBuilder();
            sb.AppendLine($"{LogPrefix} Pile state:");
            sb.AppendLine($"Deck={deckController.Deck.Count}");
            sb.AppendLine($"Hand={deckController.Hand.Count}/{deckController.MaxHandSize}");
            sb.AppendLine($"Graveyard={deckController.Graveyard.Count}");
            sb.AppendLine($"Exhaust={deckController.GetExhaustCount()}");
            sb.AppendLine($"PendingOverflow={deckController.PendingOverflowCount}");
            sb.AppendLine($"Total={GetConservedTotal()}");
            sb.AppendLine(
                $"RunnerBusy={(battleActionRunner != null && battleActionRunner.IsBusy)} " +
                $"CanAct={(player != null && player.CanAct)}");

            var hand = deckController.Hand;
            sb.AppendLine($"Hand cards ({hand.Count}):");
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                {
                    sb.AppendLine($"  [{i}] <null>");
                    continue;
                }

                sb.AppendLine(
                    $"  [{i}] {card.Data.DisplayName} | id={card.InstanceId} | " +
                    $"Retain={DeckController.ResolveRetainAtEndTurn(card)} | " +
                    $"ExhaustAfterPlay={card.Data.ExhaustAfterPlay}");
            }

            Debug.Log(sb.ToString());
        }

        [ContextMenu("Debug/Validate No Duplicate InstanceIds")]
        public void DebugValidateNoDuplicateInstanceIds()
        {
            if (!ValidateDeck())
                return;

            if (!HasDuplicateInstanceIdsAcrossPiles())
                Debug.Log($"{LogPrefix} PASS — No duplicate InstanceIds across Deck/Hand/Graveyard/Exhaust");
            else
                Debug.LogError($"{LogPrefix} FAIL — Duplicate InstanceIds detected across piles");
        }

        [ContextMenu("Debug/Capture Conservation Baseline")]
        public void DebugCaptureConservationBaseline()
        {
            if (!ValidateDeck())
                return;

            conservationBaseline = GetConservedTotal();
            Debug.Log($"{LogPrefix} Baseline captured: total={conservationBaseline.Value}");
        }

        [ContextMenu("Debug/Reset Conservation Baseline")]
        public void DebugResetConservationBaseline()
        {
            conservationBaseline = null;
            Debug.Log($"{LogPrefix} Baseline reset.");
        }

        [ContextMenu("Debug/Validate Card Conservation")]
        public void DebugValidateCardConservation()
        {
            if (!ValidateDeck())
                return;

            int total = GetConservedTotal();
            bool noDup = !HasDuplicateInstanceIdsAcrossPiles();
            bool baselineOk = !conservationBaseline.HasValue || total == conservationBaseline.Value;

            if (noDup && baselineOk)
            {
                string baselineNote = conservationBaseline.HasValue
                    ? $"matches baseline={conservationBaseline.Value}"
                    : "no baseline captured (capture one for strict comparison)";
                Debug.Log($"{LogPrefix} PASS — Conservation total={total}, {baselineNote}");
            }
            else
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL — total={total}, noDup={noDup}, " +
                    $"baseline={(conservationBaseline.HasValue ? conservationBaseline.Value.ToString() : "none")}, " +
                    $"baselineOk={baselineOk}");
            }
        }

        [ContextMenu("Debug/Print Retain End Turn Checklist")]
        public void DebugPrintRetainEndTurnChecklist()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{LogPrefix} Retain end-turn checklist (manual Play Mode via BattleActionRunner):");
            sb.AppendLine("1. End turn with a Retain card still in Hand.");
            sb.AppendLine("2. Retain card stays in Hand; non-Retain cards fly to Graveyard VFX.");
            sb.AppendLine("3. Graveyard count increases only by discarded (non-Retain) cards.");
            sb.AppendLine("4. Exhaust count unchanged by end-turn discard.");
            sb.AppendLine("5. Next player round draws the full Draw Per Round amount (not reduced by retained cards).");
            sb.AppendLine("6. Hand = retained + newly drawn (up to MaxHandSize); overflow follows existing rules.");
            sb.AppendLine("7. Retained CardView keeps the same CardInstance binding (no duplicate views).");
            sb.AppendLine("8. Retained card is playable next round.");
            sb.AppendLine("9. Playing a Retain card routes via ExhaustAfterPlay (Graveyard or Exhaust).");
            sb.AppendLine("10. Manual discard can discard a Retain card to Graveyard.");
            sb.AppendLine("11. Retain + Exhaust: unplayed → retained; played → Exhaust pile.");
            sb.AppendLine("12. Capture baseline, end turn, draw — total conservation unchanged.");

            if (retainTestCard != null)
            {
                sb.AppendLine($"Retain test card: {retainTestCard.DisplayName} (id={retainTestCard.CardId})");
                sb.AppendLine($"Retain={retainTestCard.Retain} ExhaustAfterPlay={retainTestCard.ExhaustAfterPlay}");
                sb.AppendLine($"Description:\n{CardDescriptionBuilder.Build(retainTestCard)}");
            }
            else
            {
                sb.AppendLine("Assign retainTestCard (e.g. Card_HoldTheLine) for asset details.");
            }

            if (normalTestCard != null)
                sb.AppendLine($"Normal test card: {normalTestCard.DisplayName} (id={normalTestCard.CardId})");

            Debug.Log(sb.ToString());
            DebugPrintRetainState();
        }

        [ContextMenu("Debug/Resolve End Turn Hand Directly")]
        public void DebugResolveEndTurnHandDirectly()
        {
            if (!ValidateDeck())
                return;

            var result = deckController.ResolveEndTurnHand();
            Debug.Log(
                $"{LogPrefix} ResolveEndTurnHand | Discarded={result.DiscardedCount} " +
                $"Retained={result.RetainedCount} | Hand={deckController.Hand.Count} " +
                $"Graveyard={deckController.Graveyard.Count}");

            if (handUIController != null)
                handUIController.SyncHandViewsExternal();

            if (pileCounterUI != null)
                pileCounterUI.ForceSyncDisplayedToReal();

            DebugPrintRetainState();
        }

        private int GetConservedTotal()
        {
            if (deckController == null)
                return 0;

            return deckController.Deck.Count
                   + deckController.Hand.Count
                   + deckController.Graveyard.Count
                   + deckController.GetExhaustCount()
                   + deckController.PendingOverflowCount;
        }

        private bool HasDuplicateInstanceIdsAcrossPiles()
        {
            var seen = new HashSet<System.Guid>();
            return HasDupInPile(deckController.Deck, seen) ||
                   HasDupInPile(deckController.Hand, seen) ||
                   HasDupInPile(deckController.Graveyard, seen) ||
                   HasDupInPile(deckController.ExhaustPile, seen);
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

        private bool ValidateDeck()
        {
            if (deckController != null)
                return true;

            Debug.LogError($"{LogPrefix} DeckController reference is missing.");
            return false;
        }
    }
}
