using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class StatusIconSlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image statusIconImage;
        [SerializeField] private Image directionIconImage;
        [SerializeField] private TextMeshProUGUI numberText;

        [Header("Options")]
        [SerializeField] private bool hideNumberWhenZero = true;

        public void Bind(
            StatusDisplayData data,
            StatusIconDatabase.StatusIconEntry entry,
            Sprite buffArrowIcon,
            Sprite debuffArrowIcon)
        {
            if (statusIconImage != null)
            {
                Sprite statusSprite = entry != null ? entry.icon : null;
                statusIconImage.sprite = statusSprite;
                statusIconImage.enabled = statusSprite != null;
            }

            if (directionIconImage != null)
            {
                Sprite directionSprite = data.IsDebuff ? debuffArrowIcon : buffArrowIcon;
                directionIconImage.sprite = directionSprite;
                directionIconImage.enabled = directionSprite != null;
            }

            if (numberText != null)
            {
                bool showNumber = !hideNumberWhenZero || data.DisplayNumber > 0;
                numberText.gameObject.SetActive(showNumber);

                if (showNumber)
                    numberText.text = data.DisplayNumber.ToString();
                else
                    numberText.text = string.Empty;
            }
        }

        public void Clear()
        {
            if (statusIconImage != null)
            {
                statusIconImage.sprite = null;
                statusIconImage.enabled = false;
            }

            if (directionIconImage != null)
            {
                directionIconImage.sprite = null;
                directionIconImage.enabled = false;
            }

            if (numberText != null)
            {
                numberText.text = string.Empty;
                numberText.gameObject.SetActive(false);
            }
        }
    }
}
