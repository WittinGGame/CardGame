namespace CardBattle.Core
{
    /// <summary>
    /// Outcome of creating new runtime card copies directly in Hand.
    /// </summary>
    public readonly struct GeneratedCardResult
    {
        public int RequestedCount { get; }
        public int CreatedCount { get; }
        public int AddedToHandCount { get; }
        public int RemovedByOverflowCount { get; }

        public GeneratedCardResult(
            int requestedCount,
            int createdCount,
            int addedToHandCount,
            int removedByOverflowCount)
        {
            RequestedCount = requestedCount < 0 ? 0 : requestedCount;
            CreatedCount = createdCount < 0 ? 0 : createdCount;
            AddedToHandCount = addedToHandCount < 0 ? 0 : addedToHandCount;
            RemovedByOverflowCount = removedByOverflowCount < 0 ? 0 : removedByOverflowCount;
        }

        public static GeneratedCardResult Empty(int requestedCount)
        {
            return new GeneratedCardResult(requestedCount, 0, 0, 0);
        }
    }
}
