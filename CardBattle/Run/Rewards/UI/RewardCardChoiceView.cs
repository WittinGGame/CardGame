using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class RewardCardChoiceView : MonoBehaviour
    {
        [Header("Card Display")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image cardTypeIcon;
        [SerializeField] private Button chooseButton;

        [Header("State")]
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Interaction")]
        [SerializeField] private float disabledAlpha = 0.45f;
        [SerializeField] private float enabledAlpha = 1f;

        public CardData CardData { get; private set; }
        public int ChoiceIndex { get; private set; } = -1;
        public bool IsInteractable { get; private set; }

        public event Action<int> OnChoiceRequested;

        private void Awake()
        {
            if (chooseButton != null)
            {
                chooseButton.onClick.RemoveListener(HandleChooseClicked);
                chooseButton.onClick.AddListener(HandleChooseClicked);
            }
        }

        private void OnDestroy()
        {
            if (chooseButton != null)
                chooseButton.onClick.RemoveListener(HandleChooseClicked);
        }

        public void Bind(CardData cardData, int choiceIndex)
        {
            CardData = cardData;
            ChoiceIndex = choiceIndex;

            if (cardData == null)
            {
                ClearView();
                return;
            }

            if (nameText != null)
                nameText.text = cardData.DisplayName;

            if (costText != null)
                costText.text = cardData.ApCost.ToString();

            if (descriptionText != null)
                descriptionText.text = CardDescriptionBuilder.Build(cardData);

            if (artworkImage != null)
            {
                Sprite artwork = cardData.Artwork;
                artworkImage.sprite = artwork;
                artworkImage.enabled = artwork != null;
            }

            if (cardTypeIcon != null)
                cardTypeIcon.enabled = cardTypeIcon.sprite != null;

            SetSelected(false);
            SetInteractable(true);
        }

        public void SetInteractable(bool value)
        {
            IsInteractable = value;

            if (chooseButton != null)
                chooseButton.interactable = value;

            if (canvasGroup != null)
                canvasGroup.alpha = value ? enabledAlpha : disabledAlpha;
        }

        public void SetSelected(bool value)
        {
            if (selectedRoot != null)
                selectedRoot.SetActive(value);
        }

        public void ClearView()
        {
            CardData = null;
            ChoiceIndex = -1;

            if (nameText != null)
                nameText.text = string.Empty;

            if (costText != null)
                costText.text = string.Empty;

            if (descriptionText != null)
                descriptionText.text = string.Empty;

            if (artworkImage != null)
            {
                artworkImage.sprite = null;
                artworkImage.enabled = false;
            }

            if (cardTypeIcon != null)
                cardTypeIcon.enabled = false;

            SetSelected(false);
            SetInteractable(false);
        }

        private void HandleChooseClicked()
        {
            if (!IsInteractable || CardData == null || ChoiceIndex < 0)
                return;

            OnChoiceRequested?.Invoke(ChoiceIndex);
        }
    }
}
