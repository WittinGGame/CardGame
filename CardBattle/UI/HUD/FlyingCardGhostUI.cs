using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only ghost card that flies toward a graveyard anchor and self-destructs on arrival.
    /// Supports a curved path for smoother motion.
    /// </summary>
    public class FlyingCardGhostUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image artworkImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Motion")]
        [SerializeField] private float flyDuration = 0.42f;
        [SerializeField] private AnimationCurve motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float endScaleMultiplier = 0.72f;
        [SerializeField] private float endAlphaMultiplier = 0.78f;

        [Header("Curved Path")]
        [SerializeField] private bool useCurvedPath = true;
        [SerializeField] private bool useDynamicArcHeight = true;
        [SerializeField] private float arcHeight = 140f;
        [SerializeField] private float arcDistanceMultiplier = 0.18f;
        [SerializeField] private float minArcHeight = 60f;
        [SerializeField] private float maxArcHeight = 180f;
        [SerializeField] private float horizontalControlOffset = 0f;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (artworkImage == null)
                artworkImage = GetComponentInChildren<Image>(true);
        }

        public void BeginFlight(
            Sprite artwork,
            Vector2 startAnchoredPosition,
            Vector2 endAnchoredPosition,
            System.Action onArrived)
        {
            if (artworkImage != null)
            {
                artworkImage.sprite = artwork;
                artworkImage.enabled = artwork != null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
                rectTransform.localScale = Vector3.one;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            StartCoroutine(CoFly(startAnchoredPosition, endAnchoredPosition, onArrived));
        }

        private IEnumerator CoFly(
            Vector2 start,
            Vector2 end,
            System.Action onArrived)
        {
            float dur = Mathf.Max(0.01f, flyDuration);
            float t = 0f;

            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            float endAlpha = startAlpha * endAlphaMultiplier;

            float resolvedArcHeight = arcHeight;
            if (useDynamicArcHeight)
            {
                float distance = Vector2.Distance(start, end);
                resolvedArcHeight = Mathf.Clamp(distance * arcDistanceMultiplier, minArcHeight, maxArcHeight);
            }

            Vector2 control = (start + end) * 0.5f;
            control += Vector2.up * resolvedArcHeight;
            control += Vector2.right * horizontalControlOffset;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = motionCurve.Evaluate(Mathf.Clamp01(t / dur));

                if (rectTransform != null)
                {
                    Vector2 pos = useCurvedPath
                        ? EvaluateQuadraticBezier(start, control, end, u)
                        : Vector2.LerpUnclamped(start, end, u);

                    rectTransform.anchoredPosition = pos;

                    float s = Mathf.Lerp(1f, endScaleMultiplier, u);
                    rectTransform.localScale = new Vector3(s, s, 1f);
                }

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, u);

                yield return null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = end;
                rectTransform.localScale = new Vector3(endScaleMultiplier, endScaleMultiplier, 1f);
            }

            if (canvasGroup != null)
                canvasGroup.alpha = endAlpha;

            onArrived?.Invoke();
            Destroy(gameObject);
        }

        private static Vector2 EvaluateQuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float omt = 1f - t;
            return omt * omt * a + 2f * omt * t * b + t * t * c;
        }
    }
}