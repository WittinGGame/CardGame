using System.Collections.Generic;

namespace CardBattle.Core
{
    // Persistence only. Bonfire Confirm owns creating this commitment.
    [System.Serializable]
    public class PendingCardUpgradeState
    {
        public bool isCommitted;
        public string nodeId;
        public string runCardInstanceId;
        public List<string> offeredBonusUpgradeIds = new List<string>();
        // Added without renaming the established Bonus fields so existing saves remain readable.
        // A committed Mutation choice freezes both its result and preview chance here.
        public bool mutationTriggered;
        public float mutationChance;

        public PendingCardUpgradeState Clone()
        {
            return new PendingCardUpgradeState
            {
                isCommitted = isCommitted,
                nodeId = nodeId,
                runCardInstanceId = runCardInstanceId,
                mutationTriggered = mutationTriggered,
                mutationChance = mutationChance,
                offeredBonusUpgradeIds = offeredBonusUpgradeIds != null
                    ? new List<string>(offeredBonusUpgradeIds) : null
            };
        }
    }
}
