using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Coordinates enemy reactions to the player's cards and turn boundaries.
    /// Handles countdown interrupts (sorted by <see cref="EnemyBattleUnit.Speed"/> descending)
    /// and end-of-turn attackers. Countdown interrupts may repeat in the same player round.
    /// </summary>
    public class EnemyActionSystem : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private List<EnemyBattleUnit> enemies = new List<EnemyBattleUnit>();
        [SerializeField] private BattleDrawSequenceController battleDrawSequenceController;

        [Header("Turn Presentation")]
        [SerializeField] private TurnPresentationController turnPresentation;

        [Header("Status Timing")]
        [SerializeField] private bool tickStatusesOnPlayerRoundStart = true;
        [SerializeField] private bool skipStatusTickOnFirstPlayerRound = true;
        [SerializeField] private bool verboseStatusTickLogs = false;

        private bool resolvingEnemyActions;
        private bool startingPlayerRound;
        private BattleActionExecution enemySequence;
        private BattleActionExecution roundSequence;

        public BattleActionExecution LastRoundStart { get; private set; }
        public PlayerBattleUnit Player => player;
        public IReadOnlyList<EnemyBattleUnit> Enemies => enemies;
        public bool IsResolvingEnemyActions => resolvingEnemyActions || startingPlayerRound;
        public int CurrentTurn { get; private set; }

        public event System.Action<int> OnTurnStarted;

#if UNITY_EDITOR
        private void OnValidate()
        {
            enemies.RemoveAll(e => e == null);
        }
#endif

        public void ResetTurnCounter()
        {
            ResetRuntimeActions();
            CurrentTurn = 0;
            player?.ResetOwnerCycleState();
            foreach (var enemy in enemies)
                enemy?.ResetOwnerCycleState();
        }

        /// <summary>Designer helper to register enemies without code.</summary>
        public void RegisterEnemy(EnemyBattleUnit enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void ClearRegisteredEnemies()
        {
            ResetRuntimeActions();
            enemies.Clear();
        }

        public void ReplaceRegisteredEnemies(IReadOnlyList<EnemyBattleUnit> newEnemies)
        {
            ResetRuntimeActions();
            enemies.Clear();

            if (newEnemies == null)
                return;

            for (int i = 0; i < newEnemies.Count; i++)
                RegisterEnemy(newEnemies[i]);
        }

        /// <summary>
        /// Begins the player's round: clears enemy attack flags, refreshes AP, and draws cards.
        /// Call this from your battle director after enemy phases (if any) complete.
        /// </summary>
        public void StartPlayerRound()
        {
            StartCoroutine(StartPlayerRoundRoutine());
        }

        public IEnumerator StartPlayerRoundRoutine()
        {
            if (IsResolvingEnemyActions || !isActiveAndEnabled)
                yield break;
            startingPlayerRound = true;
            var execution = new BattleActionExecution();
            roundSequence = execution;
            LastRoundStart = execution;
            execution.Commit();
            try
            {
                yield return execution.Run(StartPlayerRoundCore(), exception => Debug.LogException(exception, this));
                execution.Complete(BattleActionResult.Successful);
            }
            finally
            {
                execution.Complete(BattleActionResult.Cancelled, "Round start interrupted.");
                if (ReferenceEquals(roundSequence, execution))
                {
                    startingPlayerRound = false;
                    roundSequence = null;
                }
            }
        }

        private IEnumerator StartPlayerRoundCore()
        {
            if (player == null)
            {
                throw new System.InvalidOperationException("EnemyActionSystem requires a PlayerBattleUnit reference.");
            }

            CurrentTurn++;
            OnTurnStarted?.Invoke(CurrentTurn);

            if (turnPresentation != null)
                yield return turnPresentation.PlayTurnIntro(CurrentTurn);

            bool tickPlayerStatuses = tickStatusesOnPlayerRoundStart &&
                !(skipStatusTickOnFirstPlayerRound && CurrentTurn <= 1);
            player.BeginOwnerCycle(tickPlayerStatuses);

            // Reset enemy flags
            foreach (var enemy in enemies)
                enemy?.ResetRoundCombatFlags();

            // Player round start state
            player.BeginRoundState();

            if (player.DeckController == null)
            {
                throw new System.InvalidOperationException("Player is missing a DeckController.");
            }

            int requestedDraw = Mathf.Max(0, player.DrawPerRound);
            if (battleDrawSequenceController != null)
            {
                yield return battleDrawSequenceController.DrawCardsRoutine(requestedDraw);
            }
            else
            {
                Debug.LogError(
                    "EnemyActionSystem: BattleDrawSequenceController is missing. " +
                    "Falling back to immediate DrawCards.");
                player.DeckController.DrawCards(requestedDraw);
            }
        }

        /// <summary>
        /// Invoked after a card fully resolves. Steps countdowns, then processes simultaneous interrupts.
        /// </summary>
        public void HandlePlayerSuccessfullyPlayedCard()
        {
            if (player == null || !player.IsAlive || !isActiveAndEnabled || IsResolvingEnemyActions)
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
            StartEnemySequence(RunCountdownAttacksSequentially(ready));
        }

        /// <summary>
        /// Runs after the player discards their hand for ending the turn.
        /// Includes end-turn attackers and eligible countdown attackers.
        /// </summary>
        public void ResolveEndTurnAttacks()
        {
            if (player == null || !player.IsAlive || !isActiveAndEnabled || IsResolvingEnemyActions)
                return;

            var actors = new List<EnemyBattleUnit>();
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                bool isEndTurnAttacker =
                    enemy.Behavior == EnemyBehaviorType.EndTurnAttacker && !enemy.HasAttackedThisPlayerRound;
                bool isEligibleCountdownAttacker =
                    enemy.Behavior == EnemyBehaviorType.CountdownAttacker && enemy.CanExecuteCountdownAttackAtEndTurn();

                if (!isEndTurnAttacker && !isEligibleCountdownAttacker)
                    continue;

                actors.Add(enemy);
            }

            actors.Sort((a, b) => b.Speed.CompareTo(a.Speed));
            StartEnemySequence(RunEndTurnAttacksSequentially(actors));
        }

        private IEnumerator RunCountdownAttacksSequentially(List<EnemyBattleUnit> ready)
        {
            for (int i = 0; i < ready.Count; i++)
            {
                var enemy = ready[i];
                if (player == null || !player.IsAlive) yield break;
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                yield return enemy.ExecuteCountdownAttackRoutine(player);
            }

        }

        private IEnumerator RunEndTurnAttacksSequentially(List<EnemyBattleUnit> actors)
        {
            // Every living enemy advances, including countdown enemies absent from actors.
            var cycleOwners = new List<EnemyBattleUnit>(enemies);
            try
            {
                foreach (var enemy in cycleOwners)
                    if (enemy != null && enemy.IsAlive)
                        enemy.BeginOwnerCycle(tickStatusesOnPlayerRoundStart);
                for (int i = 0; i < actors.Count; i++)
                {
                    var enemy = actors[i];
                    if (player == null || !player.IsAlive) yield break;
                    if (enemy == null || !enemy.isActiveAndEnabled) continue;

                    if (enemy.Behavior == EnemyBehaviorType.CountdownAttacker)
                        yield return enemy.ExecuteEndTurnCountdownAttackRoutine(player);
                    else
                        yield return enemy.ExecuteEndTurnAttackRoutine(player);
                }
            }
            finally
            {
                foreach (var enemy in cycleOwners)
                    enemy?.EndOwnerCycle();
            }
        }

        private void StartEnemySequence(IEnumerator routine)
        {
            resolvingEnemyActions = true;
            var execution = new BattleActionExecution();
            enemySequence = execution;
            execution.Commit();
            StartCoroutine(RunEnemySequence(routine, execution));
        }

        private IEnumerator RunEnemySequence(IEnumerator routine, BattleActionExecution execution)
        {
            try
            {
                yield return execution.Run(routine, exception => Debug.LogException(exception, this));
                execution.Complete(BattleActionResult.Successful);
            }
            finally
            {
                execution.Complete(BattleActionResult.Cancelled, "Enemy sequence interrupted.");
                if (ReferenceEquals(enemySequence, execution))
                {
                    resolvingEnemyActions = false;
                    enemySequence = null;
                }
            }
        }

        public void ResetRuntimeActions()
        {
            enemySequence?.Cancel("Enemy sequence reset.");
            roundSequence?.Cancel("Round start reset.");
            foreach (var enemy in enemies)
                enemy?.CancelRuntimeAction();
            StopAllCoroutines();
            resolvingEnemyActions = false;
            startingPlayerRound = false;
        }

        private void OnDisable()
        {
            ResetRuntimeActions();
        }

        private void TickTurnDurationStatusesForPlayerRoundStart()
        {
            if (!tickStatusesOnPlayerRoundStart)
                return;

            if (skipStatusTickOnFirstPlayerRound && CurrentTurn <= 1)
            {
                if (verboseStatusTickLogs)
                    Debug.Log("[EnemyActionSystem] Skipping status tick on first player round.");

                return;
            }

            if (player != null && player.IsAlive)
                player.TickStatusTurnDuration();

            if (verboseStatusTickLogs)
                DebugPrintBattleStatuses();
        }

        [ContextMenu("Debug Print Battle Statuses")]
        private void DebugPrintBattleStatuses()
        {
            Debug.Log("[EnemyActionSystem] --- Battle Statuses ---");
            Debug.Log($"CurrentTurn={CurrentTurn}");

            if (player != null)
            {
                string playerText = player.StatusController != null
                    ? player.StatusController.BuildDebugText()
                    : "None";
                Debug.Log($"Player: {playerText}");
            }
            else
            {
                Debug.Log("Player: (missing)");
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBattleUnit enemy = enemies[i];
                if (enemy == null)
                {
                    Debug.Log($"Enemy[{i}]: (null)");
                    continue;
                }

                string enemyText = enemy.StatusController != null
                    ? enemy.StatusController.BuildDebugText()
                    : "None";
                Debug.Log($"Enemy[{i}] {enemy.name}: {enemyText}");
            }
        }

        [ContextMenu("Debug Tick Turn Duration Statuses")]
        private void DebugTickTurnDurationStatuses()
        {
            TickTurnDurationStatusesForPlayerRoundStart();
            DebugPrintBattleStatuses();
        }

        [ContextMenu("Debug Print Enemy Planned Actions")]
        private void DebugPrintEnemyPlannedActions()
        {
            Debug.Log(BuildEnemyPlannedActionsDebugText());
        }

        public string BuildEnemyPlannedActionsDebugText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[EnemyActionSystem] --- Enemy Planned Actions ---");

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBattleUnit enemy = enemies[i];
                if (enemy == null)
                {
                    sb.AppendLine($"Enemy[{i}]: (null)");
                    continue;
                }

                string defaultActionName = enemy.Data != null && enemy.Data.DefaultAction != null
                    ? enemy.Data.DefaultAction.DisplayName
                    : "None";

                int fallbackDamage = enemy.Data != null ? enemy.Data.AttackDamage : 0;

                sb.AppendLine(
                    $"Enemy[{i}] {enemy.name} | " +
                    $"alive={enemy.IsAlive} | " +
                    $"behavior={enemy.Behavior} | " +
                    $"countdown={enemy.CurrentCountdown} | " +
                    $"pattern={enemy.CurrentActionPatternName} | " +
                    $"patternIndex={enemy.CurrentActionPatternIndex} | " +
                    $"planned={enemy.CurrentPlannedActionName} | " +
                    $"default={defaultActionName} | " +
                    $"fallbackAttackDamage={fallbackDamage}");
            }

            return sb.ToString();
        }
    }
}
