using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class RunCardRecord
    {
        public string cardId;
        public int upgradeLevel;

        public RunCardRecord()
        {
        }

        public RunCardRecord(string cardId, int upgradeLevel = 0)
        {
            this.cardId = cardId ?? string.Empty;
            this.upgradeLevel = Mathf.Max(0, upgradeLevel);
        }

        public void SetUpgradeLevel(int value)
        {
            upgradeLevel = Mathf.Max(0, value);
        }

        public RunCardRecord Clone()
        {
            return new RunCardRecord(cardId, upgradeLevel);
        }
    }
}
