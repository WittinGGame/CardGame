namespace CardBattle.Core
{
    /// <summary>
    /// Outcome of end-turn hand resolution (Retain cards stay in Hand; others move to Graveyard or Removed).
    /// </summary>
    public readonly struct EndTurnHandResult
    {
        public int DiscardedCount { get; }
        public int RetainedCount { get; }
        public int RemovedCount { get; }

        public EndTurnHandResult(int discardedCount, int retainedCount, int removedCount)
        {
            DiscardedCount = discardedCount;
            RetainedCount = retainedCount;
            RemovedCount = removedCount;
        }

        public static EndTurnHandResult Empty => new EndTurnHandResult(0, 0, 0);
    }
}
