using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Phase 1: listens for unit defeat and logs battle outcome for debugging.
    /// Does not lock input, stop actions, or transition scenes yet.
    /// </summary>
    public class BattleOutcomeController : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        private readonly List<EnemyBattleUnit> subscribedEnemies = new List<EnemyBattleUnit>();
        private bool encounterClearedLogged;

        private void OnEnable()
        {
            SubscribePlayer();
            SubscribeEnemies();
        }

        private void OnDisable()
        {
            UnsubscribePlayer();
            UnsubscribeEnemies();
        }

        private void SubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDefeatedEvent += HandlePlayerDefeated;
        }

        private void UnsubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDefeatedEvent -= HandlePlayerDefeated;
        }

        private void SubscribeEnemies()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || subscribedEnemies.Contains(enemy))
                    continue;

                enemy.OnDefeatedEvent += HandleEnemyDefeated;
                subscribedEnemies.Add(enemy);
            }
        }

        private void UnsubscribeEnemies()
        {
            for (int i = 0; i < subscribedEnemies.Count; i++)
            {
                var enemy = subscribedEnemies[i];
                if (enemy == null)
                    continue;

                enemy.OnDefeatedEvent -= HandleEnemyDefeated;
            }

            subscribedEnemies.Clear();
        }

        private void HandlePlayerDefeated(BattleUnit unit)
        {
            Debug.Log("[BattleOutcome] Player defeated.");
        }

        private void HandleEnemyDefeated(BattleUnit unit)
        {
            Debug.Log($"[BattleOutcome] Enemy defeated: {unit.name}");
            TryLogEncounterCleared();
        }

        private void TryLogEncounterCleared()
        {
            if (encounterClearedLogged || enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsAlive)
                    return;
            }

            encounterClearedLogged = true;
            Debug.Log("[BattleOutcome] Encounter cleared.");
        }
    }
}
