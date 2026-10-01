using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    // Keep this component on an always-active host, outside panelRoot.
    public class BonfireUpgradePanelUI : MonoBehaviour
    {
        public enum UpgradeUIState { DeckSelection, PreviewSelection, ResolvingUpgrade, Completed }
        public UpgradeUIState State { get; private set; } = UpgradeUIState.Completed;
        [SerializeField] private BonfireUpgradePreviewUI previewScreen;
        [SerializeField] private Button previewBackButton;
        [SerializeField] private BonfireUpgradePresentation presentation;
        [SerializeField] private BonfirePresentationBackgroundDissolve backgroundDissolve;

        [Header("Deck Content Transition")]
        [SerializeField] private CanvasGroup deckSelectionCanvasGroup;
        [SerializeField, InspectorName("Deck Content Fade In Delay"), Min(0f)]
        [Tooltip("Delay after the background transition starts before DeckSelection begins fading in.")]
        private float deckContentFadeInDelay = .08f;
        [SerializeField, InspectorName("Deck Content Fade In Duration"), Min(0f)]
        [Tooltip("Duration of the top-level DeckSelection entry fade.")]
        private float deckContentFadeInDuration = .18f;
        [SerializeField, InspectorName("Deck Content Fade Out Duration"), Min(0f)]
        [Tooltip("Duration of the top-level DeckSelection exit fade.")]
        private float deckContentFadeOutDuration = .2f;
        [SerializeField, InspectorName("Background Start Delay"), Min(0f)]
        [Tooltip("Delay from the start of DeckSelection exit before the background transition begins.")]
        private float backgroundStartDelay = .08f;
        private string confirmingCardId;
        private bool showingPreview;
        private bool resolving;
        private bool backgroundSessionOpen;
        private bool backExitInProgress;
        private bool backBackgroundFadeCompleted;
        private bool deckEntryInProgress;
        private Coroutine deckEntryRoutine;
        private Coroutine backExitRoutine;
        [SerializeField] private BonfireController bonfireController;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject deckRoot;
        [SerializeField] private Transform content;
        [SerializeField] private UpgradeCardChoiceView cardTemplate;
        [SerializeField] private UpgradeCardChoiceView lockedCardView;
        [SerializeField] private GameObject previewRoot;
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI currentText;
        [SerializeField] private TextMeshProUGUI upgradeText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private GameObject bonusRoot;
        [SerializeField] private BonusUpgradeChoiceView[] bonusChoices;
        [SerializeField] private Button applyBonusButton;
        private BonfireController subscribed;
        private readonly List<UpgradeCardChoiceView> rows = new List<UpgradeCardChoiceView>();
        private readonly List<RunCardRecord> displayed = new List<RunCardRecord>();
        public int DisplayedCardCount => rows.Count;

        public void BindController(BonfireController controller)
        {
            CancelBackExit();
            if (bonfireController != controller) { showingPreview = false; presentation?.ResetPresentation(); }
            Unsubscribe(); bonfireController = controller;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }
        private void OnEnable()
        {
            RemoveInputListeners();
            if (presentation != null)
            {
                presentation.Held -= Refresh; presentation.Held += Refresh;
                presentation.Finished -= Refresh; presentation.Finished += Refresh;
            }
            Subscribe();
            if (previewBackButton != null) previewBackButton.onClick.AddListener(HandlePreviewBack);
            if (bonusChoices != null) foreach (var view in bonusChoices) if (view != null) view.OnSelected += HandleBonus;
            if (applyBonusButton != null) applyBonusButton.onClick.AddListener(HandleApplyBonus);
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (retryButton != null) retryButton.onClick.AddListener(HandleRetry);
            Refresh();
        }
        private void OnDisable()
        {
            Unsubscribe();
            if (presentation != null)
            {
                presentation.Held -= Refresh;
                presentation.Finished -= Refresh;
                presentation.ResetPresentation();
            }
            RemoveInputListeners();
            CancelBackExit();
            showingPreview = false;
            ClearRows(); SetPanelVisible(false);
        }
        private void RemoveInputListeners()
        {
            if (previewBackButton != null) previewBackButton.onClick.RemoveListener(HandlePreviewBack);
            if (bonusChoices != null) foreach (var view in bonusChoices) if (view != null) view.OnSelected -= HandleBonus;
            if (applyBonusButton != null) applyBonusButton.onClick.RemoveListener(HandleApplyBonus);
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirm);
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetry);
        }
        private void Subscribe() { Unsubscribe(); subscribed = bonfireController; if (subscribed != null) { subscribed.OnStateChanged += Refresh; subscribed.OnUpgradeResolved += HandleUpgradeResolved; } }
        private void Unsubscribe() { if (subscribed != null) { subscribed.OnStateChanged -= Refresh; subscribed.OnUpgradeResolved -= HandleUpgradeResolved; } subscribed = null; }
        private void HandleUpgradeResolved(UpgradeResolution result)
        {
            if (resolving && result.RunCardInstanceId == confirmingCardId && presentation != null)
            {
                if (presentation.TryBegin(result)) transform.SetAsLastSibling();
            }
        }
        private void HandleBonus(string id) { bonfireController?.TrySelectBonus(id); Refresh(); }
        private void HandleApplyBonus() { bonfireController?.TryApplySelectedBonus(); Refresh(); }
        // Grid Back exits to Bonfire choices; Preview Back only changes this presentation state.
        private void HandleBack()
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.DeckSelection || resolving || backExitInProgress || deckEntryInProgress ||
                bonfireController == null || !bonfireController.CanBackFromUpgrade) return;
            backExitInProgress = true;
            SetDeckContentInput(false);
            Refresh();
            if (!Application.isPlaying)
            {
                if (deckSelectionCanvasGroup != null) deckSelectionCanvasGroup.alpha = 0f;
                BeginBackBackgroundFade();
                CompleteBackExit();
                return;
            }
            backExitRoutine = StartCoroutine(BackExitSequence());
        }

        private IEnumerator BackExitSequence()
        {
            float elapsed = 0f;
            float contentDuration = Mathf.Max(0f, deckContentFadeOutDuration);
            float delay = Mathf.Max(0f, backgroundStartDelay);
            bool backgroundStarted = false;
            backBackgroundFadeCompleted = false;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float contentProgress = contentDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / contentDuration);
                if (deckSelectionCanvasGroup != null) deckSelectionCanvasGroup.alpha = 1f - contentProgress;
                if (!backgroundStarted && elapsed >= delay)
                {
                    backgroundStarted = true;
                    BeginBackBackgroundFade();
                }
                if (contentProgress >= 1f && backgroundStarted && backBackgroundFadeCompleted) break;
                yield return null;
            }
            backExitRoutine = null;
            CompleteBackExit();
        }

        private void BeginBackBackgroundFade()
        {
            if (backgroundDissolve == null)
            {
                backBackgroundFadeCompleted = true;
                return;
            }
            backgroundDissolve.FadeOutCompleted -= HandleBackBackgroundFadeCompleted;
            backgroundDissolve.FadeOutCompleted += HandleBackBackgroundFadeCompleted;
            backgroundDissolve.PlayFadeOut();
        }

        private void HandleBackBackgroundFadeCompleted()
        {
            if (backgroundDissolve != null) backgroundDissolve.FadeOutCompleted -= HandleBackBackgroundFadeCompleted;
            backBackgroundFadeCompleted = true;
        }

        private void CompleteBackExit()
        {
            if (!backExitInProgress) return;
            bool completed = bonfireController != null && bonfireController.TryBackFromUpgrade();
            backExitInProgress = false;
            if (!completed)
            {
                ResetDeckContent();
                if (backgroundDissolve != null) backgroundDissolve.PlayFadeIn();
            }
            Refresh();
        }

        private void CancelBackExit()
        {
            if (backExitRoutine != null) StopCoroutine(backExitRoutine);
            backExitRoutine = null;
            if (backgroundDissolve != null) backgroundDissolve.FadeOutCompleted -= HandleBackBackgroundFadeCompleted;
            backExitInProgress = false;
            backBackgroundFadeCompleted = false;
        }
        private void HandlePreviewBack()
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.PreviewSelection || backExitInProgress || bonfireController == null || !bonfireController.CanBackFromUpgrade) return;
            showingPreview = false; Refresh();
        }
        private void HandleConfirm()
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.PreviewSelection || resolving || backExitInProgress || bonfireController == null || !bonfireController.CanConfirmUpgradeCard) return;
            confirmingCardId = bonfireController.SelectedRunCardInstanceId;
            resolving = true; Refresh();
            try
            {
                if (bonfireController.TryConfirmUpgradeCard() && bonfireController.IsWaitingForMutationChoice &&
                    presentation != null && presentation.TryBeginMutationChoice(bonfireController, confirmingCardId))
                    transform.SetAsLastSibling();
            }
            finally { resolving = false; confirmingCardId = null; Refresh(); }
        }
        private void HandleRetry() { bonfireController?.TryRetrySave(); Refresh(); }
        private void HandleCard(string id)
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.DeckSelection || resolving || backExitInProgress || bonfireController == null) return;
            showingPreview = true;
            if (!bonfireController.TrySelectUpgradeCard(id)) showingPreview = false;
            Refresh();
        }

        public void Refresh()
        {
            var state = bonfireController != null ? bonfireController.State : BonfireController.SessionState.Inactive;
            // Keep the already-bound Guaranteed preview independent from synchronous apply/save callbacks.
            // The existing controller may restore Map underneath; this modal remains held until explicit teardown.
            if (!resolving && presentation != null && presentation.IsActive &&
                (state == BonfireController.SessionState.Choice || state == BonfireController.SessionState.UpgradeCardSelection))
                presentation.ResetPresentation();
            if (isActiveAndEnabled && presentation != null &&
                (presentation.IsActive || (resolving && presentation.IsConfigured)))
            {
                State = presentation.IsHolding ? UpgradeUIState.Completed : UpgradeUIState.ResolvingUpgrade;
                SetPanelVisible(true);
                if (deckRoot != null) deckRoot.SetActive(false);
                if (previewRoot != null) previewRoot.SetActive(true);
                if (backButton != null) backButton.interactable = false;
                if (previewBackButton != null) previewBackButton.interactable = false;
                if (confirmButton != null) confirmButton.interactable = false;
                if (lockedCardView != null) lockedCardView.gameObject.SetActive(false);
                if (bonusRoot != null) bonusRoot.SetActive(false);
                if (retryButton != null) { retryButton.gameObject.SetActive(bonfireController != null && bonfireController.CanRetrySave); retryButton.interactable = bonfireController != null && bonfireController.CanRetrySave; }
                if (statusText != null) statusText.text = bonfireController != null ? bonfireController.LastError : "";
                return;
            }
            bool selecting = state == BonfireController.SessionState.UpgradeCardSelection;
            bool committed = state == BonfireController.SessionState.UpgradeCommitted;
            bool mutationPending = committed && bonfireController != null && bonfireController.HasPendingMutationChoice;
            bool applied = bonfireController != null && bonfireController.HasAppliedUpgrade;
            bool visible = isActiveAndEnabled && (selecting || committed || applied || state == BonfireController.SessionState.InvalidUpgrade);
            SetPanelVisible(visible);
            if (!visible) { showingPreview = false; State = UpgradeUIState.Completed; ClearRows(); return; }
            bool validSelection = selecting && bonfireController.CanConfirmUpgradeCard;
            if (!selecting && !resolving) showingPreview = false;
            State = resolving || committed ? UpgradeUIState.ResolvingUpgrade : selecting
                ? showingPreview && validSelection ? UpgradeUIState.PreviewSelection : UpgradeUIState.DeckSelection
                : UpgradeUIState.Completed;
            if (deckRoot != null) deckRoot.SetActive(State == UpgradeUIState.DeckSelection);
            if (selecting)
            {
                var cards = bonfireController.GetRunDeckSnapshot(); EnsureRows(cards);
                var eligible = new HashSet<string>();
                foreach (var card in bonfireController.GetEligibleUpgradeCards()) eligible.Add(card.runCardInstanceId);
                for (int i = 0; i < rows.Count; i++)
                {
                    var card = cards[i];
                    bonfireController.TryGetUpgradeCardData(card?.runCardInstanceId, out var data, out _);
                    bonfireController.TryResolveUpgradeCard(card?.runCardInstanceId, out var resolved);
                    rows[i].Bind(card, data, card != null && eligible.Contains(card.runCardInstanceId) && !bonfireController.IsBusy && !backExitInProgress,
                        card != null && card.runCardInstanceId == bonfireController.SelectedRunCardInstanceId, false,
                        resolved != null ? CardDescriptionBuilder.BuildForInstance(resolved) : null, resolved?.EffectiveApCost);
                }
            }
            else ClearRows();
            var locked = committed || applied ? bonfireController.GetCommittedUpgradeCardSnapshot() : null;
            if (lockedCardView != null)
            {
                lockedCardView.gameObject.SetActive(locked != null);
                if (locked != null)
                {
                    bonfireController.TryGetUpgradeCardData(locked.runCardInstanceId, out var data, out var lockedUpgrade);
                    lockedCardView.Bind(locked, data, false, true, true, applied ? bonfireController.GetAppliedUpgradeDescription() : CardDescriptionBuilder.BuildGuaranteedUpgrade(data, lockedUpgrade), applied ? bonfireController.GetAppliedUpgradeApCost() : null);
                }
            }
            string selectedId = selecting ? bonfireController.SelectedRunCardInstanceId : locked?.runCardInstanceId;
            bool hasPreview = bonfireController.TryGetUpgradeCardData(selectedId, out var baseData, out var definition) && definition != null && definition.HasValidSequence;
            if (previewRoot != null) previewRoot.SetActive(hasPreview &&
                (State == UpgradeUIState.PreviewSelection || resolving || mutationPending) && !applied);
            if (hasPreview && previewScreen != null && bonfireController.TryResolveUpgradeCard(selectedId, out var current))
                previewScreen.Bind(current, definition, mutationPending ? bonfireController.PendingMutationChance : bonfireController.MutationChance);
            if (selectedNameText != null) selectedNameText.text = hasPreview ? baseData.DisplayName : "Select a card";
            if (currentText != null) currentText.text = hasPreview ? $"Current — {baseData.ApCost} AP\n{CardDescriptionBuilder.Build(baseData)}" : "";
            if (upgradeText != null) upgradeText.text = hasPreview ? $"Guaranteed Upgrade — {baseData.ApCost} AP\n{CardDescriptionBuilder.BuildGuaranteedUpgrade(baseData, definition)}\n\nCurse Mutation chance: {bonfireController.MutationChance:P0}" : "";
            if (bonusRoot != null) bonusRoot.SetActive(committed && !mutationPending);
            var offers = bonfireController.GetOfferedBonusIds();
            if (bonusChoices != null)
                for (int i = 0; i < bonusChoices.Length; i++)
                {
                    var view = bonusChoices[i]; if (view == null) continue;
                    bool show = committed && !mutationPending && i < offers.Count && bonfireController.TryGetOfferedBonus(offers[i], out _);
                    view.gameObject.SetActive(show);
                    if (show && bonfireController.TryGetOfferedBonus(offers[i], out var bonus))
                        view.Bind(bonus, bonfireController.SelectedBonusId == offers[i], bonfireController.CanSelectBonus);
                }
            if (applyBonusButton != null) { applyBonusButton.gameObject.SetActive(committed && !mutationPending); applyBonusButton.interactable = bonfireController.CanApplySelectedBonus; }
            if (backButton != null) { backButton.gameObject.SetActive(State == UpgradeUIState.DeckSelection); backButton.interactable = bonfireController.CanBackFromUpgrade && !resolving && !backExitInProgress; }
            if (previewBackButton != null) { previewBackButton.gameObject.SetActive(State == UpgradeUIState.PreviewSelection); previewBackButton.interactable = bonfireController.CanBackFromUpgrade && !resolving && !backExitInProgress; }
            if (confirmButton != null) { confirmButton.gameObject.SetActive(State == UpgradeUIState.PreviewSelection); confirmButton.interactable = State == UpgradeUIState.PreviewSelection && bonfireController.CanConfirmUpgradeCard && !resolving && !backExitInProgress; }
            if (retryButton != null) { retryButton.gameObject.SetActive(bonfireController.CanRetrySave); retryButton.interactable = bonfireController.CanRetrySave; }
            if (statusText != null) statusText.text = applied ? "Upgrade applied. Save retry required.\n" + bonfireController.LastError : mutationPending
                ? bonfireController.CanRetrySave ? "Mutation offers committed. Save retry required.\n" + bonfireController.LastError : "Choose one Mutation."
                : committed ? bonfireController.CanRetrySave ? "Card committed. Save retry required.\n" + bonfireController.LastError : bonusRoot != null ? "Card locked — select a Bonus, then Confirm Bonus." : "Card locked — waiting for Bonus choice."
                : !string.IsNullOrEmpty(bonfireController.LastError) ? bonfireController.LastError : resolving ? "Resolving Upgrade…" : State == UpgradeUIState.PreviewSelection ? "" : "Choose one eligible card.";

            // Restore/retry may reach the persisted Mutation wait without passing through this UI's Confirm handler.
            if (mutationPending && bonfireController.IsWaitingForMutationChoice && hasPreview && presentation != null &&
                !presentation.IsActive && presentation.TryBeginMutationChoice(bonfireController, locked.runCardInstanceId))
            {
                transform.SetAsLastSibling();
                Refresh();
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null) panelRoot.SetActive(visible);
            if (visible)
            {
                if (backgroundSessionOpen) return;
                backgroundSessionOpen = true;
                PrepareDeckContentEntry();
                if (backgroundDissolve != null)
                {
                    backgroundDissolve.SetReveal(0f);
                    backgroundDissolve.PlayFadeIn();
                }
                BeginDeckContentEntry();
                return;
            }

            CancelDeckContentEntry();
            backgroundSessionOpen = false;
            if (backgroundDissolve != null) backgroundDissolve.SetReveal(0f);
        }

        private void PrepareDeckContentEntry()
        {
            if (deckSelectionCanvasGroup == null && deckRoot != null)
                deckSelectionCanvasGroup = deckRoot.GetComponent<CanvasGroup>();
            if (deckSelectionCanvasGroup == null) return;
            deckSelectionCanvasGroup.alpha = 0f;
            SetDeckContentInput(false);
        }

        private void BeginDeckContentEntry()
        {
            CancelDeckContentEntry();
            PrepareDeckContentEntry();
            if (!Application.isPlaying || (deckContentFadeInDelay <= 0f && deckContentFadeInDuration <= 0f))
            {
                ResetDeckContent();
                return;
            }
            deckEntryInProgress = true;
            deckEntryRoutine = StartCoroutine(DeckContentEntrySequence());
        }

        private IEnumerator DeckContentEntrySequence()
        {
            float elapsed = 0f;
            float delay = Mathf.Max(0f, deckContentFadeInDelay);
            float duration = Mathf.Max(0f, deckContentFadeInDuration);
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (deckSelectionCanvasGroup != null)
                    deckSelectionCanvasGroup.alpha = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            deckEntryRoutine = null;
            deckEntryInProgress = false;
            ResetDeckContent();
        }

        private void CancelDeckContentEntry()
        {
            if (deckEntryRoutine != null) StopCoroutine(deckEntryRoutine);
            deckEntryRoutine = null;
            deckEntryInProgress = false;
        }

        private void ResetDeckContent()
        {
            if (deckSelectionCanvasGroup == null && deckRoot != null)
                deckSelectionCanvasGroup = deckRoot.GetComponent<CanvasGroup>();
            if (deckSelectionCanvasGroup == null) return;
            deckSelectionCanvasGroup.alpha = 1f;
            SetDeckContentInput(true);
        }

        private void SetDeckContentInput(bool enabled)
        {
            if (deckSelectionCanvasGroup == null && deckRoot != null)
                deckSelectionCanvasGroup = deckRoot.GetComponent<CanvasGroup>();
            if (deckSelectionCanvasGroup == null) return;
            deckSelectionCanvasGroup.interactable = enabled;
            deckSelectionCanvasGroup.blocksRaycasts = enabled;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            deckContentFadeOutDuration = Mathf.Max(0f, deckContentFadeOutDuration);
            deckContentFadeInDelay = Mathf.Max(0f, deckContentFadeInDelay);
            deckContentFadeInDuration = Mathf.Max(0f, deckContentFadeInDuration);
            backgroundStartDelay = Mathf.Max(0f, backgroundStartDelay);
            if (deckSelectionCanvasGroup == null && deckRoot != null)
                deckSelectionCanvasGroup = deckRoot.GetComponent<CanvasGroup>();
        }
#endif

        private void EnsureRows(IReadOnlyList<RunCardRecord> cards)
        {
            bool same = cards.Count == displayed.Count && rows.Count == cards.Count;
            for (int i = 0; same && i < cards.Count; i++)
                same = cards[i]?.runCardInstanceId == displayed[i]?.runCardInstanceId && cards[i]?.cardId == displayed[i]?.cardId &&
                    cards[i]?.upgradeLevel == displayed[i]?.upgradeLevel && cards[i]?.selectedBonusUpgradeId == displayed[i]?.selectedBonusUpgradeId;
            if (same) return;
            ClearRows();
            if (cardTemplate == null || content == null) return;
            foreach (var card in cards)
            {
                var view = Instantiate(cardTemplate, content); view.gameObject.SetActive(true);
                view.OnSelected += HandleCard; rows.Add(view); displayed.Add(card?.Clone());
            }
        }
        private void ClearRows()
        {
            foreach (var row in rows)
            {
                row.OnSelected -= HandleCard; row.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(row.gameObject); else DestroyImmediate(row.gameObject);
            }
            rows.Clear(); displayed.Clear();
        }
    }
}
