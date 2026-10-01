using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class UpgradeCardChoiceView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI availabilityText;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image typeBadgeImage;
        [SerializeField] private CardTypeBadgeSet typeBadgeSet;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField, Range(1f, 1.15f)] private float hoverScale = 1.08f;
        [SerializeField, Min(0f)] private float hoverInDuration = .12f;
        [SerializeField, Min(0f)] private float hoverOutDuration = .12f;
        [SerializeField] private bool compactCost;
        [SerializeField] private bool fitVisualToCard;
        private bool pointerOver;
        private Vector3 originalScale;
        private bool scaleCaptured;
        private Coroutine hoverRoutine;
        public string OwnedCardId { get; private set; }
        public bool IsSelectable { get; private set; }
        public event Action<string> OnSelected;

        private void Awake()
        {
            CaptureScale();
            if (button != null) { button.onClick.RemoveListener(HandleClick); button.onClick.AddListener(HandleClick); }
        }
        private void CaptureScale()
        {
            if (scaleCaptured || visualRoot == null) return;
            originalScale = visualRoot.localScale; scaleCaptured = true;
        }
        private void ResetHover()
        {
            pointerOver = false;
            CancelHoverAnimation();
            if (scaleCaptured && visualRoot != null) visualRoot.localScale = originalScale;
        }
        private void OnRectTransformDimensionsChange()
        {
            if (!fitVisualToCard || visualRoot == null || !(transform is RectTransform rect)) return;
            var size = visualRoot.rect.size;
            if (size.x <= 0 || size.y <= 0 || rect.rect.width <= 0 || rect.rect.height <= 0) return;
            originalScale = Vector3.one * Mathf.Min(rect.rect.width / size.x, rect.rect.height / size.y);
            scaleCaptured = true;
            CancelHoverAnimation();
            visualRoot.localScale = originalScale * (pointerOver && IsSelectable ? hoverScale : 1f);
        }
        private void OnEnable() { OnRectTransformDimensionsChange(); }
        private void OnDisable() { ResetHover(); }
        public void OnPointerEnter(PointerEventData eventData)
        {
            CaptureScale();
            pointerOver = isActiveAndEnabled && IsSelectable && (button == null || button.interactable);
            if (pointerOver) AnimateScale(originalScale * hoverScale, hoverInDuration);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOver = false;
            if (scaleCaptured && visualRoot != null) AnimateScale(originalScale, hoverOutDuration);
        }

        private void AnimateScale(Vector3 targetScale, float duration)
        {
            if (visualRoot == null) return;
            CancelHoverAnimation();
            if (!Application.isPlaying || duration <= 0f)
            {
                visualRoot.localScale = targetScale;
                return;
            }
            hoverRoutine = StartCoroutine(AnimateScaleRoutine(visualRoot.localScale, targetScale, duration));
        }

        private IEnumerator AnimateScaleRoutine(Vector3 startScale, Vector3 targetScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                if (visualRoot != null) visualRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                yield return null;
            }
            if (visualRoot != null) visualRoot.localScale = targetScale;
            hoverRoutine = null;
        }

        private void CancelHoverAnimation()
        {
            if (hoverRoutine != null) StopCoroutine(hoverRoutine);
            hoverRoutine = null;
        }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(HandleClick); }

#if UNITY_EDITOR
        private void OnValidate()
        {
            hoverScale = Mathf.Max(1f, hoverScale);
            hoverInDuration = Mathf.Max(0f, hoverInDuration);
            hoverOutDuration = Mathf.Max(0f, hoverOutDuration);
        }
#endif
        private void HandleClick() { if (isActiveAndEnabled && IsSelectable) OnSelected?.Invoke(OwnedCardId); }

        // Read-only card display: full opacity, no selection/hover or availability message.
        public void BindPreview(CardData data, string description, int apCost)
        {
            Bind(null, data, false, false, true, description, apCost);
            if (availabilityText != null) availabilityText.text = "";
        }

        public void BindPresentationChoice(string choiceId, CardData data, string description,
            int apCost, string choiceName, bool interactable)
        {
            ResetHover();
            OwnedCardId = choiceId;
            IsSelectable = interactable && !string.IsNullOrWhiteSpace(choiceId);
            if (titleText != null) titleText.text = data != null ? data.DisplayName : "Invalid card";
            if (costText != null) costText.text = apCost + (compactCost ? "" : " AP");
            if (descriptionText != null) descriptionText.text = description ?? string.Empty;
            if (availabilityText != null) availabilityText.text = choiceName ?? string.Empty;
            if (artworkImage != null) { artworkImage.sprite = data != null ? data.Artwork : null; artworkImage.enabled = artworkImage.sprite != null; }
            if (typeBadgeImage != null)
            {
                typeBadgeImage.sprite = data != null && typeBadgeSet != null ? typeBadgeSet.GetBadge(data.CardType) : null;
                typeBadgeImage.enabled = typeBadgeImage.sprite != null;
            }
            if (button != null) button.interactable = IsSelectable;
            if (selectedRoot != null) selectedRoot.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        public void SetPresentationInteractable(bool interactable)
        {
            IsSelectable = interactable && !string.IsNullOrWhiteSpace(OwnedCardId);
            if (button != null) button.interactable = IsSelectable;
            if (!IsSelectable) ResetHover();
        }

        public void Bind(RunCardRecord record, CardData data, bool eligible, bool selected, bool locked = false, string resolvedDescription = null, int? effectiveApCost = null)
        {
            if (OwnedCardId != record?.runCardInstanceId || !eligible) ResetHover();
            OwnedCardId = record?.runCardInstanceId;
            IsSelectable = eligible && !string.IsNullOrWhiteSpace(OwnedCardId);
            if (titleText != null) titleText.text = data != null ? data.DisplayName : record?.cardId ?? "Invalid card";
            if (costText != null) costText.text = data != null ? (effectiveApCost ?? data.ApCost).ToString() + (compactCost ? "" : " AP") : "";
            if (descriptionText != null) descriptionText.text = resolvedDescription ?? (data != null && (record?.upgradeLevel ?? 0) == 0
                ? CardDescriptionBuilder.Build(data) : "");
            if (availabilityText != null) availabilityText.text = locked ? "Locked for Upgrade" : IsSelectable ? "" :
                record != null && record.upgradeLevel > 0 ? "Already upgraded" : "Upgrade unavailable";
            if (artworkImage != null) { artworkImage.sprite = data != null ? data.Artwork : null; artworkImage.enabled = artworkImage.sprite != null; }
            if (typeBadgeImage != null)
            {
                typeBadgeImage.sprite = data != null && typeBadgeSet != null ? typeBadgeSet.GetBadge(data.CardType) : null;
                typeBadgeImage.enabled = typeBadgeImage.sprite != null;
            }
            if (button != null) button.interactable = IsSelectable;
            if (selectedRoot != null) selectedRoot.SetActive(selected);
            if (canvasGroup != null) canvasGroup.alpha = eligible || locked ? 1f : 0.45f;
        }
    }
}
