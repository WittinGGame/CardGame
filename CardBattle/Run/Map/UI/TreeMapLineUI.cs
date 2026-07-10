using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class TreeMapLineUI : MonoBehaviour
    {
        [Header("Line")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image lineImage;

        [Header("Legacy")]
        [SerializeField] private Image image;

        [Header("Visual Colors")]
        [SerializeField] private Color lockedLineColor = new Color(0.45f, 0.45f, 0.50f, 1f);
        [SerializeField] private Color availableLineColor = new Color(0.75f, 0.68f, 0.42f, 1f);
        [SerializeField] private Color currentLineColor = new Color(0.40f, 0.72f, 0.95f, 1f);
        [SerializeField] private Color completedLineColor = new Color(0.50f, 0.62f, 0.52f, 1f);

        [Header("Visual Alpha")]
        [SerializeField] private float lockedAlpha = 0.25f;
        [SerializeField] private float availableAlpha = 0.60f;
        [SerializeField] private float currentAlpha = 0.85f;
        [SerializeField] private float completedAlpha = 0.55f;

        public string FromNodeId { get; private set; } = string.Empty;
        public string ToNodeId { get; private set; } = string.Empty;

        public void Bind(
            Vector2 start,
            Vector2 end,
            float thickness,
            string fromNodeId,
            string toNodeId)
        {
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;

            if (rectTransform == null)
                rectTransform = transform as RectTransform;

            Vector2 delta = end - start;
            float distance = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            rectTransform.anchoredPosition = (start + end) * 0.5f;
            rectTransform.sizeDelta = new Vector2(distance, thickness);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetVisualState(TreeMapLineVisualState state)
        {
            Image resolvedImage = ResolveLineImage();
            if (resolvedImage == null)
                return;

            Color baseColor = GetColorForState(state);
            float alpha = GetAlphaForState(state);
            baseColor.a = alpha;
            resolvedImage.color = baseColor;
        }

        public void SetColor(Color color)
        {
            Image resolvedImage = ResolveLineImage();
            if (resolvedImage != null)
                resolvedImage.color = color;
        }

        private Image ResolveLineImage()
        {
            if (lineImage != null)
                return lineImage;

            return image;
        }

        private Color GetColorForState(TreeMapLineVisualState state)
        {
            return state switch
            {
                TreeMapLineVisualState.Available => availableLineColor,
                TreeMapLineVisualState.Current => currentLineColor,
                TreeMapLineVisualState.Completed => completedLineColor,
                _ => lockedLineColor
            };
        }

        private float GetAlphaForState(TreeMapLineVisualState state)
        {
            return state switch
            {
                TreeMapLineVisualState.Available => availableAlpha,
                TreeMapLineVisualState.Current => currentAlpha,
                TreeMapLineVisualState.Completed => completedAlpha,
                _ => lockedAlpha
            };
        }
    }
}
