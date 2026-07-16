namespace CardBattle.Core
{
    /// <summary>
    /// Outcome of end-turn hand resolution (Retain cards stay in Hand; others move to Graveyard).
    /// </summary>
    public readonly struct EndTurnHandResult
    {
        public int DiscardedCount { get; }
        public int RetainedCount { get; }

        public EndTurnHandResult(int discardedCount, int retainedCount)
        {
            DiscardedCount = discardedCount;
            RetainedCount = retainedCount;
        }

        public static EndTurnHandResult Empty => new EndTurnHandResult(0, 0);
    }
}
