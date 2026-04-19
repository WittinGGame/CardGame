using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Simple floating TMP line: rises in canvas space, fades out, then self-destroys.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FloatingTextUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float lifetime = 1.2f;
        [SerializeField] private float riseSpeedPixelsPerSecond = 72f;

        private RectTransform _rect;
        private float _elapsed;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Configure display and reset motion state. Call right after instantiate.</summary>
        public void Play(string text, Color color)
        {
            if (label != null)
            {
                label.text = text;
                label.color = color;
            }

            _elapsed = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (_rect != null)
                _rect.anchoredPosition += Vector2.up * (riseSpeedPixelsPerSecond * Time.deltaTime);

            _elapsed += Time.deltaTime;
            float t = lifetime > 0f ? Mathf.Clamp01(_elapsed / lifetime) : 1f;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            if (_elapsed >= lifetime)
                Destroy(gameObject);
        }
    }
}
