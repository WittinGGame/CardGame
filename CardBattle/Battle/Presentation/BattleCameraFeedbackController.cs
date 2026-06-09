using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation bridge: listens to real damage and block-absorb events and triggers camera shake profiles.
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
        [SerializeField] private bool shakeOnBlockAbsorbed = true;

        [Header("Player Damage Shake")]
        [SerializeField] private float playerDamageDuration = 0.10f;
        [SerializeField] private float playerDamageStrength = 0.05f;
        [SerializeField] private float playerDamageFrequency = 36f;

        [Header("Enemy Damage Shake")]
        [SerializeField] private float enemyDamageDuration = 0.12f;
        [SerializeField] private float enemyDamageStrength = 0.06f;
        [SerializeField] private float enemyDamageFrequency = 38f;

        [Header("Block Absorbed Shake")]
        [SerializeField] private float blockShakeDuration = 0.07f;
        [SerializeField] private float blockShakeStrength = 0.025f;
        [SerializeField] private float blockShakeFrequency = 32f;

        private readonly List<EnemyBattleUnit> subscribedEnemies = new List<EnemyBattleUnit>();
        private readonly HashSet<BattleUnit> pendingBlockShakeUnits = new HashSet<BattleUnit>();
        private readonly HashSet<BattleUnit> suppressedBlockShakeUnits = new HashSet<BattleUnit>();
        private Coroutine blockShakeRoutine;

        private void OnEnable()
        {
            SubscribePlayer();
            SubscribeEnemies();
        }

        private void OnDisable()
        {
            UnsubscribePlayer();
            UnsubscribeEnemies();
            ClearPendingBlockShakeState();
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
            player.OnBlockAbsorbedEvent += HandleBlockAbsorbed;
        }

        private void UnsubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent -= HandleUnitDamageTaken;
            player.OnBlockAbsorbedEvent -= HandleBlockAbsorbed;
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
                enemy.OnBlockAbsorbedEvent += HandleBlockAbsorbed;
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
                enemy.OnBlockAbsorbedEvent -= HandleBlockAbsorbed;
            }

            subscribedEnemies.Clear();
        }

        private void HandleBlockAbsorbed(BattleUnit unit, int absorbedAmount)
        {
            if (cameraShake == null || unit == null || absorbedAmount <= 0 || !shakeOnBlockAbsorbed)
                return;

            pendingBlockShakeUnits.Add(unit);
            EnsureBlockShakeRoutineRunning();
        }

        private void HandleUnitDamageTaken(BattleUnit unit, int amount)
        {
            if (unit != null && amount > 0 && pendingBlockShakeUnits.Contains(unit))
                suppressedBlockShakeUnits.Add(unit);

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

        private void EnsureBlockShakeRoutineRunning()
        {
            if (blockShakeRoutine != null)
                return;

            blockShakeRoutine = StartCoroutine(CoProcessPendingBlockShakesEndOfFrame());
        }

        private IEnumerator CoProcessPendingBlockShakesEndOfFrame()
        {
            yield return null;

            if (cameraShake != null && shakeOnBlockAbsorbed)
            {
                foreach (var unit in pendingBlockShakeUnits)
                {
                    if (unit == null || suppressedBlockShakeUnits.Contains(unit))
                        continue;

                    cameraShake.Shake(blockShakeDuration, blockShakeStrength, blockShakeFrequency);
                }
            }

            ClearPendingBlockShakeState();
        }

        private void ClearPendingBlockShakeState()
        {
            if (blockShakeRoutine != null)
            {
                StopCoroutine(blockShakeRoutine);
                blockShakeRoutine = null;
            }

            pendingBlockShakeUnits.Clear();
            suppressedBlockShakeUnits.Clear();
        }
    }
}
