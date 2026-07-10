using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Owns the three piles (deck, hand, graveyard) and draw/discard/shuffle rules.
    /// When the deck is empty during a draw, the graveyard is shuffled back into the deck.
    /// Draw respects <see cref="MaxHandSize"/>; excess cards overflow to the graveyard
    /// (deferred via pending overflow when a multi-step draw may still reshuffle).
    /// </summary>
    public class DeckController : MonoBehaviour
    {
        [Tooltip("Optional designer list consumed by BuildFromCardDataList / BuildFromInspectorBlueprint at battle setup.")]
        [SerializeField] private List<CardData> starterDeckBlueprint = new List<CardData>();

        [Tooltip("Maximum number of cards allowed in hand. Drawn cards beyond this overflow to the graveyard.")]
        [SerializeField] private int maxHandSize = 10;

        private readonly List<CardInstance> _deck = new List<CardInstance>();
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly List<CardInstance> _graveyard = new List<CardInstance>();

        /// <summary>
        /// Cards drawn while the hand was full, held out of the graveyard until the
        /// current draw operation finishes so they cannot be reshuffled and redrawn
        /// in the same operation.
        /// </summary>
        private readonly List<CardInstance> _pendingOverflow = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Deck => _deck;
        public IReadOnlyList<CardInstance> Hand => _hand;
        public IReadOnlyList<CardInstance> Graveyard => _graveyard;

        public int MaxHandSize => Mathf.Max(0, maxHandSize);
        public int AvailableHandSpace => Mathf.Max(0, MaxHandSize - _hand.Count);
        public bool IsHandFull => AvailableHandSpace <= 0;

        /// <summary>Cards drawn past hand capacity that are not yet committed to the graveyard.</summary>
        public int PendingOverflowCount => _pendingOverflow.Count;

        /// <summary>Fired after any pile mutation so UI or VFX can subscribe later.</summary>
        public event Action OnPilesChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxHandSize = Mathf.Max(0, maxHandSize);
        }
#endif

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
            _pendingOverflow.Clear();
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

        /// <summary>
        /// Draw up to <paramref name="count"/> cards, reshuffling graveyard into deck as needed.
        /// Overflow is held pending during the operation, then committed to the graveyard once.
        /// </summary>
        public CardDrawResult DrawCards(int count)
        {
            int requested = Mathf.Max(0, count);
            if (requested == 0)
                return CardDrawResult.Empty(0);

            int drawn = 0;
            int addedToHand = 0;
            int overflowed = 0;

            for (var i = 0; i < requested; i++)
            {
                if (_deck.Count == 0)
                    ReshuffleGraveyardIntoDeck();

                if (_deck.Count == 0)
                    break;

                if (!TryDrawTopCardFromDeck(out _, out var placedInHand))
                    break;

                drawn++;
                if (placedInHand)
                    addedToHand++;
                else
                    overflowed++;
            }

            // Commit overflow only after reshuffles for this operation are finished.
            FlushPendingOverflowToGraveyard(notify: false);
            NotifyChanged();

            return new CardDrawResult(requested, drawn, addedToHand, overflowed);
        }

        /// <summary>
        /// Draws directly from the current deck without auto-reshuffle, respecting hand capacity.
        /// Overflow is flushed to the graveyard immediately (safe default for simple callers).
        /// For presentation-driven two-phase draws, use <see cref="DrawCardsFromDeckImmediate"/>
        /// and call <see cref="FlushPendingOverflowToGraveyard"/> after the full sequence.
        /// </summary>
        public List<CardInstance> DrawCardsImmediate(int count)
        {
            DrawFromCurrentDeckCore(count, collectDrawn: true, out var drawnCards);
            FlushPendingOverflowToGraveyard(notify: false);
            NotifyChanged();
            return drawnCards;
        }

        /// <summary>
        /// Low-level draw from the current deck only: no reshuffle, respects hand limit.
        /// Overflow cards stay in pending overflow (not graveyard) so a later reshuffle
        /// in the same multi-step draw cannot pick them up.
        /// </summary>
        public CardDrawResult DrawCardsFromDeckImmediate(int count)
        {
            var result = DrawFromCurrentDeckCore(count, collectDrawn: false, out _);
            NotifyChanged();
            return result;
        }

        /// <summary>
        /// Moves pending overflow cards into the graveyard in one batch.
        /// Call after a multi-step draw that may reshuffle between immediate draws.
        /// </summary>
        public int FlushPendingOverflowToGraveyard()
        {
            return FlushPendingOverflowToGraveyard(notify: true);
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

        private CardDrawResult DrawFromCurrentDeckCore(
            int count,
            bool collectDrawn,
            out List<CardInstance> drawnCards)
        {
            int requested = Mathf.Max(0, count);
            drawnCards = collectDrawn ? new List<CardInstance>(requested) : null;

            if (requested == 0)
                return CardDrawResult.Empty(0);

            int drawn = 0;
            int addedToHand = 0;
            int overflowed = 0;

            for (int i = 0; i < requested; i++)
            {
                if (_deck.Count == 0)
                    break;

                if (!TryDrawTopCardFromDeck(out var card, out var placedInHand))
                    break;

                drawn++;
                if (placedInHand)
                    addedToHand++;
                else
                    overflowed++;

                if (collectDrawn)
                    drawnCards.Add(card);
            }

            return new CardDrawResult(requested, drawn, addedToHand, overflowed);
        }

        /// <summary>
        /// Removes the top deck card and places it in hand or pending overflow.
        /// Does not reshuffle, notify, or flush overflow.
        /// </summary>
        private bool TryDrawTopCardFromDeck(out CardInstance card, out bool placedInHand)
        {
            card = null;
            placedInHand = false;
            if (_deck.Count == 0)
                return false;

            int index = _deck.Count - 1;
            card = _deck[index];
            _deck.RemoveAt(index);

            if (_hand.Count < MaxHandSize)
            {
                _hand.Add(card);
                placedInHand = true;
            }
            else
            {
                _pendingOverflow.Add(card);
            }

            return true;
        }

        private int FlushPendingOverflowToGraveyard(bool notify)
        {
            int moved = _pendingOverflow.Count;
            if (moved <= 0)
                return 0;

            for (int i = 0; i < _pendingOverflow.Count; i++)
            {
                var overflowCard = _pendingOverflow[i];
                if (overflowCard != null && !_graveyard.Contains(overflowCard))
                    _graveyard.Add(overflowCard);
            }

            _pendingOverflow.Clear();

            if (notify)
                NotifyChanged();

            return moved;
        }

        private void ReshuffleGraveyardIntoDeck()
        {
            // Intentionally ignores _pendingOverflow so overflow from this draw
            // cannot re-enter the deck mid-operation.
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
            _pendingOverflow.Remove(card);
            if (!_graveyard.Contains(card))
                _graveyard.Add(card);
        }

        private void NotifyChanged() => OnPilesChanged?.Invoke();
    }
}
