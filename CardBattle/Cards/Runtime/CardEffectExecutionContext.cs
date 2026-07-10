using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Runtime execution payload containing resolved targets for one effect application.
    /// Also accumulates deferred draw requests for the battle runner to present later.
    /// </summary>
    public class CardEffectExecutionContext
    {
        private int requestedDrawCount;

        public IReadOnlyList<EnemyBattleUnit> EnemyTargets { get; }
        public int RequestedDrawCount => requestedDrawCount;
        public bool HasDrawRequest => requestedDrawCount > 0;

        public CardEffectExecutionContext(IReadOnlyList<EnemyBattleUnit> enemyTargets)
        {
            EnemyTargets = enemyTargets;
            requestedDrawCount = 0;
        }

        /// <summary>Accumulates a deferred draw request. Ignores amount &lt;= 0.</summary>
        public void RequestDraw(int amount)
        {
            if (amount <= 0)
                return;

            requestedDrawCount += amount;
        }
    }
}
