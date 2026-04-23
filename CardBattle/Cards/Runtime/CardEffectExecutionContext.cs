using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Runtime execution payload containing resolved targets for one effect application.
    /// </summary>
    public class CardEffectExecutionContext
    {
        public IReadOnlyList<EnemyBattleUnit> EnemyTargets { get; }

        public CardEffectExecutionContext(IReadOnlyList<EnemyBattleUnit> enemyTargets)
        {
            EnemyTargets = enemyTargets;
        }
    }
}
