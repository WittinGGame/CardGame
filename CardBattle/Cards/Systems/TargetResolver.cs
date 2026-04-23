using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Utility for resolving effect targets from a play context.
    /// </summary>
    public static class TargetResolver
    {
        public static List<EnemyBattleUnit> ResolveEnemyTargets(CardPlayContext context, CardTargetMode targetMode)
        {
            var result = new List<EnemyBattleUnit>();
            if (context?.Enemies == null)
                return result;

            switch (targetMode)
            {
                case CardTargetMode.SingleEnemy:
                {
                    if (context.PrimaryTarget != null && context.PrimaryTarget.IsAlive)
                    {
                        result.Add(context.PrimaryTarget);
                        return result;
                    }

                    for (int i = 0; i < context.Enemies.Count; i++)
                    {
                        var enemy = context.Enemies[i];
                        if (enemy != null && enemy.IsAlive)
                        {
                            result.Add(enemy);
                            break;
                        }
                    }
                    break;
                }

                case CardTargetMode.AllEnemies:
                {
                    for (int i = 0; i < context.Enemies.Count; i++)
                    {
                        var enemy = context.Enemies[i];
                        if (enemy != null && enemy.IsAlive)
                            result.Add(enemy);
                    }
                    break;
                }

                case CardTargetMode.None:
                case CardTargetMode.Self:
                default:
                    break;
            }

            return result;
        }
    }
}
