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
        [SerializeField] private Button button;

        private CardInstance boundCard;

        public CardInstance BoundCard => boundCard;

        public void Bind(CardInstance card)
        {
            boundCard = card;

            if (card?.Data == null)
                return;

            var data = card.Data;

            if (costText != null)
                costText.text = data.ApCost.ToString();

            if (nameText != null)
                nameText.text = data.DisplayName;

            if (typeText != null)
                typeText.text = data.CardType.ToString();

            if (artworkImage != null)
                artworkImage.sprite = data.Artwork;

            if (descriptionText != null)
                descriptionText.text = GetDescription(data);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        public void SetClickAction(UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();

            if (action != null)
                button.onClick.AddListener(action);
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