namespace CardBattle.Core
{
    /// <summary>
    /// Immutable summary of one <see cref="CardResolver"/> resolution pass.
    /// </summary>
    public readonly struct CardResolutionResult
    {
        public bool UsedEffectsPipeline { get; }
        public int RequestedDrawCount { get; }
        public bool HasDrawRequest => RequestedDrawCount > 0;

        public CardResolutionResult(bool usedEffectsPipeline, int requestedDrawCount)
        {
            UsedEffectsPipeline = usedEffectsPipeline;
            RequestedDrawCount = requestedDrawCount < 0 ? 0 : requestedDrawCount;
        }

        public static CardResolutionResult Empty { get; } = new CardResolutionResult(false, 0);
    }
}
