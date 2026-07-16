using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Owns the battle piles (deck, hand, graveyard, exhaust, removed diagnostics) and draw/discard/shuffle rules.
    /// When the deck is empty during a draw, the graveyard is shuffled back into the deck.
    /// Exhausted cards never reshuffle. Draw respects <see cref="MaxHandSize"/>; excess cards
    /// overflow to the graveyard or removed diagnostics (deferred via pending overflow when a multi-step draw may still reshuffle).
    /// </summary>
    public class DeckController : MonoBehaviour
    {
        public enum DiscardDestination
        {
            Graveyard = 0,
            Removed = 1
        }

        public enum EndTurnCardDestination
        {
            Hand = 0,
            Graveyard = 1,
            Removed = 2
        }

        [Tooltip("Optional designer list consumed by BuildFromCardDataList / BuildFromInspectorBlueprint at battle setup.")]
        [SerializeField] private List<CardData> starterDeckBlueprint = new List<CardData>();

        [Tooltip("Maximum number of cards allowed in hand. Drawn cards beyond this overflow to the graveyard.")]
        [SerializeField] private int maxHandSize = 10;

        private readonly List<CardInstance> _deck = new List<CardInstance>();
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly List<CardInstance> _graveyard = new List<CardInstance>();
        private readonly List<CardInstance> _exhaustPile = new List<CardInstance>();
        private readonly List<CardInstance> _removedCards = new List<CardInstance>();

        /// <summary>
        /// Cards drawn while the hand was full, held out of the graveyard until the
        /// current draw operation finishes so they cannot be reshuffled and redrawn
        /// in the same operation.
        /// </summary>
        private readonly List<CardInstance> _pendingOverflow = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Deck => _deck;
        public IReadOnlyList<CardInstance> Hand => _hand;
        public IReadOnlyList<CardInstance> Graveyard => _graveyard;
        public IReadOnlyList<CardInstance> ExhaustPile => _exhaustPile;
        public IReadOnlyList<CardInstance> RemovedCards => _removedCards;

        public int MaxHandSize => Mathf.Max(0, maxHandSize);
        public int AvailableHandSpace => Mathf.Max(0, MaxHandSize - _hand.Count);
        public bool IsHandFull => AvailableHandSpace <= 0;

        /// <summary>Cards drawn past hand capacity that are not yet committed to a final destination.</summary>
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
            _exhaustPile.Clear();
            _removedCards.Clear();
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

        public int GetExhaustCount()
        {
            return _exhaustPile.Count;
        }

        public int GetRemovedCount()
        {
            return _removedCards.Count;
        }

        /// <summary>
        /// Draw up to <paramref name="count"/> cards, reshuffling graveyard into deck as needed.
        /// Overflow is held pending during the operation, then committed to Graveyard or Removed once.
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
        /// Overflow is flushed to Graveyard or Removed immediately (safe default for simple callers).
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
        /// Overflow cards stay in pending overflow so a later reshuffle in the same
        /// multi-step draw cannot pick them up.
        /// </summary>
        public CardDrawResult DrawCardsFromDeckImmediate(int count)
        {
            var result = DrawFromCurrentDeckCore(count, collectDrawn: false, out _);
            NotifyChanged();
            return result;
        }

        /// <summary>
        /// Moves pending overflow cards into Graveyard or Removed in one batch.
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

        /// <summary>
        /// End-turn hand cleanup: Retain cards stay in Hand; non-Retain cards move to Graveyard or Removed.
        /// Manual discard and card play use separate APIs and ignore Retain.
        /// </summary>
        public EndTurnHandResult ResolveEndTurnHand()
        {
            if (_hand.Count == 0)
                return EndTurnHandResult.Empty;

            int discarded = 0;
            int retained = 0;
            int removed = 0;
            bool mutated = false;

            for (int i = _hand.Count - 1; i >= 0; i--)
            {
                var card = _hand[i];
                if (card == null)
                {
                    _hand.RemoveAt(i);
                    mutated = true;
                    continue;
                }

                EndTurnCardDestination destination = ResolveEndTurnDestination(card);
                if (destination == EndTurnCardDestination.Hand)
                {
                    retained++;
                    continue;
                }

                _hand.RemoveAt(i);
                mutated = true;

                if (destination == EndTurnCardDestination.Removed)
                {
                    AddToRemoved(card);
                    removed++;
                    continue;
                }

                if (!_graveyard.Contains(card))
                    _graveyard.Add(card);
                discarded++;
            }

            if (mutated)
                NotifyChanged();

            return new EndTurnHandResult(discarded, retained, removed);
        }

        /// <summary>Compatibility wrapper for end-turn hand cleanup (Retain-aware).</summary>
        public void DiscardEntireHand()
        {
            ResolveEndTurnHand();
        }

        /// <summary>
        /// Whether a hand card should remain in Hand at end turn.
        /// Future upgrade resolution may replace the CardData keyword read with per-instance data.
        /// </summary>
        public static bool ResolveRetainAtEndTurn(CardInstance card)
        {
            return card?.Data != null && card.Data.Retain;
        }

        /// <summary>
        /// Whether a card should be treated as Temporary.
        /// Future upgrade resolution may replace the CardData keyword read with per-instance data.
        /// </summary>
        public static bool ResolveTemporary(CardInstance card)
        {
            return card?.Data != null && card.Data.Temporary;
        }

        /// <summary>Resolves end-turn routing priority from card keywords. Does not mutate piles.</summary>
        public static EndTurnCardDestination ResolveEndTurnDestination(CardInstance card)
        {
            if (ResolveRetainAtEndTurn(card))
                return EndTurnCardDestination.Hand;

            if (ResolveTemporary(card))
                return EndTurnCardDestination.Removed;

            return EndTurnCardDestination.Graveyard;
        }

        /// <summary>Resolves manual discard routing priority. Does not mutate piles.</summary>
        public static DiscardDestination ResolveDiscardDestination(CardInstance card)
        {
            if (ResolveTemporary(card))
                return DiscardDestination.Removed;

            return DiscardDestination.Graveyard;
        }

        /// <summary>
        /// Play resolution: remove from hand and route to Graveyard, Exhaust, or Removed
        /// based on the card's resolved runtime keywords.
        /// </summary>
        public bool PlayCardFromHand(CardInstance card)
        {
            if (card == null || !_hand.Remove(card))
                return false;

            PlayedCardDestination destination = ResolvePlayedCardDestination(card);
            switch (destination)
            {
                case PlayedCardDestination.Exhaust:
                    if (!_exhaustPile.Contains(card))
                        _exhaustPile.Add(card);
                    break;

                case PlayedCardDestination.Removed:
                    AddToRemoved(card);
                    break;

                case PlayedCardDestination.Graveyard:
                default:
                    if (!_graveyard.Contains(card))
                        _graveyard.Add(card);
                    break;
            }

            NotifyChanged();
            return true;
        }

        /// <summary>Resolves play destination from card keywords. Does not mutate piles.</summary>
        public static PlayedCardDestination ResolvePlayedCardDestination(CardInstance card)
        {
            if (ResolveTemporary(card))
                return PlayedCardDestination.Removed;

            if (card?.Data != null && card.Data.ExhaustAfterPlay)
                return PlayedCardDestination.Exhaust;

            return PlayedCardDestination.Graveyard;
        }

        /// <summary>
        /// Manual discard: move one existing hand instance to Graveyard or Removed.
        /// Does not play the card or spend AP. Never routes to Exhaust.
        /// </summary>
        public bool DiscardCardFromHand(CardInstance card)
        {
            if (card == null || !_hand.Remove(card))
                return false;

            RouteCardLeavingHandByDiscard(card);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Manual discard batch. Ignores nulls, duplicates, and cards no longer in hand.
        /// Notifies once if any card moved. Never routes to Exhaust.
        /// </summary>
        public int DiscardCardsFromHand(IReadOnlyList<CardInstance> cards)
        {
            if (cards == null || cards.Count == 0)
                return 0;

            var seen = new HashSet<CardInstance>();
            int moved = 0;

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null || !seen.Add(card))
                    continue;

                if (!_hand.Remove(card))
                    continue;

                RouteCardLeavingHandByDiscard(card);
                moved++;
            }

            if (moved > 0)
                NotifyChanged();

            return moved;
        }

        /// <summary>
        /// Creates new runtime card copies directly in Hand. Generated overflow bypasses PendingOverflow
        /// and goes straight to Removed because no reshuffle is occurring in this API.
        /// </summary>
        public GeneratedCardResult CreateCardsInHand(CardData cardData, int count)
        {
            int requested = Mathf.Max(0, count);
            if (cardData == null || requested <= 0)
                return GeneratedCardResult.Empty(requested);

            if (!cardData.Temporary)
            {
                Debug.LogWarning(
                    $"[DeckController] CreateCardsInHand generated '{cardData.DisplayName}' which is not Temporary. " +
                    "Generated cards should normally be Temporary.",
                    this);
            }

            int created = 0;
            int addedToHand = 0;
            int removedByOverflow = 0;

            for (int i = 0; i < requested; i++)
            {
                var createdCard = new CardInstance(cardData);
                created++;

                if (_hand.Count < MaxHandSize)
                {
                    _hand.Add(createdCard);
                    addedToHand++;
                }
                else
                {
                    AddToRemoved(createdCard);
                    removedByOverflow++;
                }
            }

            if (created > 0)
                NotifyChanged();

            return new GeneratedCardResult(requested, created, addedToHand, removedByOverflow);
        }

        /// <summary>
        /// Future API: move one hand card directly to Exhaust.
        /// Does not spend AP or resolve effects.
        /// </summary>
        public bool ExhaustCardFromHand(CardInstance card)
        {
            if (card == null || !_hand.Remove(card))
                return false;

            if (!_exhaustPile.Contains(card))
                _exhaustPile.Add(card);

            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Future API: batch Hand → Exhaust. Ignores nulls, duplicates, and cards no longer in hand.
        /// </summary>
        public int ExhaustCardsFromHand(IReadOnlyList<CardInstance> cards)
        {
            if (cards == null || cards.Count == 0)
                return 0;

            var seen = new HashSet<CardInstance>();
            int moved = 0;

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null || !seen.Add(card))
                    continue;

                if (!_hand.Remove(card))
                    continue;

                if (!_exhaustPile.Contains(card))
                    _exhaustPile.Add(card);

                moved++;
            }

            if (moved > 0)
                NotifyChanged();

            return moved;
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

            var overflowSnapshot = new List<CardInstance>(_pendingOverflow);
            _pendingOverflow.Clear();

            for (int i = 0; i < overflowSnapshot.Count; i++)
            {
                var overflowCard = overflowSnapshot[i];
                if (overflowCard == null)
                    continue;

                if (ResolveTemporary(overflowCard))
                    AddToRemoved(overflowCard);
                else if (!_graveyard.Contains(overflowCard))
                    _graveyard.Add(overflowCard);
            }

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

        private void RouteCardLeavingHandByDiscard(CardInstance card)
        {
            if (card == null)
                return;

            if (ResolveDiscardDestination(card) == DiscardDestination.Removed)
            {
                AddToRemoved(card);
                return;
            }

            if (!_graveyard.Contains(card))
                _graveyard.Add(card);
        }

        private void AddToRemoved(CardInstance card)
        {
            if (card == null || _removedCards.Contains(card))
                return;

            _deck.Remove(card);
            _hand.Remove(card);
            _graveyard.Remove(card);
            _exhaustPile.Remove(card);
            _pendingOverflow.Remove(card);
            _removedCards.Add(card);
        }

        private void NotifyChanged() => OnPilesChanged?.Invoke();
    }
}
