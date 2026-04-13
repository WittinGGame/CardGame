using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class CardViewUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public void Bind(CardData data)
        {
            if (data == null)
                return;

            // Cost
            costText.text = data.ApCost.ToString();

            // Name
            nameText.text = data.DisplayName;

            // Type
            typeText.text = data.CardType.ToString();

            // Artwork
            artworkImage.sprite = data.Artwork;

            // Description
            descriptionText.text = GetDescription(data);
        }

        private string GetDescription(CardData data)
        {
            switch (data.CardType)
            {
                case CardType.Attack:
                    return $"Deal {data.AttackDamage} damage";

                case CardType.Heal:
                    return $"Heal {data.HealAmount}";

                case CardType.Buff:
                    return $"Gain +{data.BuffPotency}";

                default:
                    return "";
            }
        }
    }
}