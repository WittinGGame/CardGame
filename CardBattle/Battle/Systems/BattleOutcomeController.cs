using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Tracks battle outcome state from unit defeat events.
    /// Does not lock input, stop actions, or transition scenes yet.
    /// </summary>
    public class BattleOutcomeController : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        private readonly List<EnemyBattleUnit> subscribedEnemies = new List<EnemyBattleUnit>();

        public BattleOutcome CurrentOutcome { get; private set; } = BattleOutcome.None;
        public bool IsBattleEnded => CurrentOutcome != BattleOutcome.None;

        public event System.Action<BattleOutcome> OnBattleEnded;

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

        public void ResetOutcome()
        {
            CurrentOutcome = BattleOutcome.None;
            Debug.Log("[BattleOutcome] Outcome reset.");
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
            ResolveOutcome(BattleOutcome.PlayerDefeated);
        }

        private void HandleEnemyDefeated(BattleUnit unit)
        {
            Debug.Log($"[BattleOutcome] Enemy defeated: {unit.name}");

            if (enemyActionSystem == null || IsBattleEnded)
                return;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsAlive)
                    return;
            }

            ResolveOutcome(BattleOutcome.EncounterCleared);
        }

        private void ResolveOutcome(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.None || IsBattleEnded)
                return;

            CurrentOutcome = outcome;

            switch (outcome)
            {
                case BattleOutcome.PlayerDefeated:
                    Debug.Log("[BattleOutcome] Player defeated.");
                    break;
                case BattleOutcome.EncounterCleared:
                    Debug.Log("[BattleOutcome] Encounter cleared.");
                    break;
            }

            OnBattleEnded?.Invoke(outcome);
        }
    }
}
