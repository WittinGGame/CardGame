namespace CardBattle.Core
{
    /// <summary>
    /// Outcome of a draw operation against the current draw pile / hand capacity rules.
    /// </summary>
    public readonly struct CardDrawResult
    {
        /// <summary>How many cards the caller asked to draw.</summary>
        public int RequestedCount { get; }

        /// <summary>How many cards were actually removed from the draw pile.</summary>
        public int DrawnCount { get; }

        /// <summary>How many of the drawn cards entered the hand.</summary>
        public int AddedToHandCount { get; }

        /// <summary>
        /// How many drawn cards exceeded hand capacity and were routed to overflow
        /// (pending or already committed to the graveyard, depending on the API).
        /// </summary>
        public int OverflowedToGraveyardCount { get; }

        public CardDrawResult(
            int requestedCount,
            int drawnCount,
            int addedToHandCount,
            int overflowedToGraveyardCount)
        {
            RequestedCount = requestedCount;
            DrawnCount = drawnCount;
            AddedToHandCount = addedToHandCount;
            OverflowedToGraveyardCount = overflowedToGraveyardCount;
        }

        public static CardDrawResult Empty(int requestedCount)
        {
            return new CardDrawResult(requestedCount, 0, 0, 0);
        }
    }
}
