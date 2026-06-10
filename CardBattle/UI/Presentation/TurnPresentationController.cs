using System.Collections;
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only turn intro banner. Shows BATTLE START or TURN N before the player can act.
    /// </summary>
    public class TurnPresentationController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float holdDuration = 0.35f;
        [SerializeField] private float fadeOutDuration = 0.20f;

        [Header("Scale")]
        [SerializeField] private float startScale = 0.92f;
        [SerializeField] private float endScale = 1f;

        private Vector3 baseTextScale = Vector3.one;

        private void Awake()
        {
            if (turnText != null)
                baseTextScale = turnText.rectTransform.localScale;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        public IEnumerator PlayTurnIntro(int turnNumber)
        {
            if (turnText == null || canvasGroup == null)
                yield break;

            turnText.text = turnNumber <= 1 ? "BATTLE START" : $"TURN {turnNumber}";

            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            var scaleTarget = turnText.rectTransform;
            scaleTarget.localScale = baseTextScale * startScale;

            float fadeIn = Mathf.Max(0.01f, fadeInDuration);
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeIn);
                canvasGroup.alpha = t;
                scaleTarget.localScale = baseTextScale * Mathf.Lerp(startScale, endScale, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            scaleTarget.localScale = baseTextScale * endScale;

            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float fadeOut = Mathf.Max(0.01f, fadeOutDuration);
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOut);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
