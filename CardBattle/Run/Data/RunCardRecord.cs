using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class RunCardRecord
    {
        public string cardId;
        public int upgradeLevel;
        public string runCardInstanceId;
        public string selectedBonusUpgradeId = string.Empty;

        public RunCardRecord()
        {
        }

        public RunCardRecord(string cardId, int upgradeLevel = 0)
        {
            runCardInstanceId = System.Guid.NewGuid().ToString("N");
            this.cardId = cardId ?? string.Empty;
            this.upgradeLevel = Mathf.Max(0, upgradeLevel);
        }

        public void SetUpgradeLevel(int value)
        {
            upgradeLevel = Mathf.Max(0, value);
        }

        // Acquisition creates new ownership; snapshots must use Clone instead.
        public RunCardRecord CopyAsNewOwnedCard()
        {
            var copy = Clone();
            copy.runCardInstanceId = System.Guid.NewGuid().ToString("N");
            return copy;
        }

        public RunCardRecord Clone()
        {
            return new RunCardRecord
            {
                cardId = cardId,
                upgradeLevel = upgradeLevel,
                runCardInstanceId = runCardInstanceId,
                selectedBonusUpgradeId = selectedBonusUpgradeId
            };
        }
    }
}
