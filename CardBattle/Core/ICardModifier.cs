namespace CardBattle.Core
{
    /// <summary>
    /// Hook for future upgrades, relics, or temporary effects that alter how a card resolves.
    /// CardResolver can iterate modifiers before/after base resolution.
    /// </summary>
    public interface ICardModifier
    {
        /// <summary>Called before base card logic; return false to cancel further resolution for this play.</summary>
        bool PreResolve(CardPlayContext context);

        /// <summary>Called after base card logic.</summary>
        void PostResolve(CardPlayContext context);
    }
}
