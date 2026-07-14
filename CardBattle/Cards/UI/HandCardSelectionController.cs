using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Reusable hand-card selection session owner (manual discard first use-case).
    /// </summary>
    public class HandCardSelectionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private HandCardSelectionUI selectionUI;
        [SerializeField] private CardToGraveyardVFXController graveyardVfx;
        [SerializeField] private PileCounterUI pileCounterUI;
        [SerializeField] private BattleOutcomeController battleOutcomeController;

        [Header("Options")]
        [SerializeField] private string discardInstruction = "Select cards to discard";
        [SerializeField] private bool verboseLogs;

        private readonly List<CardInstance> selectedCards = new List<CardInstance>();
        private readonly List<CardInstance> candidateCards = new List<CardInstance>();
        private readonly List<CardInstance> confirmedSnapshot = new List<CardInstance>();

        private bool isSelecting;
        private bool confirmRequested;
        private bool forceCancelled;
        private int requestedCount;
        private int requiredCount;
        private HandCardSelectionPurpose purpose;

        public bool IsSelecting => isSelecting;
        public int RequestedCount => requestedCount;
        public int RequiredCount => requiredCount;
        public int SelectedCount => selectedCards.Count;
        public IReadOnlyList<CardInstance> SelectedCards => selectedCards;
        public HandCardSelectionPurpose Purpose => purpose;

        public event System.Action OnSelectionStarted;
        public event System.Action OnSelectionChanged;
        public event System.Action OnSelectionConfirmed;
        public event System.Action OnSelectionEnded;

        private void OnEnable()
        {
            if (selectionUI != null && selectionUI.ConfirmButton != null)
                selectionUI.ConfirmButton.onClick.AddListener(HandleConfirmClicked);

            if (deckController != null)
                deckController.OnPilesChanged += HandlePilesChanged;

            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (selectionUI != null && selectionUI.ConfirmButton != null)
                selectionUI.ConfirmButton.onClick.RemoveListener(HandleConfirmClicked);

            if (deckController != null)
                deckController.OnPilesChanged -= HandlePilesChanged;

            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            ForceCancelSelectionInternal(discardSelectedCards: false);
        }

        private void OnDestroy()
        {
            ForceCancelSelectionInternal(discardSelectedCards: false);
        }

        /// <summary>
        /// Waitable manual discard sequence used by the sequential effect runner.
        /// </summary>
        public IEnumerator SelectAndDiscardRoutine(int requestedDiscardCount)
        {
            int requested = Mathf.Max(0, requestedDiscardCount);
            RebuildCandidatesFromCurrentHand();
            int required = Mathf.Min(requested, CountValidCandidates());

            if (verboseLogs)
            {
                Debug.Log(
                    $"[HandSelection] Begin discard Requested={requested} Required={required} " +
                    $"Hand={CountValidCandidates()}");
            }

            if (required <= 0)
                yield break;

            BeginSelection(HandCardSelectionPurpose.ManualDiscard, candidateCards, requested, discardInstruction);

            confirmRequested = false;
            forceCancelled = false;

            while (isSelecting && !confirmRequested && !forceCancelled)
                yield return null;

            if (forceCancelled || !isSelecting)
            {
                EndSelectionInternal(confirmed: false);
                yield break;
            }

            confirmedSnapshot.Clear();
            for (int i = 0; i < selectedCards.Count; i++)
            {
                var card = selectedCards[i];
                if (card != null)
                    confirmedSnapshot.Add(card);
            }

            OnSelectionConfirmed?.Invoke();

            // Capture views for optional VFX before pile mutation / selection exit.
            var viewsForVfx = new List<CardViewUI>(confirmedSnapshot.Count);
            if (handUIController != null)
            {
                for (int i = 0; i < confirmedSnapshot.Count; i++)
                {
                    var view = handUIController.GetViewForCard(confirmedSnapshot[i]);
                    if (view != null)
                        viewsForVfx.Add(view);
                }
            }

            EndSelectionInternal(confirmed: true);

            if (graveyardVfx != null && viewsForVfx.Count > 0)
                graveyardVfx.PlayBatchCardsToGraveyard(viewsForVfx);

            int moved = deckController != null
                ? deckController.DiscardCardsFromHand(confirmedSnapshot)
                : 0;

            if (graveyardVfx == null)
                pileCounterUI?.ForceSyncDisplayedToReal();

            handUIController?.SyncHandViewsExternal();
            handUIController?.RefreshInteractivityExternal();

            if (verboseLogs)
                Debug.Log($"[HandSelection] Discarded {moved} card(s).");

            confirmedSnapshot.Clear();
        }

        public void BeginSelection(
            HandCardSelectionPurpose selectionPurpose,
            IReadOnlyList<CardInstance> candidates,
            int requested,
            string instruction = null)
        {
            // Snapshot first so callers may pass candidateCards itself without alias wipe.
            var incomingSnapshot = new List<CardInstance>();
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    var card = candidates[i];
                    if (card?.Data == null)
                        continue;
                    if (deckController != null && !deckController.IsInHand(card))
                        continue;
                    if (!incomingSnapshot.Contains(card))
                        incomingSnapshot.Add(card);
                }
            }

            ForceCancelSelectionInternal(discardSelectedCards: false);

            purpose = selectionPurpose;
            requestedCount = Mathf.Max(0, requested);
            selectedCards.Clear();
            candidateCards.Clear();
            candidateCards.AddRange(incomingSnapshot);

            requiredCount = Mathf.Min(requestedCount, candidateCards.Count);
            if (requiredCount <= 0)
            {
                selectionUI?.HideImmediate();
                return;
            }

            isSelecting = true;
            confirmRequested = false;
            forceCancelled = false;

            handUIController?.BeginHandSelection(this);
            selectionUI?.Show(
                string.IsNullOrEmpty(instruction) ? discardInstruction : instruction,
                SelectedCount,
                requiredCount);

            RefreshSelectionVisuals();
            OnSelectionStarted?.Invoke();
            OnSelectionChanged?.Invoke();
        }

        public bool TryToggleCard(CardInstance card)
        {
            if (!isSelecting || card?.Data == null)
                return false;

            if (!candidateCards.Contains(card))
                return false;

            if (deckController != null && !deckController.IsInHand(card))
            {
                RefreshCandidatesAfterPileChange();
                return false;
            }

            int index = selectedCards.IndexOf(card);
            if (index >= 0)
            {
                selectedCards.RemoveAt(index);
            }
            else
            {
                if (selectedCards.Count >= requiredCount)
                    return false;

                selectedCards.Add(card);
            }

            selectionUI?.RefreshCount(SelectedCount, requiredCount);
            RefreshSelectionVisuals();
            OnSelectionChanged?.Invoke();
            return true;
        }

        public void ConfirmSelection()
        {
            if (!isSelecting)
                return;

            if (SelectedCount != requiredCount)
                return;

            selectionUI?.SetConfirmInteractable(false);
            confirmRequested = true;
        }

        /// <summary>Cancel selection without discarding. Used by battle reset / battle end.</summary>
        public void ForceCancelSelection()
        {
            ForceCancelSelectionInternal(discardSelectedCards: false);
        }

        public void RefreshSelectionVisuals()
        {
            if (!isSelecting)
            {
                handUIController?.EndHandSelection();
                return;
            }

            handUIController?.RefreshHandSelectionVisuals(
                candidateCards,
                selectedCards,
                requiredCount);
        }

        private void HandleConfirmClicked()
        {
            ConfirmSelection();
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            ForceCancelSelectionInternal(discardSelectedCards: false);
        }

        private void HandlePilesChanged()
        {
            if (!isSelecting)
                return;

            RefreshCandidatesAfterPileChange();
        }

        private void RefreshCandidatesAfterPileChange()
        {
            for (int i = candidateCards.Count - 1; i >= 0; i--)
            {
                var card = candidateCards[i];
                if (card == null || deckController == null || !deckController.IsInHand(card))
                    candidateCards.RemoveAt(i);
            }

            for (int i = selectedCards.Count - 1; i >= 0; i--)
            {
                var card = selectedCards[i];
                if (card == null || !candidateCards.Contains(card))
                    selectedCards.RemoveAt(i);
            }

            // Newly drawn cards should join the candidate set while still selecting.
            if (deckController != null)
            {
                var hand = deckController.Hand;
                for (int i = 0; i < hand.Count; i++)
                {
                    var card = hand[i];
                    if (card?.Data == null)
                        continue;
                    if (!candidateCards.Contains(card))
                        candidateCards.Add(card);
                }
            }

            int newRequired = Mathf.Min(requestedCount, candidateCards.Count);
            if (newRequired != requiredCount)
            {
                requiredCount = newRequired;
                while (selectedCards.Count > requiredCount && selectedCards.Count > 0)
                    selectedCards.RemoveAt(selectedCards.Count - 1);
            }

            if (requiredCount <= 0)
            {
                // Nothing left to select: complete as a soft cancel/skip without locking.
                confirmRequested = true;
                EndSelectionInternal(confirmed: false);
                return;
            }

            selectionUI?.RefreshCount(SelectedCount, requiredCount);
            RefreshSelectionVisuals();
            OnSelectionChanged?.Invoke();
        }

        private void RebuildCandidatesFromCurrentHand()
        {
            candidateCards.Clear();
            if (deckController == null)
                return;

            var hand = deckController.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data != null)
                    candidateCards.Add(card);
            }
        }

        private int CountValidCandidates()
        {
            int count = 0;
            for (int i = 0; i < candidateCards.Count; i++)
            {
                if (candidateCards[i]?.Data != null)
                    count++;
            }

            return count;
        }

        private void ForceCancelSelectionInternal(bool discardSelectedCards)
        {
            if (!isSelecting && selectedCards.Count == 0 && (selectionUI == null || !selectionUI.IsVisible))
                return;

            forceCancelled = true;
            confirmRequested = false;

            if (discardSelectedCards && selectedCards.Count > 0 && deckController != null)
                deckController.DiscardCardsFromHand(selectedCards);

            EndSelectionInternal(confirmed: false);
        }

        private void EndSelectionInternal(bool confirmed)
        {
            bool wasSelecting = isSelecting;
            isSelecting = false;

            selectedCards.Clear();
            candidateCards.Clear();
            requestedCount = 0;
            requiredCount = 0;

            selectionUI?.HideImmediate();
            handUIController?.EndHandSelection();

            if (wasSelecting)
                OnSelectionEnded?.Invoke();
        }
    }
}
