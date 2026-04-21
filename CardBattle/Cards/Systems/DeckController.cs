using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Owns the three piles (deck, hand, graveyard) and draw/discard/shuffle rules.
    /// When the deck is empty during a draw, the graveyard is shuffled back into the deck.
    /// </summary>
    public class DeckController : MonoBehaviour
    {
        [Tooltip("Optional designer list consumed by BuildFromCardDataList / BuildFromInspectorBlueprint at battle setup.")]
        [SerializeField] private List<CardData> starterDeckBlueprint = new List<CardData>();

        private readonly List<CardInstance> _deck = new List<CardInstance>();
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly List<CardInstance> _graveyard = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Deck => _deck;
        public IReadOnlyList<CardInstance> Hand => _hand;
        public IReadOnlyList<CardInstance> Graveyard => _graveyard;

        /// <summary>Fired after any pile mutation so UI or VFX can subscribe later.</summary>
        public event Action OnPilesChanged;

        /// <summary>Replace runtime piles using blueprint assets (one <see cref="CardInstance"/> per entry).</summary>
        public void BuildFromCardDataList(IEnumerable<CardData> cards)
        {
            ClearAllPiles();
            if (cards == null)
                return;

            foreach (var data in cards)
            {
                if (data != null)
                    _deck.Add(new CardInstance(data));
            }

            ShuffleDeck();
            NotifyChanged();
        }

        /// <summary>Uses the serialized starter blueprint when no explicit list is provided.</summary>
        public void BuildFromInspectorBlueprint()
        {
            BuildFromCardDataList(starterDeckBlueprint);
        }

        public void ClearAllPiles()
        {
            _deck.Clear();
            _hand.Clear();
            _graveyard.Clear();
            NotifyChanged();
        }

        public bool IsInHand(CardInstance card) => card != null && _hand.Contains(card);

        public int GetDeckCount()
        {
            return _deck.Count;
        }

        public int GetGraveyardCount()
        {
            return _graveyard.Count;
        }

        /// <summary>Draw up to <paramref name="count"/> cards into the hand, reshuffling graveyard into deck as needed.</summary>
        public void DrawCards(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!TryDrawSingleCard())
                    break;
            }

            NotifyChanged();
        }

        /// <summary>
        /// Draws directly from deck to hand without auto-reshuffle.
        /// Use this for presentation-driven two-phase draws.
        /// </summary>
        public List<CardInstance> DrawCardsImmediate(int count)
        {
            var result = new List<CardInstance>();
            int drawCount = Mathf.Max(0, count);

            for (int i = 0; i < drawCount; i++)
            {
                if (_deck.Count == 0)
                    break;

                int index = _deck.Count - 1;
                var card = _deck[index];
                _deck.RemoveAt(index);
                _hand.Add(card);
                result.Add(card);
            }

            NotifyChanged();
            return result;
        }

        /// <summary>Moves all graveyard cards into deck and shuffles. Returns moved card count.</summary>
        public int ReshuffleGraveyardIntoDeckImmediate()
        {
            int moved = _graveyard.Count;
            if (moved <= 0)
                return 0;

            _deck.AddRange(_graveyard);
            _graveyard.Clear();
            ShuffleListInPlace(_deck);
            NotifyChanged();
            return moved;
        }

        /// <summary>Move every card from hand to graveyard (end of player turn).</summary>
        public void DiscardEntireHand()
        {
            for (var i = _hand.Count - 1; i >= 0; i--)
                MoveToGraveyard(_hand[i]);

            NotifyChanged();
        }

        /// <summary>Play resolution: remove from hand and send to graveyard.</summary>
        public void PlayCardFromHand(CardInstance card)
        {
            if (card == null || !_hand.Remove(card))
                return;

            _graveyard.Add(card);
            NotifyChanged();
        }

        public void ShuffleDeck()
        {
            ShuffleListInPlace(_deck);
            NotifyChanged();
        }

        private bool TryDrawSingleCard()
        {
            if (_deck.Count == 0)
                ReshuffleGraveyardIntoDeck();

            if (_deck.Count == 0)
                return false;

            var index = _deck.Count - 1;
            var drawn = _deck[index];
            _deck.RemoveAt(index);
            _hand.Add(drawn);
            return true;
        }

        private void ReshuffleGraveyardIntoDeck()
        {
            if (_graveyard.Count == 0)
                return;

            _deck.AddRange(_graveyard);
            _graveyard.Clear();
            ShuffleListInPlace(_deck);
        }

        private static void ShuffleListInPlace(IList<CardInstance> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void MoveToGraveyard(CardInstance card)
        {
            if (card == null)
                return;

            _hand.Remove(card);
            _deck.Remove(card);
            if (!_graveyard.Contains(card))
                _graveyard.Add(card);
        }

        private void NotifyChanged() => OnPilesChanged?.Invoke();
    }
}
