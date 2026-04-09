namespace CardBattle.Core
{
    public enum EnemyBehaviorType
    {
        /// <summary>Strikes the player once when the player ends their turn (if it has not attacked this round).</summary>
        EndTurnAttacker,

        /// <summary>Countdown drops by 1 each time the player successfully plays a card. At 0, attacks during the player's turn.</summary>
        CountdownAttacker
    }
}
