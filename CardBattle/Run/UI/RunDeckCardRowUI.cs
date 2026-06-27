using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class RunDeckCardRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI indexText;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardIdText;
        [SerializeField] private TextMeshProUGUI upgradeText;

        public void Bind(int index, RunCardRecord record, CardData cardData)
        {
            if (indexText != null)
                indexText.text = (index + 1).ToString();

            string cardId = record != null ? record.cardId : string.Empty;
            int upgradeLevel = record != null ? record.upgradeLevel : 0;

            if (cardNameText != null)
            {
                cardNameText.text = cardData != null
                    ? cardData.DisplayName
                    : cardId;
            }

            if (cardIdText != null)
                cardIdText.text = cardId;

            if (upgradeText != null)
            {
                upgradeText.text = upgradeLevel > 0
                    ? $"+{upgradeLevel}"
                    : "Base";
            }
        }
    }
}