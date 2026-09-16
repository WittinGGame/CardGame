using System;
using System.Collections.Generic;

namespace CardBattle.Core
{
    public static class RunCardPersistenceValidation
    {
        // Check all ownership before migration assigns any missing IDs.
        public static bool TryValidate(RunState run, bool allowLegacyBaseIds, out string error)
        {
            error = null;
            if (run == null)
                return Fail("Run state is missing.", out error);

            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (run.currentDeck != null)
            {
                foreach (var card in run.currentDeck)
                {
                    if (card == null || string.IsNullOrWhiteSpace(card.cardId))
                        return Fail("Run deck contains a missing card record or cardId.", out error);

                    if (string.IsNullOrWhiteSpace(card.runCardInstanceId))
                    {
                        if (!allowLegacyBaseIds || card.upgradeLevel != 0 ||
                            !string.IsNullOrEmpty(card.selectedBonusUpgradeId))
                            return Fail("Missing card ownership ID; only legacy base cards can be normalized.", out error);
                    }
                    else if (!ids.Add(card.runCardInstanceId))
                        return Fail("Duplicate runCardInstanceId in run deck.", out error);
                }
            }

            var pending = run.pendingCardUpgrade;
            if (pending == null)
                return true;

            // Unity inline serialization may represent null as an empty default object.
            if (!pending.isCommitted)
            {
                if (!string.IsNullOrEmpty(pending.nodeId) ||
                    !string.IsNullOrEmpty(pending.runCardInstanceId) ||
                    (pending.offeredBonusUpgradeIds != null && pending.offeredBonusUpgradeIds.Count > 0))
                    return Fail("Uncommitted pending Upgrade contains selection data.", out error);
                return true;
            }

            if (string.IsNullOrWhiteSpace(pending.nodeId) ||
                string.IsNullOrWhiteSpace(pending.runCardInstanceId) ||
                !ids.Contains(pending.runCardInstanceId))
                return Fail("Pending Upgrade has no node or references an unknown owned card.", out error);

            if (pending.offeredBonusUpgradeIds == null || pending.offeredBonusUpgradeIds.Count == 0)
                return Fail("Committed Upgrade has no offered Bonus IDs.", out error);

            var offers = new HashSet<string>(StringComparer.Ordinal);
            foreach (string offer in pending.offeredBonusUpgradeIds)
            {
                if (string.IsNullOrWhiteSpace(offer) || !offers.Add(offer))
                    return Fail("Pending Upgrade contains empty or duplicate Bonus IDs.", out error);
            }
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
