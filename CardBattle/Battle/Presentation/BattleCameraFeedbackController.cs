using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation bridge: listens to real damage events and triggers camera shake profiles.
    /// </summary>
    public class BattleCameraFeedbackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraShakeController cameraShake;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Toggles")]
        [SerializeField] private bool shakeOnPlayerDamage = true;
        [SerializeField] private bool shakeOnEnemyDamage = true;

        [Header("Player Damage Shake")]
        [SerializeField] private float playerDamageDuration = 0.10f;
        [SerializeField] private float playerDamageStrength = 0.05f;
        [SerializeField] private float playerDamageFrequency = 36f;

        [Header("Enemy Damage Shake")]
        [SerializeField] private float enemyDamageDuration = 0.12f;
        [SerializeField] private float enemyDamageStrength = 0.06f;
        [SerializeField] private float enemyDamageFrequency = 38f;

        private readonly List<EnemyBattleUnit> subscribedEnemies = new List<EnemyBattleUnit>();

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

        /// <summary>
        /// Call this after runtime enemy registration/spawn to resubscribe the current enemy set.
        /// </summary>
        public void RefreshEnemySubscriptions()
        {
            UnsubscribeEnemies();
            SubscribeEnemies();
        }

        private void SubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent += HandleUnitDamageTaken;
        }

        private void UnsubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent -= HandleUnitDamageTaken;
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

                enemy.OnDamageTakenEvent += HandleUnitDamageTaken;
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

                enemy.OnDamageTakenEvent -= HandleUnitDamageTaken;
            }

            subscribedEnemies.Clear();
        }

        private void HandleUnitDamageTaken(BattleUnit unit, int amount)
        {
            if (cameraShake == null || unit == null || amount <= 0)
                return;

            if (player != null && unit == player)
            {
                if (shakeOnPlayerDamage)
                    cameraShake.Shake(playerDamageDuration, playerDamageStrength, playerDamageFrequency);
                return;
            }

            if (unit is EnemyBattleUnit)
            {
                if (shakeOnEnemyDamage)
                    cameraShake.Shake(enemyDamageDuration, enemyDamageStrength, enemyDamageFrequency);
            }
        }
    }
}
