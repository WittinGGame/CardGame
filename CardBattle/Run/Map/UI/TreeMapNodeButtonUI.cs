using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class TreeMapNodeButtonUI : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private Button button;

        [Header("Visual Layers")]
        [SerializeField] private Image bgImage;
        [SerializeField] private Image ringImage;
        [SerializeField] private Image glowImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject completedXRoot;
        [SerializeField] private GameObject currentMarkerRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual State Styles")]
        [SerializeField] private TreeMapNodeVisualStyle lockedStyle = TreeMapNodeVisualStyle.CreateLockedDefault();
        [SerializeField] private TreeMapNodeVisualStyle availableStyle = TreeMapNodeVisualStyle.CreateAvailableDefault();
        [SerializeField] private TreeMapNodeVisualStyle currentStyle = TreeMapNodeVisualStyle.CreateCurrentDefault();
        [SerializeField] private TreeMapNodeVisualStyle completedStyle = TreeMapNodeVisualStyle.CreateCompletedDefault();

        [Header("State Animation")]
        [SerializeField] private bool enableStateAnimation = true;
        [SerializeField] private float availablePulseScale = 1.06f;
        [SerializeField] private float availablePulseDuration = 1.4f;
        [SerializeField] private float availableGlowMinAlpha = 0.25f;
        [SerializeField] private float availableGlowMaxAlpha = 0.60f;
        [SerializeField] private float currentMarkerBobDistance = 4f;
        [SerializeField] private float currentMarkerBobDuration = 1.2f;
        [SerializeField] private float currentGlowMinAlpha = 0.35f;
        [SerializeField] private float currentGlowMaxAlpha = 0.70f;

        [Header("Options")]
        [SerializeField] private bool verboseLogs;

        public string NodeId { get; private set; } = string.Empty;
        public MapNodeData NodeData { get; private set; }
        public MapNodeState CurrentState { get; private set; } = MapNodeState.Locked;
        public TreeMapNodeVisualState CurrentVisualState { get; private set; } = TreeMapNodeVisualState.Locked;

        public event Action<string> OnNodeClicked;

        private TreeMapNodeVisualState lastLoggedVisualState = (TreeMapNodeVisualState)(-1);

        private Coroutine stateAnimationRoutine;
        private bool hasActiveStateAnimation;
        private TreeMapNodeVisualState animatingState = TreeMapNodeVisualState.Locked;

        private RectTransform ringRect;
        private RectTransform currentMarkerRect;
        private Vector3 baseRingScale = Vector3.one;
        private Vector2 baseMarkerAnchoredPosition = Vector2.zero;
        private bool baseTransformsCached;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleButtonClicked);
                button.onClick.AddListener(HandleButtonClicked);
            }

            CacheBaseTransforms();
        }

        private void OnEnable()
        {
            UpdateStateAnimation(CurrentVisualState);
        }

        private void OnDisable()
        {
            StopStateAnimation();
        }

        private void OnDestroy()
        {
            StopStateAnimation();
        }

        public void Bind(MapNodeData nodeData)
        {
            NodeData = nodeData;
            NodeId = nodeData != null ? nodeData.NodeId : string.Empty;
            lastLoggedVisualState = (TreeMapNodeVisualState)(-1);
        }

        public void RefreshState(
            MapNodeState state,
            bool canStartBattle,
            TreeMapNodeVisualState visualState)
        {
            CurrentState = state;
            SetVisualState(visualState);
            SetInteractable(canStartBattle);
        }

        public void SetVisualState(TreeMapNodeVisualState visualState)
        {
            CurrentVisualState = visualState;
            TreeMapNodeVisualStyle style = GetStyleForVisualState(visualState);
            ApplyVisualStyle(style);

            if (verboseLogs && lastLoggedVisualState != visualState)
            {
                Debug.Log(
                    $"[TreeMapNodeUI] Node {NodeId} visual state = {visualState}");
                lastLoggedVisualState = visualState;
            }

            UpdateStateAnimation(visualState);
        }

        public void SetNodeIcon(Sprite icon)
        {
            if (iconImage == null)
            {
                LogMissingOptionalReference(nameof(iconImage));
                return;
            }

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        public void Clear()
        {
            NodeId = string.Empty;
            NodeData = null;
            CurrentState = MapNodeState.Locked;
            lastLoggedVisualState = (TreeMapNodeVisualState)(-1);

            SetVisualState(TreeMapNodeVisualState.Locked);
            SetInteractable(false);
        }

        public static TreeMapNodeVisualState MapNodeStateToVisualState(MapNodeState state)
        {
            return state switch
            {
                MapNodeState.Locked => TreeMapNodeVisualState.Locked,
                MapNodeState.Available => TreeMapNodeVisualState.Available,
                MapNodeState.Current => TreeMapNodeVisualState.Current,
                MapNodeState.Completed => TreeMapNodeVisualState.Completed,
                _ => TreeMapNodeVisualState.Locked
            };
        }

        private TreeMapNodeVisualStyle GetStyleForVisualState(TreeMapNodeVisualState visualState)
        {
            return visualState switch
            {
                TreeMapNodeVisualState.Available => availableStyle,
                TreeMapNodeVisualState.Current => currentStyle,
                TreeMapNodeVisualState.Completed => completedStyle,
                _ => lockedStyle
            };
        }

        private void ApplyVisualStyle(TreeMapNodeVisualStyle style)
        {
            if (style == null)
                return;

            SetImageAlpha(bgImage, style.bgAlpha);
            SetImageAlpha(ringImage, style.ringAlpha);
            SetImageAlpha(iconImage, style.iconAlpha);

            SetLayerActive(glowImage, style.glowEnabled);

            if (completedXRoot != null)
                completedXRoot.SetActive(style.completedXEnabled);

            if (currentMarkerRoot != null)
                currentMarkerRoot.SetActive(style.currentMarkerEnabled);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static void SetLayerActive(Image image, bool enabled)
        {
            if (image == null)
                return;

            image.enabled = enabled;
            if (image.gameObject.activeSelf != enabled)
                image.gameObject.SetActive(enabled);
        }

        private void CacheBaseTransforms()
        {
            if (baseTransformsCached)
                return;

            if (ringImage != null)
                ringRect = ringImage.rectTransform;

            if (currentMarkerRoot != null)
                currentMarkerRect = currentMarkerRoot.transform as RectTransform;

            baseRingScale = ringRect != null ? ringRect.localScale : Vector3.one;
            baseMarkerAnchoredPosition =
                currentMarkerRect != null ? currentMarkerRect.anchoredPosition : Vector2.zero;

            baseTransformsCached = true;
        }

        private void UpdateStateAnimation(TreeMapNodeVisualState visualState)
        {
            if (!isActiveAndEnabled)
            {
                StopStateAnimation();
                return;
            }

            bool wantsAnimation = enableStateAnimation &&
                                  (visualState == TreeMapNodeVisualState.Available ||
                                   visualState == TreeMapNodeVisualState.Current);

            if (!wantsAnimation)
            {
                StopStateAnimation();
                return;
            }

            if (hasActiveStateAnimation && animatingState == visualState)
                return;

            StopStateAnimation();

            CacheBaseTransforms();
            animatingState = visualState;
            hasActiveStateAnimation = true;

            if (visualState == TreeMapNodeVisualState.Available)
            {
                stateAnimationRoutine = StartCoroutine(AvailablePulseRoutine());

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapNodeUI] Node {NodeId} started Available pulse.");
                }
            }
            else
            {
                stateAnimationRoutine = StartCoroutine(CurrentAnimationRoutine());

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapNodeUI] Node {NodeId} started Current animation.");
                }
            }
        }

        private void StopStateAnimation()
        {
            bool wasAnimating = hasActiveStateAnimation;

            if (stateAnimationRoutine != null)
            {
                if (isActiveAndEnabled)
                    StopCoroutine(stateAnimationRoutine);

                stateAnimationRoutine = null;
            }

            hasActiveStateAnimation = false;
            ResetAnimatedTransforms();

            if (wasAnimating && verboseLogs)
            {
                Debug.Log(
                    $"[TreeMapNodeUI] Node {NodeId} stopped state animation.");
            }
        }

        private void ResetAnimatedTransforms()
        {
            if (!baseTransformsCached)
                return;

            if (ringRect != null)
                ringRect.localScale = baseRingScale;

            if (currentMarkerRect != null)
                currentMarkerRect.anchoredPosition = baseMarkerAnchoredPosition;
        }

        private IEnumerator AvailablePulseRoutine()
        {
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float sin01 = ComputeSin01(elapsed, availablePulseDuration);

                if (ringRect != null)
                {
                    float scale = Mathf.Lerp(1f, availablePulseScale, sin01);
                    ringRect.localScale = baseRingScale * scale;
                }

                if (glowImage != null)
                {
                    float glowAlpha = Mathf.Lerp(availableGlowMinAlpha, availableGlowMaxAlpha, sin01);
                    SetImageAlpha(glowImage, glowAlpha);
                }

                yield return null;
            }
        }

        private IEnumerator CurrentAnimationRoutine()
        {
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float sinSigned = ComputeSinSigned(elapsed, currentMarkerBobDuration);
                float sin01 = ComputeSin01(elapsed, currentMarkerBobDuration);

                if (currentMarkerRect != null)
                {
                    Vector2 offset = new Vector2(0f, sinSigned * currentMarkerBobDistance);
                    currentMarkerRect.anchoredPosition = baseMarkerAnchoredPosition + offset;
                }

                if (glowImage != null)
                {
                    float glowAlpha = Mathf.Lerp(currentGlowMinAlpha, currentGlowMaxAlpha, sin01);
                    SetImageAlpha(glowImage, glowAlpha);
                }

                yield return null;
            }
        }

        private static float ComputeSin01(float elapsed, float duration)
        {
            float phase = duration > 0f ? elapsed / duration : 0f;
            return (Mathf.Sin(phase * Mathf.PI * 2f) + 1f) * 0.5f;
        }

        private static float ComputeSinSigned(float elapsed, float duration)
        {
            float phase = duration > 0f ? elapsed / duration : 0f;
            return Mathf.Sin(phase * Mathf.PI * 2f);
        }

        private void LogMissingOptionalReference(string fieldName)
        {
            if (!verboseLogs)
                return;

            Debug.Log(
                $"[TreeMapNodeUI] Missing optional visual reference: {fieldName} | nodeId={NodeId}");
        }

        private void HandleButtonClicked()
        {
            if (string.IsNullOrWhiteSpace(NodeId))
                return;

            OnNodeClicked?.Invoke(NodeId);
        }
    }
}
