using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class HpBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform colorBarRect;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Bar Size")]
        [SerializeField] private float maxWidth = 620f;

        public void SetHp(int currentHp, int maxHp)
        {
            if (maxHp <= 0)
                maxHp = 1;

            currentHp = Mathf.Clamp(currentHp, 0, maxHp);

            float percent = (float)currentHp / maxHp;
            float width = maxWidth * percent;

            Debug.Log($"SetHp called -> HP: {currentHp}/{maxHp}, width: {width}");

            if (colorBarRect != null)
            {
                colorBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                Debug.Log($"Applied to: {colorBarRect.name}, new width: {colorBarRect.rect.width}");
            }

            if (hpText != null)
                hpText.text = $"{currentHp}/{maxHp}";
        }
    }
}