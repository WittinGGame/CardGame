using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Coordinates enemy reactions to the player's cards and turn boundaries.
    /// Handles countdown interrupts (sorted by <see cref="EnemyBattleUnit.Speed"/> descending)
    /// and end-of-turn attackers while respecting the "one attack per enemy per player round" rule.
    /// </summary>
    public class EnemyActionSystem : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private List<EnemyBattleUnit> enemies = new List<EnemyBattleUnit>();

        public PlayerBattleUnit Player => player;
        public IReadOnlyList<EnemyBattleUnit> Enemies => enemies;

#if UNITY_EDITOR
        private void OnValidate()
        {
            enemies.RemoveAll(e => e == null);
        }
#endif

        /// <summary>Designer helper to register enemies without code.</summary>
        public void RegisterEnemy(EnemyBattleUnit enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        /// <summary>
        /// Begins the player's round: clears enemy attack flags, refreshes AP, and draws cards.
        /// Call this from your battle director after enemy phases (if any) complete.
        /// </summary>
        public void StartPlayerRound()
        {
            if (player == null)
            {
                Debug.LogError("EnemyActionSystem requires a PlayerBattleUnit reference.");
                return;
            }

            foreach (var enemy in enemies)
                enemy?.ResetRoundCombatFlags();

            player.BeginRoundState();

            if (player.DeckController != null)
                player.DeckController.DrawCards(player.DrawPerRound);
            else
                Debug.LogError("Player is missing a DeckController.");
        }

        /// <summary>
        /// Invoked after a card fully resolves. Steps countdowns, then processes simultaneous interrupts.
        /// </summary>
        public void HandlePlayerSuccessfullyPlayedCard()
        {
            if (player == null)
                return;

            foreach (var enemy in enemies)
                enemy?.StepCountdownAfterPlayerCard();

            var ready = new List<EnemyBattleUnit>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsCountdownReady)
                    ready.Add(enemy);
            }

            ready.Sort((a, b) => b.Speed.CompareTo(a.Speed));

            foreach (var enemy in ready)
                enemy.ExecuteCountdownAttack(player);
        }

        /// <summary>
        /// Runs after the player discards their hand for ending the turn.
        /// Only <see cref="EnemyBehaviorType.EndTurnAttacker"/> enemies participate, and only if they have not attacked yet.
        /// </summary>
        public void ResolveEndTurnAttacks()
        {
            if (player == null)
                return;

            var actors = new List<EnemyBattleUnit>();
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                if (enemy.Behavior != EnemyBehaviorType.EndTurnAttacker)
                    continue;

                if (enemy.HasAttackedThisPlayerRound)
                    continue;

                actors.Add(enemy);
            }

            actors.Sort((a, b) => b.Speed.CompareTo(a.Speed));

            foreach (var enemy in actors)
                enemy.ExecuteEndTurnAttack(player);
        }
    }
}
