using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "CardRewardPool",
        menuName = "Card Battle/Rewards/Card Reward Pool",
        order = 20)]
    public class CardRewardPool : ScriptableObject
    {
        [SerializeField] private List<CardData> cards = new List<CardData>();

        public IReadOnlyList<CardData> Cards => cards;

        public int BuildUniqueChoices(
            int requestedCount,
            System.Random random,
            List<CardData> output)
        {
            if (output == null)
                return 0;

            output.Clear();

            if (requestedCount <= 0)
                return 0;

            System.Random rng = random ?? new System.Random();

            var uniqueById = new List<CardData>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;

                string cardId = card.CardId;
                if (string.IsNullOrWhiteSpace(cardId))
                    continue;

                if (!seenIds.Add(cardId))
                    continue;

                uniqueById.Add(card);
            }

            if (uniqueById.Count == 0)
                return 0;

            FisherYatesShuffle(uniqueById, rng);

            int count = Mathf.Min(requestedCount, uniqueById.Count);
            for (int i = 0; i < count; i++)
                output.Add(uniqueById[i]);

            return count;
        }

        private static void FisherYatesShuffle(List<CardData> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                CardData temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cards == null || cards.Count == 0)
                return;

            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                {
                    Debug.LogWarning(
                        $"[CardRewardPool] Null CardData entry at index {i} in '{name}'.",
                        this);
                    continue;
                }

                string cardId = card.CardId;
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    Debug.LogWarning(
                        $"[CardRewardPool] Card at index {i} has a blank CardId in '{name}'.",
                        this);
                    continue;
                }

                if (!seenIds.Add(cardId))
                {
                    Debug.LogWarning(
                        $"[CardRewardPool] Duplicate CardId '{cardId}' at index {i} in '{name}'.",
                        this);
                }
            }
        }
#endif
    }
}
