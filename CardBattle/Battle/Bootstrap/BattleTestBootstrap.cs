using System.Collections;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Minimal bootstrap for testing the core battle loop without UI.
    ///
    /// Controls:
    /// 1 = Play hand card at index 0
    /// 2 = Play hand card at index 1
    /// 3 = Play hand card at index 2
    /// 4 = Play hand card at index 3
    /// 5 = Play hand card at index 4
    /// E = End turn
    /// R = Restart battle setup
    /// T = Print battle state to console
    /// </summary>
    public class BattleTestBootstrap : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private DeckController deckController;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Encounter Startup")]
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;
        [SerializeField] private EncounterEnemySceneBinder encounterEnemySceneBinder;
        [SerializeField] private bool prepareRuntimeEncounterOnStartBattle = true;
        [SerializeField] private bool selectDefaultEncounterIfNone = true;
        [SerializeField] private bool requireRuntimeEncounterForBattle = false;

        [Header("Runtime Cleanup")]
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleHUDController battleHUDController;
        [SerializeField] private PileCounterUI pileCounterUI;
        [SerializeField] private bool cleanupRuntimeStateOnStartBattle = true;
        [SerializeField] private bool resetPlayerRuntimeStateOnStartBattle = true;
        [SerializeField] private bool refreshUiAfterBattleStart = true;

        [Header("Optional")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private bool verboseLogs = true;
        [SerializeField] private int defaultTargetEnemyIndex = 0;

        [Header("Run Integration")]
        [SerializeField] private BattleRunBridge battleRunBridge;

        public bool LastEncounterPreparationAttempted { get; private set; }
        public bool LastEncounterPreparationSucceeded { get; private set; }
        public string LastEncounterPreparationError { get; private set; } = string.Empty;
        public int EncounterPreparationSuccessCount { get; private set; }

        public bool LastRuntimeCleanupAttempted { get; private set; }
        public bool LastRuntimeCleanupSucceeded { get; private set; }
        public string LastRuntimeCleanupError { get; private set; } = string.Empty;
        public int RuntimeCleanupSuccessCount { get; private set; }

        private bool _initialized;
        private Coroutine startBattleRoutine;

        private void Start()
        {
            if (autoStartOnPlay)
                StartTestBattle();
        }

        private void Update()
        {
            if (!_initialized)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) TryPlayCardAtHandIndex(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryPlayCardAtHandIndex(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryPlayCardAtHandIndex(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryPlayCardAtHandIndex(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TryPlayCardAtHandIndex(4);

            if (Input.GetKeyDown(KeyCode.E)) EndTurn();
            if (Input.GetKeyDown(KeyCode.R)) StartTestBattle();
            if (Input.GetKeyDown(KeyCode.T)) PrintBattleState();
        }

        [ContextMenu("Start Test Battle")]
        public void StartTestBattle()
        {
            if (startBattleRoutine != null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning("[BattleTestBootstrap] Start battle is already running.");
                }

                return;
            }

            startBattleRoutine = StartCoroutine(StartTestBattleRoutine());
        }

        private IEnumerator StartTestBattleRoutine()
        {
            try
            {
                if (!ValidateReferences())
                    yield break;

                if (!TryCleanupRuntimeStateBeforeBattleStart())
                    yield break;

                if (!TryPrepareRuntimeEncounterForBattle())
                {
                    Debug.LogError(
                        "BattleTestBootstrap: Runtime encounter preparation failed. Battle start aborted.");
                    yield break;
                }

                if (battleRunBridge != null && battleRunBridge.HasActiveRun)
                {
                    if (!battleRunBridge.TryInitializeBattleFromActiveRun())
                    {
                        Debug.LogError(
                            "BattleTestBootstrap: Active run exists, " +
                            "but Battle data could not be initialized.");
                        yield break;
                    }
                }
                else
                {
                    deckController.BuildFromInspectorBlueprint();
                }

                if (refreshUiAfterBattleStart)
                {
                    handUIController?.RefreshHandUI();
                    pileCounterUI?.ForceSyncDisplayedToReal();
                    battleHUDController?.RefreshUIExternal();
                }

                enemyActionSystem.ResetTurnCounter();
                yield return enemyActionSystem.StartPlayerRoundRoutine();

                _initialized = true;

                if (verboseLogs)
                {
                    Debug.Log("=== Test Battle Started ===");
                    PrintBattleState();
                }
            }
            finally
            {
                startBattleRoutine = null;
            }
        }

        [ContextMenu("Debug Cleanup Runtime State")]
        private void DebugCleanupRuntimeState()
        {
            TryCleanupRuntimeStateBeforeBattleStart();
            DebugPrintBattleStartFlowState();
        }

        [ContextMenu("Debug Prepare Runtime Encounter")]
        private void DebugPrepareRuntimeEncounter()
        {
            bool prepared = TryPrepareRuntimeEncounterForBattle();
            Debug.Log($"[BattleTestBootstrap] TryPrepareRuntimeEncounterForBattle => {prepared}");
            DebugPrintBattleStartFlowState();
        }

        [ContextMenu("Debug Print Battle Start Flow State")]
        public void DebugPrintBattleStartFlowState()
        {
            string currentEncounterId = runtimeEncounterContext != null
                ? runtimeEncounterContext.CurrentEncounterId
                : string.Empty;

            bool encounterValid = runtimeEncounterContext != null &&
                                  runtimeEncounterContext.ValidateCurrentEncounter();

            bool binderApplied = encounterEnemySceneBinder != null &&
                                 encounterEnemySceneBinder.HasAppliedEncounterEnemies;

            int enemyCount = enemyActionSystem != null
                ? enemyActionSystem.Enemies.Count
                : -1;

            Debug.Log(
                $"[BattleTestBootstrap] --- Battle Start Flow ---\n" +
                $"Initialized={_initialized}\n" +
                $"PrepareRuntimeEncounterOnStartBattle={prepareRuntimeEncounterOnStartBattle}\n" +
                $"SelectDefaultEncounterIfNone={selectDefaultEncounterIfNone}\n" +
                $"RequireRuntimeEncounterForBattle={requireRuntimeEncounterForBattle}\n" +
                $"CleanupRuntimeStateOnStartBattle={cleanupRuntimeStateOnStartBattle}\n" +
                $"ResetPlayerRuntimeStateOnStartBattle={resetPlayerRuntimeStateOnStartBattle}\n" +
                $"RefreshUiAfterBattleStart={refreshUiAfterBattleStart}\n" +
                $"LastEncounterPreparationAttempted={LastEncounterPreparationAttempted}\n" +
                $"LastEncounterPreparationSucceeded={LastEncounterPreparationSucceeded}\n" +
                $"LastEncounterPreparationError={LastEncounterPreparationError}\n" +
                $"EncounterPreparationSuccessCount={EncounterPreparationSuccessCount}\n" +
                $"LastRuntimeCleanupAttempted={LastRuntimeCleanupAttempted}\n" +
                $"LastRuntimeCleanupSucceeded={LastRuntimeCleanupSucceeded}\n" +
                $"LastRuntimeCleanupError={LastRuntimeCleanupError}\n" +
                $"RuntimeCleanupSuccessCount={RuntimeCleanupSuccessCount}\n" +
                $"CurrentEncounterId={currentEncounterId}\n" +
                $"EncounterValid={encounterValid}\n" +
                $"BinderApplied={binderApplied}\n" +
                $"EnemyActionSystemCount={enemyCount}\n" +
                $"BattleActionRunnerBusy={battleActionRunner != null && battleActionRunner.IsBusy}\n" +
                $"TargetSelectionActive={targetSelectionSystem != null && targetSelectionSystem.IsSelectingTarget}\n" +
                $"PlayerAp={(player != null ? player.CurrentAp.ToString() : "n/a")}/{(player != null ? player.ApPerRound.ToString() : "n/a")}\n" +
                $"PlayerBlock={(player != null ? player.CurrentBlock.ToString() : "n/a")}\n" +
                $"Deck={(deckController != null ? deckController.Deck.Count.ToString() : "n/a")}\n" +
                $"Hand={(deckController != null ? deckController.Hand.Count.ToString() : "n/a")}\n" +
                $"Graveyard={(deckController != null ? deckController.Graveyard.Count.ToString() : "n/a")}");
        }

        public void TryPlayCardAtHandIndex(int handIndex)
        {
            if (!ValidateReferences())
                return;

            var hand = deckController.Hand;
            if (handIndex < 0 || handIndex >= hand.Count)
            {
                if (verboseLogs)
                    Debug.LogWarning($"No card in hand slot {handIndex}.");
                return;
            }

            var card = hand[handIndex];
            var target = GetDefaultAliveEnemy();

            if (card == null)
            {
                if (verboseLogs)
                    Debug.LogWarning($"Hand slot {handIndex} is null.");
                return;
            }

            var success = player.TryPlayCard(card, target);

            if (verboseLogs)
            {
                var targetName = target != null ? target.name : "None";
                Debug.Log($"Play Card [{handIndex}] => {card.Data.DisplayName} | Target: {targetName} | Success: {success}");
                PrintBattleState();
            }

            CheckSimpleBattleEnd();
        }

        public void EndTurn()
        {
            if (!ValidateReferences())
                return;

            player.RequestEndTurn();

            if (verboseLogs)
            {
                Debug.Log("=== End Turn ===");
                PrintBattleState();
            }

            CheckSimpleBattleEnd();

            if (player != null && player.IsAlive && HasAliveEnemy())
            {
                enemyActionSystem.StartPlayerRound();

                if (verboseLogs)
                {
                    Debug.Log("=== New Player Round Started ===");
                    PrintBattleState();
                }
            }
        }

        [ContextMenu("Print Battle State")]
        public void PrintBattleState()
        {
            if (!ValidateReferences())
                return;

            var sb = new StringBuilder();

            sb.AppendLine("----- Battle State -----");

            if (player != null)
            {
                sb.AppendLine($"Player HP: {player.CurrentHp}/{player.MaxHp}");
                sb.AppendLine($"Player AP: {player.CurrentAp}/{player.ApPerRound}");
                sb.AppendLine($"Player Block: {player.CurrentBlock}");
                sb.AppendLine($"Player CanAct: {player.CanAct}");
            }

            sb.AppendLine($"Deck: {deckController.Deck.Count}");
            sb.AppendLine($"Hand: {deckController.Hand.Count}");
            sb.AppendLine($"Graveyard: {deckController.Graveyard.Count}");

            for (int i = 0; i < deckController.Hand.Count; i++)
            {
                var card = deckController.Hand[i];
                if (card?.Data == null) continue;

                sb.AppendLine($"Hand[{i}] = {card.Data.DisplayName} | Cost: {card.Data.ApCost} | Type: {card.Data.CardType}");
            }

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;

                sb.AppendLine(
                    $"Enemy[{i}] {enemy.name} | HP: {enemy.CurrentHp}/{enemy.MaxHp} | Alive: {enemy.IsAlive} | Behavior: {enemy.Behavior} | Countdown: {enemy.CurrentCountdown} | Speed: {enemy.Speed} | ActedThisRound: {enemy.HasAttackedThisPlayerRound}"
                );
            }

            Debug.Log(sb.ToString());
        }

        private bool TryCleanupRuntimeStateBeforeBattleStart()
        {
            LastRuntimeCleanupAttempted = false;
            LastRuntimeCleanupSucceeded = false;
            LastRuntimeCleanupError = string.Empty;

            if (!cleanupRuntimeStateOnStartBattle)
            {
                LastRuntimeCleanupSucceeded = true;
                return true;
            }

            LastRuntimeCleanupAttempted = true;

            battleActionRunner?.ResetRuntimeActionState();
            targetSelectionSystem?.ForceCancelTargetSelection();
            handUIController?.ResetHandRuntimeStateForNewBattle();

            if (resetPlayerRuntimeStateOnStartBattle && player != null)
                player.ResetBattleRuntimeStateForNewEncounter();

            pileCounterUI?.ForceSyncDisplayedToReal();
            battleHUDController?.RefreshUIExternal();

            LastRuntimeCleanupSucceeded = true;
            RuntimeCleanupSuccessCount++;

            if (verboseLogs)
                Debug.Log("[BattleTestBootstrap] Runtime battle state cleanup complete.");

            return true;
        }

        private bool TryPrepareRuntimeEncounterForBattle()
        {
            LastEncounterPreparationAttempted = false;
            LastEncounterPreparationSucceeded = false;
            LastEncounterPreparationError = string.Empty;

            if (!prepareRuntimeEncounterOnStartBattle)
            {
                LastEncounterPreparationSucceeded = true;
                return true;
            }

            LastEncounterPreparationAttempted = true;

            if (runtimeEncounterContext == null)
            {
                if (requireRuntimeEncounterForBattle)
                {
                    return FailEncounterPreparation("RuntimeEncounterContext reference is missing.");
                }

                return WarnAndSkipEncounterPreparation("no RuntimeEncounterContext assigned.");
            }

            if (!runtimeEncounterContext.HasCurrentEncounter)
            {
                if (selectDefaultEncounterIfNone)
                    runtimeEncounterContext.TrySelectDefaultEncounter();

                if (!runtimeEncounterContext.HasCurrentEncounter)
                {
                    if (requireRuntimeEncounterForBattle)
                    {
                        return FailEncounterPreparation(
                            "No current encounter is selected and default selection failed.");
                    }

                    return WarnAndSkipEncounterPreparation(
                        "no current encounter is selected and default selection failed.");
                }
            }

            if (!runtimeEncounterContext.ValidateCurrentEncounter())
            {
                string validationError = runtimeEncounterContext.LastValidationError;
                if (requireRuntimeEncounterForBattle)
                {
                    return FailEncounterPreparation(
                        string.IsNullOrEmpty(validationError)
                            ? "Current encounter is invalid."
                            : validationError);
                }

                return WarnAndSkipEncounterPreparation(
                    string.IsNullOrEmpty(validationError)
                        ? "current encounter is invalid."
                        : validationError);
            }

            if (encounterEnemySceneBinder == null)
            {
                if (requireRuntimeEncounterForBattle)
                {
                    return FailEncounterPreparation("EncounterEnemySceneBinder reference is missing.");
                }

                return WarnAndSkipEncounterPreparation("no EncounterEnemySceneBinder assigned.");
            }

            if (!encounterEnemySceneBinder.TryApplyCurrentEncounterEnemies())
            {
                string binderError = encounterEnemySceneBinder.LastApplyError;
                if (requireRuntimeEncounterForBattle)
                {
                    return FailEncounterPreparation(
                        string.IsNullOrEmpty(binderError)
                            ? "EncounterEnemySceneBinder rejected current encounter."
                            : binderError);
                }

                return WarnAndSkipEncounterPreparation(
                    string.IsNullOrEmpty(binderError)
                        ? "EncounterEnemySceneBinder rejected current encounter."
                        : binderError);
            }

            LastEncounterPreparationSucceeded = true;
            LastEncounterPreparationError = string.Empty;
            EncounterPreparationSuccessCount++;

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleTestBootstrap] Runtime encounter prepared. " +
                    $"Encounter={runtimeEncounterContext.CurrentEncounterId} | " +
                    $"BoundEnemies={encounterEnemySceneBinder.LastBoundEnemyCount}");
            }

            return true;
        }

        private bool FailEncounterPreparation(string error)
        {
            LastEncounterPreparationSucceeded = false;
            LastEncounterPreparationError = error;

            if (verboseLogs)
            {
                Debug.LogError(
                    $"[BattleTestBootstrap] Runtime encounter preparation failed: {error}");
            }

            return false;
        }

        private bool WarnAndSkipEncounterPreparation(string message)
        {
            LastEncounterPreparationSucceeded = true;
            LastEncounterPreparationError = string.Empty;

            if (verboseLogs)
            {
                Debug.LogWarning(
                    $"[BattleTestBootstrap] Runtime encounter preparation skipped: {message}");
            }

            return true;
        }

        private EnemyBattleUnit GetDefaultAliveEnemy()
        {
            var enemies = enemyActionSystem.Enemies;
            if (enemies == null || enemies.Count == 0)
                return null;

            if (defaultTargetEnemyIndex >= 0 &&
                defaultTargetEnemyIndex < enemies.Count &&
                enemies[defaultTargetEnemyIndex] != null &&
                enemies[defaultTargetEnemyIndex].IsAlive)
            {
                return enemies[defaultTargetEnemyIndex];
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return enemies[i];
            }

            return null;
        }

        private bool HasAliveEnemy()
        {
            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return true;
            }

            return false;
        }

        private void CheckSimpleBattleEnd()
        {
            if (player == null)
                return;

            if (!player.IsAlive)
            {
                Debug.Log("=== Defeat: Player HP reached 0 ===");
                return;
            }

            if (!HasAliveEnemy())
            {
                Debug.Log("=== Victory: All enemies defeated ===");
            }
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (player == null)
            {
                Debug.LogError("BattleTestBootstrap: PlayerBattleUnit reference is missing.");
                valid = false;
            }

            if (deckController == null)
            {
                Debug.LogError("BattleTestBootstrap: DeckController reference is missing.");
                valid = false;
            }

            if (enemyActionSystem == null)
            {
                Debug.LogError("BattleTestBootstrap: EnemyActionSystem reference is missing.");
                valid = false;
            }

            return valid;
        }
    }
}
