using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class RewardPanelUI : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] private RewardController rewardController;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Reward Summary")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI goldAmountText;
        [SerializeField] private GameObject goldRoot;

        [Header("Card Choices")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private RewardCardChoiceView choicePrefab;
        [SerializeField] private GameObject noChoicesRoot;

        [Header("Actions")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Options")]
        [SerializeField] private bool hidePanelOnStart = true;
        [SerializeField] private bool disableNonSelectedCardsAfterResolution = true;
        [SerializeField] private bool verboseLogs;

        public bool IsVisible { get; private set; }
        public bool IsAwaitingChoice { get; private set; }
        public bool IsCompletedState { get; private set; }

        public event Action OnContinueRequested;

        private readonly List<RewardCardChoiceView> spawnedChoices = new List<RewardCardChoiceView>();
        private RewardController subscribedRewardController;

        private void Awake()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipClicked);
                skipButton.onClick.AddListener(HandleSkipClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinueClicked);
                continueButton.onClick.AddListener(HandleContinueClicked);
            }
        }

        private void Start()
        {
            if (hidePanelOnStart)
                HidePanel();
            else
                SetContinueVisible(false);

            if (rewardController != null && rewardController.CurrentSession != null)
                ShowSession(rewardController.CurrentSession);
        }

        private void OnEnable()
        {
            RefreshRewardSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeRewardController();
        }

        private void OnDestroy()
        {
            UnsubscribeRewardController();

            if (skipButton != null)
                skipButton.onClick.RemoveListener(HandleSkipClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinueClicked);

            ClearSpawnedChoices();
        }

        private void RefreshRewardSubscription()
        {
            UnsubscribeRewardController();

            if (rewardController == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardPanelUI] RewardController reference is missing. Event subscription skipped.");
                }

                return;
            }

            subscribedRewardController = rewardController;
            subscribedRewardController.OnRewardSessionStarted += HandleRewardSessionStarted;
            subscribedRewardController.OnRewardCardSelected += HandleRewardCardSelected;
            subscribedRewardController.OnRewardCardSkipped += HandleRewardCardSkipped;
            subscribedRewardController.OnRewardSessionCompleted += HandleRewardSessionCompleted;
        }

        private void UnsubscribeRewardController()
        {
            if (subscribedRewardController == null)
                return;

            subscribedRewardController.OnRewardSessionStarted -= HandleRewardSessionStarted;
            subscribedRewardController.OnRewardCardSelected -= HandleRewardCardSelected;
            subscribedRewardController.OnRewardCardSkipped -= HandleRewardCardSkipped;
            subscribedRewardController.OnRewardSessionCompleted -= HandleRewardSessionCompleted;
            subscribedRewardController = null;
        }

        private void HandleRewardSessionStarted(RewardSession session)
        {
            ShowSession(session);
        }

        public void ShowSession(RewardSession session)
        {
            if (session == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[RewardPanelUI] ShowSession called with null session.");

                HidePanel();
                return;
            }

            if (panelRoot == null)
            {
                Debug.LogError("[RewardPanelUI] Panel Root reference is missing.");
                return;
            }

            ClearSpawnedChoices();

            panelRoot.SetActive(true);
            IsVisible = true;

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }

            IsAwaitingChoice = !session.CardChoiceResolved;
            IsCompletedState = session.IsComplete;

            if (titleText != null)
                titleText.text = "Reward";

            UpdateGoldDisplay(session.GoldAmount);
            BuildChoiceViews(session);
            UpdateNoChoicesDisplay(session.ChoiceCount == 0);
            UpdateResultTextForSession(session);

            if (session.IsComplete)
                ApplyCompletedState(session);
            else
            {
                SetContinueVisible(false);
                SetChoiceInteraction(true);
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RewardPanelUI] ShowSession | Gold={session.GoldAmount} | " +
                    $"Choices={session.ChoiceCount} | Awaiting={IsAwaitingChoice} | Complete={IsCompletedState}");
            }
        }

        public void HidePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            IsVisible = false;
        }

        public void RefreshFromCurrentSession()
        {
            if (rewardController == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[RewardPanelUI] RefreshFromCurrentSession: RewardController is missing.");

                HidePanel();
                return;
            }

            RewardSession session = rewardController.CurrentSession;
            if (session == null)
            {
                HidePanel();
                return;
            }

            ShowSession(session);
        }

        public void SetContinueInteractable(bool value)
        {
            if (continueButton == null)
                return;

            continueButton.interactable = value;
        }

        private void HandleChoiceRequested(int choiceIndex)
        {
            if (!IsAwaitingChoice)
                return;

            if (rewardController == null)
            {
                Debug.LogError("[RewardPanelUI] RewardController reference is missing.");
                return;
            }

            SetChoiceInteraction(false);

            bool accepted = rewardController.TryChooseCard(choiceIndex);

            if (accepted)
                return;

            RewardSession session = rewardController.CurrentSession;
            if (session != null && !session.CardChoiceResolved)
                SetChoiceInteraction(true);

            if (verboseLogs)
            {
                Debug.LogWarning(
                    $"[RewardPanelUI] TryChooseCard({choiceIndex}) was rejected.");
            }
        }

        private void HandleSkipClicked()
        {
            if (!IsAwaitingChoice)
                return;

            if (rewardController == null)
            {
                Debug.LogError("[RewardPanelUI] RewardController reference is missing.");
                return;
            }

            SetChoiceInteraction(false);

            bool accepted = rewardController.TrySkipCard();

            if (accepted)
                return;

            RewardSession session = rewardController.CurrentSession;
            if (session != null && !session.CardChoiceResolved)
                SetChoiceInteraction(true);

            if (verboseLogs)
                Debug.LogWarning("[RewardPanelUI] TrySkipCard was rejected.");
        }

        private void HandleRewardCardSelected(CardData selectedCard)
        {
            IsAwaitingChoice = false;
            SetSkipVisible(false);
            SetChoiceInteraction(false);

            MarkSelectedCardView(selectedCard);

            if (resultText != null && selectedCard != null)
            {
                resultText.text = $"Selected: {selectedCard.DisplayName}";
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RewardPanelUI] Card selected: " +
                    $"{(selectedCard != null ? selectedCard.CardId : "null")}");
            }
        }

        private void HandleRewardCardSkipped()
        {
            IsAwaitingChoice = false;
            SetSkipVisible(false);
            SetChoiceInteraction(false);
            ClearAllSelectedStates();

            if (resultText != null)
                resultText.text = "Skipped card reward.";

            if (verboseLogs)
                Debug.Log("[RewardPanelUI] Card reward skipped.");
        }

        private void HandleRewardSessionCompleted(RewardSession session)
        {
            IsAwaitingChoice = false;
            IsCompletedState = true;
            SetChoiceInteraction(false);
            SetSkipVisible(false);
            SetContinueVisible(true);

            if (session != null)
                UpdateResultTextForSession(session);

            if (verboseLogs)
                Debug.Log("[RewardPanelUI] Reward session completed. Continue is available.");
        }

        private void HandleContinueClicked()
        {
            if (rewardController == null)
            {
                Debug.LogError("[RewardPanelUI] RewardController reference is missing.");
                return;
            }

            RewardSession session = rewardController.CurrentSession;
            if (session == null || !session.IsComplete || !IsCompletedState)
                return;

            SetContinueVisible(false);
            OnContinueRequested?.Invoke();

            if (verboseLogs)
                Debug.Log("[RewardPanelUI] OnContinueRequested invoked.");
        }

        private void BuildChoiceViews(RewardSession session)
        {
            if (session.ChoiceCount <= 0)
                return;

            if (choiceContainer == null)
            {
                Debug.LogError("[RewardPanelUI] Choice Container reference is missing.");
                return;
            }

            if (choicePrefab == null)
            {
                Debug.LogError("[RewardPanelUI] Choice Prefab reference is missing.");
                return;
            }

            IReadOnlyList<CardData> choices = session.CardChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                CardData cardData = choices[i];
                if (cardData == null)
                    continue;

                RewardCardChoiceView view = Instantiate(choicePrefab, choiceContainer);
                view.Bind(cardData, i);
                view.OnChoiceRequested += HandleChoiceRequested;
                spawnedChoices.Add(view);
            }
        }

        private void ClearSpawnedChoices()
        {
            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view == null)
                    continue;

                view.OnChoiceRequested -= HandleChoiceRequested;
                Destroy(view.gameObject);
            }

            spawnedChoices.Clear();
        }

        private void SetChoiceInteraction(bool value)
        {
            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view != null)
                    view.SetInteractable(value);
            }

            UpdateSkipInteractable(value);
        }

        private void UpdateSkipInteractable(bool value)
        {
            if (skipButton == null)
                return;

            RewardSession session = rewardController != null
                ? rewardController.CurrentSession
                : null;

            bool canSkip = value &&
                           session != null &&
                           !session.CardChoiceResolved;

            skipButton.interactable = canSkip;
            SetSkipVisible(canSkip || (session != null && !session.CardChoiceResolved));
        }

        private void SetSkipVisible(bool visible)
        {
            if (skipButton != null)
                skipButton.gameObject.SetActive(visible);
        }

        private void SetContinueVisible(bool visible)
        {
            if (continueButton == null)
                return;

            continueButton.gameObject.SetActive(visible);
            continueButton.interactable = visible;
        }

        private void UpdateGoldDisplay(int goldAmount)
        {
            bool hasGold = goldAmount > 0;

            if (goldRoot != null)
                goldRoot.SetActive(hasGold);

            if (goldAmountText != null)
                goldAmountText.text = hasGold ? $"+{goldAmount} Gold" : string.Empty;
        }

        private void UpdateNoChoicesDisplay(bool showNoChoices)
        {
            if (noChoicesRoot != null)
                noChoicesRoot.SetActive(showNoChoices);
        }

        private void ApplyCompletedState(RewardSession session)
        {
            IsAwaitingChoice = false;
            IsCompletedState = true;
            SetChoiceInteraction(false);
            SetSkipVisible(false);
            SetContinueVisible(true);
            UpdateResultTextForSession(session);

            if (session.WasCardSkipped)
            {
                ClearAllSelectedStates();
                return;
            }

            if (session.SelectedCard != null)
                MarkSelectedCardView(session.SelectedCard);
        }

        private void MarkSelectedCardView(CardData selectedCard)
        {
            if (selectedCard == null)
                return;

            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view == null || view.CardData == null)
                    continue;

                bool isSelected = ReferenceEquals(view.CardData, selectedCard) ||
                                  string.Equals(
                                      view.CardData.CardId,
                                      selectedCard.CardId,
                                      StringComparison.Ordinal);

                view.SetSelected(isSelected);

                if (disableNonSelectedCardsAfterResolution && !isSelected)
                    view.SetInteractable(false);
            }
        }

        private void ClearAllSelectedStates()
        {
            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view != null)
                    view.SetSelected(false);
            }
        }

        private void UpdateResultTextForSession(RewardSession session)
        {
            if (resultText == null || session == null)
                return;

            if (!session.CardChoiceResolved)
            {
                resultText.text = string.Empty;
                return;
            }

            if (session.WasCardSkipped)
            {
                resultText.text = "Skipped card reward.";
                return;
            }

            if (session.SelectedCard != null)
                resultText.text = $"Selected: {session.SelectedCard.DisplayName}";
        }
    }
}
