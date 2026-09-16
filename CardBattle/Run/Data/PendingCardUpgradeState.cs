using System.Collections.Generic;

namespace CardBattle.Core
{
    // Persistence only. The future Confirm flow owns creating this commitment.
    [System.Serializable]
    public class PendingCardUpgradeState
    {
        public bool isCommitted;
        public string nodeId;
        public string runCardInstanceId;
        public List<string> offeredBonusUpgradeIds = new List<string>();

        public PendingCardUpgradeState Clone()
        {
            return new PendingCardUpgradeState
            {
                isCommitted = isCommitted,
                nodeId = nodeId,
                runCardInstanceId = runCardInstanceId,
                offeredBonusUpgradeIds = offeredBonusUpgradeIds != null
                    ? new List<string>(offeredBonusUpgradeIds) : null
            };
        }
    }
}
