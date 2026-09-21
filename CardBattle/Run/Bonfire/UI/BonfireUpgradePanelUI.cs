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
        private bool showingPreview;
        private bool resolving;
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
            if (bonfireController != controller) showingPreview = false;
            Unsubscribe(); bonfireController = controller;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }
        private void OnEnable()
        {
            RemoveInputListeners();
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
            RemoveInputListeners();
            showingPreview = false;
            ClearRows(); if (panelRoot != null) panelRoot.SetActive(false);
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
        private void Subscribe() { Unsubscribe(); subscribed = bonfireController; if (subscribed != null) subscribed.OnStateChanged += Refresh; }
        private void Unsubscribe() { if (subscribed != null) subscribed.OnStateChanged -= Refresh; subscribed = null; }
        private void HandleBonus(string id) { bonfireController?.TrySelectBonus(id); Refresh(); }
        private void HandleApplyBonus() { bonfireController?.TryApplySelectedBonus(); Refresh(); }
        // Grid Back exits to Bonfire choices; Preview Back only changes this presentation state.
        private void HandleBack()
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.DeckSelection || resolving) return;
            bonfireController?.TryBackFromUpgrade(); Refresh();
        }
        private void HandlePreviewBack()
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.PreviewSelection || bonfireController == null || !bonfireController.CanBackFromUpgrade) return;
            showingPreview = false; Refresh();
        }
        private void HandleConfirm()
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.PreviewSelection || resolving || bonfireController == null || !bonfireController.CanConfirmUpgradeCard) return;
            resolving = true; Refresh();
            try { bonfireController.TryConfirmUpgradeCard(); }
            finally { resolving = false; Refresh(); }
        }
        private void HandleRetry() { bonfireController?.TryRetrySave(); Refresh(); }
        private void HandleCard(string id)
        {
            if (!isActiveAndEnabled || State != UpgradeUIState.DeckSelection || resolving || bonfireController == null) return;
            showingPreview = true;
            if (!bonfireController.TrySelectUpgradeCard(id)) showingPreview = false;
            Refresh();
        }

        public void Refresh()
        {
            var state = bonfireController != null ? bonfireController.State : BonfireController.SessionState.Inactive;
            bool selecting = state == BonfireController.SessionState.UpgradeCardSelection;
            bool committed = state == BonfireController.SessionState.UpgradeCommitted;
            bool applied = bonfireController != null && bonfireController.HasAppliedUpgrade;
            bool visible = isActiveAndEnabled && (selecting || committed || applied || state == BonfireController.SessionState.InvalidUpgrade);
            if (panelRoot != null) panelRoot.SetActive(visible);
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
                    rows[i].Bind(card, data, card != null && eligible.Contains(card.runCardInstanceId) && !bonfireController.IsBusy,
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
            if (previewRoot != null) previewRoot.SetActive(hasPreview && (State == UpgradeUIState.PreviewSelection || resolving) && !applied);
            if (hasPreview && previewScreen != null && bonfireController.TryResolveUpgradeCard(selectedId, out var current))
                previewScreen.Bind(current, definition, bonfireController.MutationChance);
            if (selectedNameText != null) selectedNameText.text = hasPreview ? baseData.DisplayName : "Select a card";
            if (currentText != null) currentText.text = hasPreview ? $"Current — {baseData.ApCost} AP\n{CardDescriptionBuilder.Build(baseData)}" : "";
            if (upgradeText != null) upgradeText.text = hasPreview ? $"Guaranteed Upgrade — {baseData.ApCost} AP\n{CardDescriptionBuilder.BuildGuaranteedUpgrade(baseData, definition)}\n\nCurse Mutation chance: {bonfireController.MutationChance:P0}" : "";
            if (bonusRoot != null) bonusRoot.SetActive(committed);
            var offers = bonfireController.GetOfferedBonusIds();
            if (bonusChoices != null)
                for (int i = 0; i < bonusChoices.Length; i++)
                {
                    var view = bonusChoices[i]; if (view == null) continue;
                    bool show = committed && i < offers.Count && bonfireController.TryGetOfferedBonus(offers[i], out _);
                    view.gameObject.SetActive(show);
                    if (show && bonfireController.TryGetOfferedBonus(offers[i], out var bonus))
                        view.Bind(bonus, bonfireController.SelectedBonusId == offers[i], bonfireController.CanSelectBonus);
                }
            if (applyBonusButton != null) { applyBonusButton.gameObject.SetActive(committed); applyBonusButton.interactable = bonfireController.CanApplySelectedBonus; }
            if (backButton != null) { backButton.gameObject.SetActive(State == UpgradeUIState.DeckSelection); backButton.interactable = bonfireController.CanBackFromUpgrade && !resolving; }
            if (previewBackButton != null) { previewBackButton.gameObject.SetActive(State == UpgradeUIState.PreviewSelection); previewBackButton.interactable = bonfireController.CanBackFromUpgrade && !resolving; }
            if (confirmButton != null) { confirmButton.gameObject.SetActive(State == UpgradeUIState.PreviewSelection); confirmButton.interactable = State == UpgradeUIState.PreviewSelection && bonfireController.CanConfirmUpgradeCard && !resolving; }
            if (retryButton != null) { retryButton.gameObject.SetActive(bonfireController.CanRetrySave); retryButton.interactable = bonfireController.CanRetrySave; }
            if (statusText != null) statusText.text = applied ? "Upgrade applied. Save retry required.\n" + bonfireController.LastError : committed
                ? bonfireController.CanRetrySave ? "Card committed. Save retry required.\n" + bonfireController.LastError : bonusRoot != null ? "Card locked — select a Bonus, then Confirm Bonus." : "Card locked — waiting for Bonus choice."
                : !string.IsNullOrEmpty(bonfireController.LastError) ? bonfireController.LastError : resolving ? "Resolving Upgrade…" : State == UpgradeUIState.PreviewSelection ? "" : "Choose one eligible card.";
        }

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
