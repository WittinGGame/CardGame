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

        [Header("Audio")]
        [SerializeField] private CombatSFXController combatSfx;

        [Header("UI")]
        [SerializeField] private Transform uiAnchorHP;
        [SerializeField] private Transform uiAnchorIntent;
        [SerializeField] private Transform uiAnchorBuff;
        [SerializeField] private Transform uiAnchorDamage;
        [SerializeField] private Transform uiAnchorTargetGuide;

        public BattleUnitView View => battleUnitView;
        public Transform UIAnchorHP => uiAnchorHP;
        public Transform UIAnchorIntent => uiAnchorIntent;
        public Transform UIAnchorBuff => uiAnchorBuff;
        public Transform UIAnchorDamage => uiAnchorDamage;
        public Transform UIAnchorTargetGuide => uiAnchorTargetGuide;

        private int _countdown;
        private bool _hasAttackedThisPlayerRound;
        private bool waitingForHit;
        private bool waitingForFinish;
        private PlayerBattleUnit pendingTarget;
        private bool attackInProgress;
        private EnemyActionData pendingAction;
        private int pendingAttackDamage;
        private int pendingHitCount;
        private float pendingDelayBetweenHits;
        private bool multiHitInProgress;
        private int currentActionPatternIndex;
        private bool lastActionResolved;

        public event System.Action OnEnemyStateChanged;
        public event System.Action<EnemyBattleUnit> OnPlannedActionChanged;

        public EnemyData Data => enemyData;
        public EnemyBehaviorType Behavior => enemyData != null ? enemyData.Behavior : EnemyBehaviorType.EndTurnAttacker;
        public int Speed => enemyData != null ? enemyData.Speed : 0;
        public int CurrentCountdown => _countdown;
        public bool HasAttackedThisPlayerRound => _hasAttackedThisPlayerRound;
        public bool IsAttackInProgress => attackInProgress;
        public EnemyActionData CurrentPlannedAction { get; private set; }
        public int CurrentActionPatternIndex => currentActionPatternIndex;
        public bool HasActionPattern =>
            Data != null &&
            Data.ActionPattern != null &&
            Data.ActionPattern.HasValidActions();
        public string CurrentPlannedActionName =>
            CurrentPlannedAction != null ? CurrentPlannedAction.DisplayName : "None";
        public string CurrentActionPatternName =>
            HasActionPattern ? Data.ActionPattern.DisplayName : "None";
        public bool AllowEndTurnAttackAfterCountdownAttackThisRound =>
            enemyData != null && enemyData.AllowEndTurnAttackAfterCountdownAttackThisRound;

        protected override void Awake()
        {
            base.Awake();
            ApplyEnemyData();
            ResetActionPatternForEncounter();
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
            ClearStatuses();
            _hasAttackedThisPlayerRound = false;
            attackInProgress = false;
            // Pattern resets once per encounter bind, not per player round or countdown tick.
            ResetActionPatternForEncounter();
            NotifyStateChanged();
        }

        private void ApplyEnemyData()
        {
            if (enemyData == null)
                return;

            SetMaxHp(enemyData.MaxHp, true);
            _countdown = enemyData.BaseCountdown;
        }

        /// <summary>Resets pattern index and planned action when enemy data is applied for a new encounter.</summary>
        public void ResetActionPatternForEncounter()
        {
            ResetActionPatternState();
        }

        public void ResetActionPatternState()
        {
            currentActionPatternIndex = ResolvePatternStartIndex();
            SetCurrentPlannedAction(ResolveCurrentPlannedAction());
        }

        private void SetCurrentPlannedAction(EnemyActionData action)
        {
            if (CurrentPlannedAction == action)
                return;

            CurrentPlannedAction = action;
            OnPlannedActionChanged?.Invoke(this);
        }

        private EnemyActionPatternData ResolveActionPattern()
        {
            return enemyData != null ? enemyData.ActionPattern : null;
        }

        private bool HasValidActionPattern()
        {
            var pattern = ResolveActionPattern();
            return pattern != null && pattern.HasValidActions();
        }

        private int ResolvePatternStartIndex()
        {
            var pattern = ResolveActionPattern();
            if (pattern == null || !pattern.HasValidActions())
                return 0;

            return pattern.GetSafeStartIndex();
        }

        private EnemyActionData ResolveCurrentPlannedAction()
        {
            var pattern = ResolveActionPattern();
            if (pattern == null || !pattern.HasValidActions())
                return null;

            return pattern.GetActionAt(currentActionPatternIndex);
        }

        private EnemyActionData ResolveActionForExecution()
        {
            if (HasValidActionPattern() && CurrentPlannedAction != null)
                return CurrentPlannedAction;

            return enemyData != null ? enemyData.DefaultAction : null;
        }

        private void AdvanceActionPatternAfterResolved()
        {
            var pattern = ResolveActionPattern();
            if (pattern == null || !pattern.HasValidActions())
                return;

            if (pattern.AdvanceMode != EnemyActionPatternAdvanceMode.AfterActionResolved)
                return;

            EnemyActionData previousAction = CurrentPlannedAction;
            currentActionPatternIndex = pattern.GetNextIndex(currentActionPatternIndex);
            SetCurrentPlannedAction(ResolveCurrentPlannedAction());

            if (pattern.VerboseLogs)
            {
                string previousName = previousAction != null ? previousAction.DisplayName : "(none)";
                string nextName = CurrentPlannedAction != null ? CurrentPlannedAction.DisplayName : "(none)";
                Debug.Log(
                    $"[{name}] Pattern advance: {previousName} -> {nextName} " +
                    $"(index {currentActionPatternIndex})",
                    pattern);
            }

            NotifyStateChanged();
        }

        /// <summary>Reset flags when a new player round begins.</summary>
        public void ResetRoundCombatFlags()
        {
            _hasAttackedThisPlayerRound = false;
            NotifyStateChanged();
        }

        /// <summary>Countdown attackers lose one tick after each successful player card.</summary>
        public void StepCountdownAfterPlayerCard()
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.CountdownAttacker)
                return;

            if (_countdown > 0)
            {
                _countdown--;
                NotifyStateChanged();
            }
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

            EnemyActionData action = ResolveActionForExecution();
            yield return PerformAction(player, action);

            if (lastActionResolved)
                AdvanceActionPatternAfterResolved();

            _countdown = enemyData != null ? enemyData.BaseCountdown : 0;
            NotifyStateChanged();
        }

        public bool CanExecuteCountdownAttackAtEndTurn()
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.CountdownAttacker)
                return false;

            if (!_hasAttackedThisPlayerRound)
                return true;

            return AllowEndTurnAttackAfterCountdownAttackThisRound;
        }

        public IEnumerator ExecuteEndTurnCountdownAttackRoutine(PlayerBattleUnit player)
        {
            if (!CanExecuteCountdownAttackAtEndTurn())
                yield break;

            // End-turn rule: eligible countdown attackers can force-ready and strike now.
            _countdown = 0;
            NotifyStateChanged();
            yield return ExecuteCountdownAttackRoutine(player);
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

            EnemyActionData action = ResolveActionForExecution();
            yield return PerformAction(player, action);

            if (lastActionResolved)
                AdvanceActionPatternAfterResolved();
        }

        private IEnumerator PerformAction(PlayerBattleUnit player, EnemyActionData action)
        {
            lastActionResolved = false;

            if (player == null || !player.IsAlive || !IsAlive)
                yield break;

            if (action == null)
            {
                yield return PerformStrike(player);
                yield break;
            }

            pendingAction = action;

            if (action.VerboseLogs)
                Debug.Log($"[{name}] PerformAction: {action.DisplayName}", action);

            if (action.ApplyStatusToSelf)
            {
                ApplyStatus(
                    action.SelfStatusType,
                    action.ResolveSelfStatusAmount(),
                    action.SelfStatusDurationType,
                    action.ResolveSelfStatusDuration(),
                    action.SelfStatusSkipNextTurnTick);
            }

            if (action.DealsAttackDamage)
            {
                yield return PerformAttackDamageAction(player, action);
                if (!lastActionResolved)
                    yield break;

                if (action.ApplyStatusToPlayer)
                    ApplyPlayerStatusFromAction(player, action);

                // Self-buff + attack: OwnerAction self buff is consumed after this damaging action.
                TickStatusOwnerActionDuration();
            }
            else
            {
                ApplyDefendFromAction(action);

                if (action.ApplyStatusToPlayer)
                    ApplyPlayerStatusFromAction(player, action);

                // Pure self-buff (e.g. Battle Cry): keep OwnerAction until a future damaging action.
                lastActionResolved = true;
            }

            _hasAttackedThisPlayerRound = true;
            pendingAction = null;
            NotifyStateChanged();
        }

        private void ApplyPlayerStatusFromAction(PlayerBattleUnit player, EnemyActionData action)
        {
            if (player == null || !player.IsAlive || action == null || !action.ApplyStatusToPlayer)
                return;

            player.ApplyStatus(
                action.PlayerStatusType,
                action.ResolvePlayerStatusAmount(),
                action.PlayerStatusDurationType,
                action.ResolvePlayerStatusDuration(),
                action.PlayerStatusSkipNextTurnTick);
        }

        private void ApplyDefendFromAction(EnemyActionData action)
        {
            if (action == null)
                return;

            if (action.IntentType != EnemyActionIntentType.Defend)
                return;

            int blockAmount = Mathf.Max(0, action.IntentValue);
            if (blockAmount <= 0)
                return;

            AddBlock(blockAmount);

            if (action.VerboseLogs)
                Debug.Log($"[{name}] Defend gained {blockAmount} Block.", action);
        }

        private IEnumerator PerformStrike(PlayerBattleUnit player)
        {
            pendingAttackDamage = enemyData != null ? enemyData.AttackDamage : 0;
            pendingHitCount = 1;
            pendingDelayBetweenHits = 0f;
            yield return PerformAttackAnimation(player);
        }

        private IEnumerator PerformAttackDamageAction(PlayerBattleUnit player, EnemyActionData action)
        {
            pendingAttackDamage = action.ResolveDamage();
            pendingHitCount = action.ResolveHitCount();
            pendingDelayBetweenHits = action.ResolveDelayBetweenHits();
            yield return PerformAttackAnimation(player);
        }

        private IEnumerator PerformAttackAnimation(PlayerBattleUnit player)
        {
            if (attackInProgress || player == null || !player.IsAlive)
            {
                lastActionResolved = false;
                yield break;
            }

            attackInProgress = true;
            pendingTarget = player;
            waitingForHit = true;
            waitingForFinish = true;

            if (View != null)
            {
                SubscribeToViewEvents();
                View.PlayAttack();

                yield return new WaitUntil(() => !waitingForFinish && !multiHitInProgress);
                UnsubscribeFromViewEvents();
            }
            else
            {
                ApplyDamageOnHit();
                if (multiHitInProgress)
                    yield return new WaitUntil(() => !multiHitInProgress);

                waitingForFinish = false;
            }

            waitingForHit = false;
            pendingTarget = null;
            attackInProgress = false;
            lastActionResolved = true;
            NotifyStateChanged();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewEvents();
            waitingForHit = false;
            waitingForFinish = false;
            pendingTarget = null;
            attackInProgress = false;
            pendingAction = null;
            multiHitInProgress = false;
        }

        private void SubscribeToViewEvents()
        {
            if (View == null)
                return;

            UnsubscribeFromViewEvents();
            View.OnAttackHit += HandleAttackHit;
            View.OnAttackPreHit += HandleAttackPreHit;
            View.OnActionFinished += HandleActionFinished;
        }

        private void UnsubscribeFromViewEvents()
        {
            if (View == null)
                return;

            View.OnAttackHit -= HandleAttackHit;
            View.OnAttackPreHit -= HandleAttackPreHit;
            View.OnActionFinished -= HandleActionFinished;
        }

        private void HandleAttackHit()
        {
            if (!waitingForHit)
                return;

            waitingForHit = false;
            ApplyDamageOnHit();
        }

        private void HandleAttackPreHit()
        {
            if (pendingTarget == null || !pendingTarget.IsAlive)
                return;

            if (pendingTarget.CurrentBlock > 0)
                pendingTarget.View?.PlayDefense();
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

            if (pendingAttackDamage <= 0)
                return;

            if (pendingHitCount <= 1)
            {
                ApplySingleHitDamage();
                return;
            }

            StartCoroutine(ApplyMultiHitDamageRoutine());
        }

        private IEnumerator ApplyMultiHitDamageRoutine()
        {
            multiHitInProgress = true;

            for (int i = 0; i < pendingHitCount; i++)
            {
                if (pendingTarget == null || !pendingTarget.IsAlive)
                    break;

                ApplySingleHitDamage();

                if (i >= pendingHitCount - 1)
                    continue;

                if (pendingDelayBetweenHits > 0f)
                    yield return new WaitForSeconds(pendingDelayBetweenHits);
                else
                    yield return null;
            }

            multiHitInProgress = false;
        }

        private void ApplySingleHitDamage()
        {
            if (pendingTarget == null || !pendingTarget.IsAlive)
                return;

            if (pendingAttackDamage <= 0)
                return;

            bool wasAliveBeforeHit = pendingTarget.IsAlive;
            int blockBeforeHit = pendingTarget.CurrentBlock;

            int hpDamage = pendingTarget.TakeAttackDamage(this, pendingAttackDamage);
            bool blockedAnyDamage = blockBeforeHit > pendingTarget.CurrentBlock;

            if (blockedAnyDamage)
                combatSfx?.PlayBlock();
            else if (hpDamage > 0)
                combatSfx?.PlayAttackHit();

            if (wasAliveBeforeHit)
            {
                if (!pendingTarget.IsAlive)
                    pendingTarget.View?.PlayDead();
                else if (hpDamage > 0)
                    pendingTarget.View?.PlayHurt();
            }

            _hasAttackedThisPlayerRound = true;
        }

        private void NotifyStateChanged()
        {
            OnEnemyStateChanged?.Invoke();
        }

        protected override void OnDefeated()
        {
            base.OnDefeated();
            SetCurrentPlannedAction(null);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Print Planned Action")]
        private void DebugPrintPlannedAction()
        {
            Debug.Log(
                $"[EnemyBattleUnit] Planned Action | " +
                $"Enemy={name} | " +
                $"Behavior={Behavior} | " +
                $"Pattern={CurrentActionPatternName} | " +
                $"Index={CurrentActionPatternIndex} | " +
                $"Planned={CurrentPlannedActionName} | " +
                $"Default={(Data != null && Data.DefaultAction != null ? Data.DefaultAction.DisplayName : "None")} | " +
                $"FallbackAttackDamage={(Data != null ? Data.AttackDamage : 0)}",
                this);
        }
#endif
    }
}
