using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Play Mode harness for Self Exhaust / Exhaust pile validation.
    /// </summary>
    public class SelfExhaustDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[SelfExhaustDebug]";

        [Header("References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private PileCounterUI pileCounterUI;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private CardData exhaustTestCard;

        [ContextMenu("Debug/Print Pile State")]
        public void DebugPrintPileState()
        {
            if (!ValidateDeck())
                return;

            Debug.Log(
                $"{LogPrefix}\n" +
                $"Deck={deckController.Deck.Count}\n" +
                $"Hand={deckController.Hand.Count}/{deckController.MaxHandSize}\n" +
                $"Graveyard={deckController.Graveyard.Count}\n" +
                $"Exhaust={deckController.GetExhaustCount()}\n" +
                $"PendingOverflow={deckController.PendingOverflowCount}\n" +
                $"Total={GetConservedTotal()}\n" +
                $"RunnerBusy={(battleActionRunner != null && battleActionRunner.IsBusy)}");
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

        [ContextMenu("Debug/Validate Total Card Conservation")]
        public void DebugValidateTotalCardConservation()
        {
            if (!ValidateDeck())
                return;

            int total = GetConservedTotal();
            bool noDup = !HasDuplicateInstanceIdsAcrossPiles();

            if (noDup && total > 0)
                Debug.Log($"{LogPrefix} PASS — Conservation total={total}, no duplicates");
            else
                Debug.LogError($"{LogPrefix} FAIL — Conservation total={total}, noDup={noDup}");
        }

        [ContextMenu("Debug/Validate Play Exhaust Card Checklist")]
        public void DebugValidatePlayExhaustCardChecklist()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{LogPrefix} Play Exhaust card checklist (manual Play Mode via BattleActionRunner):");
            sb.AppendLine("1. Play Overcharge (or assigned exhaustTestCard) from Hand.");
            sb.AppendLine("2. AP decreases by card cost; effects resolve normally.");
            sb.AppendLine("3. Played card leaves Hand and appears in Exhaust pile exactly once.");
            sb.AppendLine("4. Played card does NOT appear in Graveyard.");
            sb.AppendLine("5. No Graveyard fly VFX for the exhausted card.");
            sb.AppendLine("6. PileCounter Exhaust count increases immediately.");
            sb.AppendLine("7. Enemy countdown decreases once after full sequence.");
            sb.AppendLine("8. Reshuffle Graveyard → Deck never includes Exhaust pile cards.");
            sb.AppendLine("9. Start new battle — Exhaust pile clears; card returns via RunState deck rebuild.");
            sb.AppendLine("10. Total card conservation remains valid (Deck+Hand+GY+Exhaust+Pending).");

            if (exhaustTestCard != null)
            {
                sb.AppendLine($"Test card: {exhaustTestCard.DisplayName} (id={exhaustTestCard.CardId})");
                sb.AppendLine($"ExhaustAfterPlay={exhaustTestCard.ExhaustAfterPlay}");
                sb.AppendLine($"Description:\n{CardDescriptionBuilder.Build(exhaustTestCard)}");
            }
            else
            {
                sb.AppendLine("Assign exhaustTestCard (e.g. Card_Overcharge) for asset details.");
            }

            Debug.Log(sb.ToString());
            DebugPrintPileState();
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
