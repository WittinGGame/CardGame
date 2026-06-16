using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "CardCatalog",
        menuName = "Card Battle/Card Catalog",
        order = 10)]
    public class CardCatalog : ScriptableObject
    {
        [SerializeField] private List<CardData> cards = new List<CardData>();

        private Dictionary<string, CardData> lookup;
        private bool lookupBuilt;

        public int Count => cards != null ? cards.Count : 0;

        public IReadOnlyList<CardData> Cards => cards;

        private void OnEnable()
        {
            lookupBuilt = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            lookupBuilt = false;
        }
#endif

        public bool TryGetCard(string cardId, out CardData cardData)
        {
            cardData = null;

            if (string.IsNullOrWhiteSpace(cardId))
                return false;

            EnsureLookupBuilt();
            return lookup.TryGetValue(cardId, out cardData);
        }

        public void RebuildLookup()
        {
            if (lookup == null)
                lookup = new Dictionary<string, CardData>(StringComparer.Ordinal);
            else
                lookup.Clear();

            lookupBuilt = true;

            if (cards == null)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;

                string id = card.CardId;
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (lookup.ContainsKey(id))
                {
                    CardData existing = lookup[id];
                    Debug.LogError(
                        $"[CardCatalog] Duplicate card ID '{id}'.\n" +
                        $"Existing asset: {existing.name}\n" +
                        $"Duplicate asset: {card.name}");
                    continue;
                }

                lookup[id] = card;
            }
        }

        private void EnsureLookupBuilt()
        {
            if (lookupBuilt)
                return;

            RebuildLookup();
        }
    }
}
