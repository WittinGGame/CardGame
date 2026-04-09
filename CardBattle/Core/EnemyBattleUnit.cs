using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Enemy-specific state: behavior pattern, countdown, and per-player-round attack tracking.
    /// </summary>
    public class EnemyBattleUnit : BattleUnit
    {
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private BattleUnitView battleUnitView;
        public BattleUnitView View => battleUnitView;

        private int _countdown;
        private bool _hasAttackedThisPlayerRound;
        private bool waitingForHit;
        private bool waitingForFinish;
        private PlayerBattleUnit pendingTarget;
        private bool attackInProgress;

        public EnemyData Data => enemyData;
        public EnemyBehaviorType Behavior => enemyData != null ? enemyData.Behavior : EnemyBehaviorType.EndTurnAttacker;
        public int Speed => enemyData != null ? enemyData.Speed : 0;
        public int CurrentCountdown => _countdown;
        public bool HasAttackedThisPlayerRound => _hasAttackedThisPlayerRound;
        public bool IsAttackInProgress => attackInProgress;

        protected override void Awake()
        {
            base.Awake();
            ApplyEnemyData();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (enemyData != null)
                ApplyEnemyData();
        }
#endif

        /// <summary>Swap template at runtime (e.g. encounter scripting).</summary>
        public void BindEnemyData(EnemyData data)
        {
            enemyData = data;
            ApplyEnemyData();
        }

        private void ApplyEnemyData()
        {
            if (enemyData == null)
                return;

            SetMaxHp(enemyData.MaxHp, true);
            _countdown = enemyData.BaseCountdown;
        }

        /// <summary>Reset flags when a new player round begins.</summary>
        public void ResetRoundCombatFlags()
        {
            _hasAttackedThisPlayerRound = false;
        }

        /// <summary>Countdown attackers lose one tick after each successful player card.</summary>
        public void StepCountdownAfterPlayerCard()
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.CountdownAttacker)
                return;

            if (_countdown > 0)
                _countdown--;
        }

        /// <summary>True immediately after stepping when this unit should interrupt the player.</summary>
        public bool IsCountdownReady => Behavior == EnemyBehaviorType.CountdownAttacker && IsAlive && _countdown <= 0;

        /// <summary>Perform the interrupt attack, mark round flag, and reload countdown.</summary>
        public void ExecuteCountdownAttack(PlayerBattleUnit player)
        {
            if (!IsCountdownReady)
                return;

            StartCoroutine(ExecuteCountdownAttackRoutine(player));
        }

        public IEnumerator ExecuteCountdownAttackRoutine(PlayerBattleUnit player)
        {
            if (!IsCountdownReady)
                yield break;

            yield return PerformStrike(player);
            _countdown = enemyData != null ? enemyData.BaseCountdown : 0;
        }

        /// <summary>End-of-turn attack for <see cref="EnemyBehaviorType.EndTurnAttacker"/>.</summary>
        public void ExecuteEndTurnAttack(PlayerBattleUnit player)
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.EndTurnAttacker)
                return;

            if (_hasAttackedThisPlayerRound)
                return;

            StartCoroutine(ExecuteEndTurnAttackRoutine(player));
        }

        public IEnumerator ExecuteEndTurnAttackRoutine(PlayerBattleUnit player)
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.EndTurnAttacker)
                yield break;

            if (_hasAttackedThisPlayerRound)
                yield break;

            yield return PerformStrike(player);
        }

        private IEnumerator PerformStrike(PlayerBattleUnit player)
        {
            if (attackInProgress || player == null || !player.IsAlive)
                yield break;

            attackInProgress = true;
            pendingTarget = player;
            waitingForHit = true;
            waitingForFinish = true;

            if (View != null)
            {
                SubscribeToViewEvents();
                View.PlayAttack();

                yield return new WaitUntil(() => !waitingForFinish);
                UnsubscribeFromViewEvents();
            }
            else
            {
                ApplyDamageOnHit();
                waitingForFinish = false;
            }

            waitingForHit = false;
            pendingTarget = null;
            attackInProgress = false;
        }

        private void OnDisable()
        {
            UnsubscribeFromViewEvents();
            waitingForHit = false;
            waitingForFinish = false;
            pendingTarget = null;
            attackInProgress = false;
        }

        private void SubscribeToViewEvents()
        {
            if (View == null)
                return;

            UnsubscribeFromViewEvents();
            View.OnAttackHit += HandleAttackHit;
            View.OnActionFinished += HandleActionFinished;
        }

        private void UnsubscribeFromViewEvents()
        {
            if (View == null)
                return;

            View.OnAttackHit -= HandleAttackHit;
            View.OnActionFinished -= HandleActionFinished;
        }

        private void HandleAttackHit()
        {
            if (!waitingForHit)
                return;

            waitingForHit = false;
            ApplyDamageOnHit();
        }

        private void HandleActionFinished()
        {
            if (!waitingForFinish)
                return;

            waitingForFinish = false;
        }

        private void ApplyDamageOnHit()
        {
            if (pendingTarget == null || !pendingTarget.IsAlive)
                return;

            var damage = enemyData != null ? enemyData.AttackDamage : 0;
            bool wasAliveBeforeHit = pendingTarget.IsAlive;

            pendingTarget.TakeDamage(damage);

            if (wasAliveBeforeHit)
            {
                if (pendingTarget.IsAlive)
                    pendingTarget.View?.PlayHurt();
                else
                    pendingTarget.View?.PlayDead();
            }

            _hasAttackedThisPlayerRound = true;
        }
    }
}
