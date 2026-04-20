using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only ghost card that lerps toward a graveyard anchor and self-destructs on arrival.
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

            StartCoroutine(
                CoFly(startAnchoredPosition, endAnchoredPosition, onArrived));
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

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = motionCurve.Evaluate(Mathf.Clamp01(t / dur));

                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, u);
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
    }
}
