using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public static class BonusUpgradeOfferGenerator
    {
        public static List<string> GetEligibleBonusIds(CardData card, CardUpgradeCatalog catalog)
        {
            var ids = new List<string>();
            if (catalog == null || !catalog.TryGetUpgrade(card, out var upgrade) || !upgrade.HasValidSequence || upgrade.BonusPool == null)
                return ids;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var bonus in upgrade.BonusPool)
            {
                if (bonus == null || !bonus.IsCompatibleWith(card) ||
                    !catalog.TryGetBonus(bonus.BonusId, out var registered) || registered != bonus)
                {
                    Debug.LogWarning($"[BonusOffers] Invalid/unregistered/incompatible pool entry on '{card.CardId}'.");
                    continue;
                }
                if (!seen.Add(bonus.BonusId))
                {
                    Debug.LogWarning($"[BonusOffers] Duplicate pool Bonus '{bonus.BonusId}' ignored.");
                    continue;
                }
                ids.Add(bonus.BonusId);
            }
            return ids;
        }

        // Call only at an explicit future Confirm boundary, never from UI refresh/save retry.
        public static bool TryGenerate(CardData card, CardUpgradeCatalog catalog, System.Random random,
            out List<string> offers)
        {
            offers = GetEligibleBonusIds(card, catalog);
            if (offers.Count == 0 || random == null) return false;
            int count = Math.Min(3, offers.Count);
            for (int i = 0; i < count; i++)
            {
                int j = random.Next(i, offers.Count);
                string swap = offers[i]; offers[i] = offers[j]; offers[j] = swap;
            }
            if (offers.Count > count) offers.RemoveRange(count, offers.Count - count);
            return true;
        }

        // Reopening a committed selection validates its saved IDs, NOT today's offer pool.
        public static bool TryValidatePending(PendingCardUpgradeState pending, RunCardRecord card,
            CardData baseCard, CardUpgradeCatalog catalog)
        {
            if (pending == null || !pending.isCommitted || card == null || baseCard == null ||
                card.cardId != baseCard.CardId || card.runCardInstanceId != pending.runCardInstanceId ||
                card.upgradeLevel != 0 || !string.IsNullOrEmpty(card.selectedBonusUpgradeId) ||
                string.IsNullOrWhiteSpace(pending.nodeId) || catalog == null ||
                !catalog.TryGetUpgrade(baseCard, out var upgrade) || !upgrade.HasValidSequence ||
                pending.offeredBonusUpgradeIds == null || pending.offeredBonusUpgradeIds.Count < 1 ||
                pending.offeredBonusUpgradeIds.Count > 3) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in pending.offeredBonusUpgradeIds)
                if (!seen.Add(id) || !catalog.TryGetBonus(id, out var bonus) || !bonus.IsCompatibleWith(baseCard)) return false;
            return true;
        }
    }
}
