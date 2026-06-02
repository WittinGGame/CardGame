using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class CardViewUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum CardVisualState
        {
            Normal,
            Hovered,
            Selected,
            Disabled
        }

        [Header("UI References")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image typeBadgeImage;
        [SerializeField] private CardTypeBadgeSet typeBadgeSet;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Core References")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private RectTransform guideStartAnchor;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button button;

        [Header("Visual Tuning")]
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float hoveredScale = 1.08f;
        [SerializeField] private float selectedScale = 1.14f;
        [SerializeField] private float disabledScale = 0.96f;

        [SerializeField] private float normalAlpha = 1f;
        [SerializeField] private float disabledAlpha = 0.5f;

        [SerializeField] private float hoveredYOffset = 25f;
        [SerializeField] private float selectedYOffset = 40f;

        [SerializeField] private float scaleLerpSpeed = 12f;
        [SerializeField] private float moveLerpSpeed = 12f;
        [SerializeField] private float layoutLerpSpeed = 12f;
        [SerializeField] private float rotationLerpSpeed = 14f;

        [Header("Deal-In Presentation")]
        [SerializeField] private bool useDealFadeIn = true;
        [SerializeField] private float dealSpawnAlpha = 0f;
        [SerializeField] private float dealFadeDuration = 0.12f;

        private CardInstance boundCard;
        private CardVisualState currentState = CardVisualState.Normal;

        private RectTransform _rectTransform;
        private Vector2 targetLayoutAnchoredPos;
        private float _layoutRotationZ;
        private float targetRotationZ;

        private Vector3 baseScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;

        private Vector3 baseLocalPosition = Vector3.zero;
        private Vector3 targetLocalPosition = Vector3.zero;

        private bool isInteractable = true;
        private bool isSelected = false;
        private bool isPointerOver;
        private bool layoutMovementBlocked;
        private bool pendingDealFadeIn;
        private Coroutine dealFadeRoutine;

        public CardInstance BoundCard => boundCard;
        /// <summary>Root layout rect (anchored fan position). Used by presentation VFX.</summary>
        public RectTransform LayoutRect => _rectTransform;
        public RectTransform GuideStartAnchor => guideStartAnchor;
        public bool IsSelected => isSelected;
        public bool IsInteractable => isInteractable;
        public bool IsPointerOver => isPointerOver;
        public bool IsDealPresentationPending => layoutMovementBlocked || pendingDealFadeIn || dealFadeRoutine != null;

        public event System.Action<CardViewUI> OnHoverStarted;
        public event System.Action<CardViewUI> OnHoverEnded;

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform.Find("VisualRoot") as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (button == null)
                button = GetComponent<Button>();

            if (visualRoot != null)
            {
                baseScale = visualRoot.localScale;
                targetScale = baseScale * normalScale;

                baseLocalPosition = visualRoot.localPosition;
                targetLocalPosition = baseLocalPosition;

                targetRotationZ = visualRoot.localEulerAngles.z;
                _layoutRotationZ = targetRotationZ;
            }

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
                targetLayoutAnchoredPos = _rectTransform.anchoredPosition;
        }

        private void OnDisable()
        {
            StopDealFadeRoutine();
        }

        /// <summary>Tuning from <see cref="HandUIController"/> so hand and card share one layout motion speed.</summary>
        public void SetLayoutLerpSpeed(float speed)
        {
            layoutLerpSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>Targets root layout position and idle fan rotation; motion is smoothed in <see cref="Update"/>.</summary>
        public void SetLayoutPose(Vector2 anchoredPos, float rotationZ)
        {
            targetLayoutAnchoredPos = anchoredPos;
            _layoutRotationZ = rotationZ;
            SyncRotationTargetToState();
        }

        /// <summary>
        /// Sets an immediate spawn pose before the normal layout lerp pulls this card toward its target slot.
        /// </summary>
        public void PrepareForDealIn(Vector2 startAnchoredPos, float startRotationZ, float startScale)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
                _rectTransform.anchoredPosition = startAnchoredPos;

            if (visualRoot != null)
            {
                float clampedScale = Mathf.Max(0.01f, startScale);
                visualRoot.localScale = baseScale * clampedScale;
                visualRoot.localEulerAngles = new Vector3(0f, 0f, startRotationZ);
            }

            StopDealFadeRoutine();
            pendingDealFadeIn = useDealFadeIn && canvasGroup != null;
            if (pendingDealFadeIn && canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(dealSpawnAlpha);
        }

        public void SetLayoutMovementBlocked(bool value)
        {
            bool wasBlocked = layoutMovementBlocked;
            layoutMovementBlocked = value;

            if (layoutMovementBlocked)
            {
                StopDealFadeRoutine();
                return;
            }

            if (wasBlocked && pendingDealFadeIn)
                StartDealFadeIn();
        }

        public void ForceCompleteDealPresentation()
        {
            layoutMovementBlocked = false;
            pendingDealFadeIn = false;

            StopDealFadeRoutine();

            if (canvasGroup != null)
                canvasGroup.alpha = ResolveTargetAlphaForCurrentState();

            if (visualRoot != null)
            {
                visualRoot.localScale = targetScale;
                visualRoot.localPosition = targetLocalPosition;
                visualRoot.localEulerAngles = new Vector3(0f, 0f, targetRotationZ);
            }
        }

        private void SyncRotationTargetToState()
        {
            if (currentState == CardVisualState.Hovered || currentState == CardVisualState.Selected)
                targetRotationZ = 0f;
            else
                targetRotationZ = _layoutRotationZ;
        }

        private void Update()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
            {
                if (!layoutMovementBlocked)
                {
                    _rectTransform.anchoredPosition = Vector2.Lerp(
                        _rectTransform.anchoredPosition,
                        targetLayoutAnchoredPos,
                        Time.deltaTime * layoutLerpSpeed
                    );
                }
            }

            if (visualRoot == null)
                return;

            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                targetScale,
                Time.deltaTime * scaleLerpSpeed
            );

            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                targetLocalPosition,
                Time.deltaTime * moveLerpSpeed
            );

            float currentZ = visualRoot.localEulerAngles.z;
            float newZ = Mathf.LerpAngle(currentZ, targetRotationZ, Time.deltaTime * rotationLerpSpeed);
            visualRoot.localEulerAngles = new Vector3(0f, 0f, newZ);
        }

        public void Bind(CardInstance card)
        {
            boundCard = card;

            if (card?.Data == null)
                return;

            var data = card.Data;

            if (costText != null)
                costText.text = data.ApCost.ToString();

            if (nameText != null)
                nameText.text = data.DisplayName;

            if (typeBadgeImage != null)
            {
                Sprite badge = typeBadgeSet != null ? typeBadgeSet.GetBadge(data.CardType) : null;

                typeBadgeImage.sprite = badge;
                typeBadgeImage.enabled = badge != null;
            }

            if (artworkImage != null)
                artworkImage.sprite = data.Artwork;

            if (descriptionText != null)
                descriptionText.text = CardDescriptionBuilder.Build(data);

            ApplyStateVisuals();
        }

        /// <summary>Artwork sprite for flying ghost VFX (presentation only).</summary>
        public Sprite GetArtworkSnapshotForVfx()
        {
            return artworkImage != null ? artworkImage.sprite : null;
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;

            if (button != null)
                button.interactable = value;

            if (!isInteractable)
            {
                isSelected = false;
                currentState = CardVisualState.Disabled;
            }
            else
            {
                currentState = isSelected
                    ? CardVisualState.Selected
                    : (isPointerOver ? CardVisualState.Hovered : CardVisualState.Normal);
            }

            ApplyStateVisuals();
        }

        public void SetClickAction(UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();

            if (action != null)
            {
                button.onClick.AddListener(() =>
                {
                    Select();
                    action.Invoke();
                });
            }
        }

        public void Select()
        {
            if (!isInteractable)
                return;

            isSelected = true;
            currentState = CardVisualState.Selected;
            ApplyStateVisuals();
        }

        public void Deselect()
        {
            isSelected = false;

            if (!isInteractable)
                currentState = CardVisualState.Disabled;
            else if (isPointerOver)
            {
                currentState = CardVisualState.Hovered;
                OnHoverStarted?.Invoke(this);
            }
            else
                currentState = CardVisualState.Normal;

            ApplyStateVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;

            if (!isInteractable || isSelected)
                return;

            OnHoverStarted?.Invoke(this);

            currentState = CardVisualState.Hovered;
            ApplyStateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;

            if (!isInteractable)
                return;

            if (isSelected)
                return;

            OnHoverEnded?.Invoke(this);

            currentState = CardVisualState.Normal;
            ApplyStateVisuals();
        }

        private void ApplyStateVisuals()
        {
            if (visualRoot == null)
                return;

            SyncRotationTargetToState();

            switch (currentState)
            {
                case CardVisualState.Normal:
                    targetScale = baseScale * normalScale;
                    targetLocalPosition = baseLocalPosition;
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Hovered:
                    targetScale = baseScale * hoveredScale;
                    targetLocalPosition = baseLocalPosition + new Vector3(0f, hoveredYOffset, 0f);
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Selected:
                    targetScale = baseScale * selectedScale;
                    targetLocalPosition = baseLocalPosition + new Vector3(0f, selectedYOffset, 0f);
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Disabled:
                    targetScale = baseScale * disabledScale;
                    targetLocalPosition = baseLocalPosition;
                    SetAlpha(disabledAlpha);
                    break;
            }
        }

        private void SetAlpha(float value)
        {
            if (canvasGroup != null)
            {
                if (layoutMovementBlocked && pendingDealFadeIn)
                    canvasGroup.alpha = Mathf.Min(Mathf.Clamp01(value), Mathf.Clamp01(dealSpawnAlpha));
                else
                    canvasGroup.alpha = value;
            }
        }

        private void StartDealFadeIn()
        {
            if (canvasGroup == null)
            {
                pendingDealFadeIn = false;
                return;
            }

            StopDealFadeRoutine();
            dealFadeRoutine = StartCoroutine(CoDealFadeIn(ResolveDealFadeTargetAlpha()));
        }

        private IEnumerator CoDealFadeIn(float targetAlpha)
        {
            float duration = Mathf.Max(0.01f, dealFadeDuration);
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            float endAlpha = Mathf.Clamp01(targetAlpha);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            pendingDealFadeIn = false;
            dealFadeRoutine = null;
        }

        private float ResolveTargetAlphaForCurrentState()
        {
            return currentState == CardVisualState.Disabled ? disabledAlpha : normalAlpha;
        }

        private float ResolveDealFadeTargetAlpha()
        {
            return normalAlpha;
        }

        private void StopDealFadeRoutine()
        {
            if (dealFadeRoutine != null)
            {
                StopCoroutine(dealFadeRoutine);
                dealFadeRoutine = null;
            }
        }

    }
}