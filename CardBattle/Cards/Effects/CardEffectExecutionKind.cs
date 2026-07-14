namespace CardBattle.Core
{
    /// <summary>
    /// How a card effect is executed by the sequential effect runner.
    /// </summary>
    public enum CardEffectExecutionKind
    {
        Immediate = 0,
        DrawPresentation = 1,
        ManualHandSelection = 2
    }
}
