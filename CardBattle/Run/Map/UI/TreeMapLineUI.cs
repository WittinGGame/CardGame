using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class TreeMapLineUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image image;

        public void Bind(Vector2 start, Vector2 end, Color color, float thickness)
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;

            Vector2 delta = end - start;
            float distance = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            rectTransform.anchoredPosition = (start + end) * 0.5f;
            rectTransform.sizeDelta = new Vector2(distance, thickness);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            SetColor(color);
        }

        public void SetColor(Color color)
        {
            if (image != null)
                image.color = color;
        }
    }
}
