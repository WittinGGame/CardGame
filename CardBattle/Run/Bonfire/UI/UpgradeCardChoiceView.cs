using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class UpgradeCardChoiceView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI availabilityText;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        public string OwnedCardId { get; private set; }
        public bool IsSelectable { get; private set; }
        public event Action<string> OnSelected;

        private void Awake() { if (button != null) button.onClick.AddListener(HandleClick); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(HandleClick); }
        private void HandleClick() { if (IsSelectable) OnSelected?.Invoke(OwnedCardId); }

        public void Bind(RunCardRecord record, CardData data, bool eligible, bool selected, bool locked = false, string resolvedDescription = null)
        {
            OwnedCardId = record?.runCardInstanceId;
            IsSelectable = eligible && !string.IsNullOrWhiteSpace(OwnedCardId);
            if (titleText != null) titleText.text = data != null ? data.DisplayName : record?.cardId ?? "Invalid card";
            if (costText != null) costText.text = data != null ? $"{data.ApCost} AP" : "";
            if (descriptionText != null) descriptionText.text = resolvedDescription ?? (data != null && (record?.upgradeLevel ?? 0) == 0
                ? CardDescriptionBuilder.Build(data) : "");
            if (availabilityText != null) availabilityText.text = locked ? "Locked for Upgrade" : IsSelectable ? "" :
                record != null && record.upgradeLevel > 0 ? "Already upgraded" : "Upgrade unavailable";
            if (button != null) button.interactable = IsSelectable;
            if (selectedRoot != null) selectedRoot.SetActive(selected);
            if (canvasGroup != null) canvasGroup.alpha = eligible || locked ? 1f : 0.45f;
        }
    }
}
