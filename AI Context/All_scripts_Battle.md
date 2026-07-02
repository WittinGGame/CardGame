## FILE: BattleTestBootstrap.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Bootstrap/BattleTestBootstrap.cs`
```csharp
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
```

## FILE: BattleStartFlowDebugTest.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Bootstrap/Debug/BattleStartFlowDebugTest.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleStartFlowDebugTest : MonoBehaviour
    {
        [SerializeField] private BattleTestBootstrap battleTestBootstrap;

        [ContextMenu("Debug Start Battle")]
        private void DebugStartBattle()
        {
            if (!TryGetBootstrap())
                return;

            battleTestBootstrap.StartTestBattle();
            DebugPrint();
        }

        [ContextMenu("Debug Print Battle Start Flow")]
        private void DebugPrint()
        {
            if (!TryGetBootstrap())
                return;

            battleTestBootstrap.DebugPrintBattleStartFlowState();
        }

        private bool TryGetBootstrap()
        {
            if (battleTestBootstrap != null)
                return true;

            Debug.LogError("[BattleStartFlowDebugTest] BattleTestBootstrap reference is missing.");
            return false;
        }
    }
}
```

## FILE: BattleActionRunner.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Systems/BattleActionRunner.cs`
```csharp
using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Event-driven action sequencer.
    /// Player attack timing is controlled by BattleUnitView animation events:
    /// - AnimEvent_AttackHit
    /// - AnimEvent_ActionFinished
    /// </summary>
    public class BattleActionRunner : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private DeckController deckController;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleHUDController battleHUDController;
        [SerializeField] private CardToGraveyardVFXController graveyardVfx;
        [SerializeField] private PileCounterUI pileCounterUI;

        [Header("Battle State")]
        [SerializeField] private BattleOutcomeController battleOutcomeController;

        [Header("Audio")]
        [SerializeField] private CardSFXController cardSfx;
        [SerializeField] private CombatSFXController combatSfx;

        [Header("Fallback / Non-Attack Timing")]
        [SerializeField] private float nonAttackResolvePause = 0.05f;
        [SerializeField] private float endTurnPause = 0.2f;
        [SerializeField] private float enemyResolveSafetyPause = 0.1f;

        public bool IsBusy { get; private set; }
        public event System.Action<bool> OnBusyStateChanged;

        private bool HasBattleEnded =>
            battleOutcomeController != null &&
            battleOutcomeController.IsBattleEnded;

        public bool CanAcceptInput =>
            !IsBusy &&
            !HasBattleEnded &&
            player != null &&
            player.CanAct &&
            player.IsAlive;

        private bool waitingForPlayerHit;
        private bool waitingForPlayerFinish;
        private bool playerAttackResolved;
        private CardPlayContext pendingPlayerCardContext;
        private EnemyBattleUnit pendingPrimaryTarget;
        private Coroutine runningActionRoutine;

        private void OnEnable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            if (runningActionRoutine != null)
            {
                StopCoroutine(runningActionRoutine);
                runningActionRoutine = null;
            }

            CleanupPlayerAttackState();
            SetBusy(false);
        }

        public void TryPlayCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            if (HasBattleEnded ||
                IsBusy ||
                card?.Data == null ||
                player == null ||
                !player.IsAlive)
            {
                return;
            }

            runningActionRoutine = StartCoroutine(PlayCardSequence(card, primaryTarget));
        }

        public void ResetRuntimeActionState()
        {
            if (runningActionRoutine != null)
            {
                StopCoroutine(runningActionRoutine);
                runningActionRoutine = null;
            }

            CleanupPlayerAttackState();
            SetBusy(false);
            RefreshExternalUI();
        }

        public void TryEndTurn()
        {
            if (HasBattleEnded ||
                IsBusy ||
                player == null ||
                !player.IsAlive ||
                !player.CanAct)
            {
                return;
            }

            runningActionRoutine = StartCoroutine(EndTurnSequence());
        }

        private IEnumerator PlayCardSequence(CardInstance card, EnemyBattleUnit primaryTarget)
        {
            try
            {
                if (!ValidateCardPlay(card))
                    yield break;

                SetBusy(true);
                RefreshExternalUI();
                cardSfx?.PlayCardPlayed(card.Data.CardType);

                int cost = card.Data.ApCost;
                player.SpendApFromRunner(cost);

                CardViewUI handViewForVfx =
                    handUIController != null ? handUIController.GetViewForCard(card) : null;
                if (graveyardVfx != null)
                    graveyardVfx.PlaySingleCardToGraveyard(handViewForVfx);

                deckController.PlayCardFromHand(card);

                if (graveyardVfx == null)
                    pileCounterUI?.ForceSyncDisplayedToReal();

                bool isAttack = card.Data.CardType == CardType.Attack;
                pendingPrimaryTarget = primaryTarget;

                if (isAttack)
                {
                    if (player?.View == null)
                    {
                        Debug.LogWarning("BattleActionRunner: Player view is missing, falling back to immediate resolve.");
                        ResolvePlayerCardImmediate(card, primaryTarget);
                    }
                    else
                    {
                        pendingPlayerCardContext = new CardPlayContext(player, card, enemyActionSystem.Enemies, primaryTarget);
                        waitingForPlayerHit = true;
                        waitingForPlayerFinish = true;
                        playerAttackResolved = false;

                        SubscribePlayerViewEvents();
                        player.View.PlayAttack();

                        yield return new WaitUntil(() => !waitingForPlayerFinish);

                        CleanupPlayerAttackState();
                    }
                }
                else
                {
                    ResolvePlayerCardImmediate(card, primaryTarget);
                    yield return new WaitForSeconds(nonAttackResolvePause);
                }

                if (HasBattleEnded)
                {
                    RefreshExternalUI();
                    SetBusy(false);
                    RefreshExternalUI();
                    yield break;
                }

                enemyActionSystem.HandlePlayerSuccessfullyPlayedCard();

                if (enemyActionSystem.IsResolvingEnemyActions)
                {
                    yield return new WaitUntil(() => !enemyActionSystem.IsResolvingEnemyActions);
                }
                else
                {
                    yield return new WaitForSeconds(enemyResolveSafetyPause);
                }

                RefreshExternalUI();
                SetBusy(false);
                RefreshExternalUI();
            }
            finally
            {
                runningActionRoutine = null;
            }
        }

        private IEnumerator EndTurnSequence()
        {
            try
            {
                yield return EndTurnSequenceCore();
            }
            finally
            {
                runningActionRoutine = null;
            }
        }

        private IEnumerator EndTurnSequenceCore()
        {
            SetBusy(true);
            RefreshExternalUI();

            if (graveyardVfx != null && handUIController != null)
                graveyardVfx.PlayBatchCardsToGraveyard(handUIController.GetCurrentHandViewsSnapshot());

            player.CommitEndTurnFromRunner();

            if (graveyardVfx == null)
                pileCounterUI?.ForceSyncDisplayedToReal();
            yield return new WaitForSeconds(endTurnPause);

            enemyActionSystem.ResolveEndTurnAttacks();

            if (enemyActionSystem.IsResolvingEnemyActions)
            {
                yield return new WaitUntil(() => !enemyActionSystem.IsResolvingEnemyActions);
            }
            else
            {
                yield return new WaitForSeconds(enemyResolveSafetyPause);
            }

            if (!HasBattleEnded &&
                player != null &&
                player.IsAlive &&
                HasAliveEnemy())
            {
                yield return enemyActionSystem.StartPlayerRoundRoutine();
            }

            RefreshExternalUI();
            SetBusy(false);
            RefreshExternalUI();
        }

        private void ResolvePlayerCardImmediate(CardInstance card, EnemyBattleUnit primaryTarget)
        {
            var context = new CardPlayContext(player, card, enemyActionSystem.Enemies, primaryTarget);
            cardResolver.Resolve(context);
        }

        private void SubscribePlayerViewEvents()
        {
            if (player?.View == null)
                return;

            CleanupPlayerViewSubscriptions();
            player.View.OnAttackHit += HandlePlayerAttackHit;
            player.View.OnActionFinished += HandlePlayerActionFinished;
        }

        private void CleanupPlayerViewSubscriptions()
        {
            if (player?.View == null)
                return;

            player.View.OnAttackHit -= HandlePlayerAttackHit;
            player.View.OnActionFinished -= HandlePlayerActionFinished;
        }

        private void HandlePlayerAttackHit()
        {
            if (!waitingForPlayerHit || playerAttackResolved)
                return;

            waitingForPlayerHit = false;
            playerAttackResolved = true;

            if (pendingPlayerCardContext == null)
                return;

            if (HasValidAttackHitTarget(pendingPlayerCardContext))
                combatSfx?.PlayAttackHit();

            cardResolver.Resolve(pendingPlayerCardContext);
        }

        private void HandlePlayerActionFinished()
        {
            if (!waitingForPlayerFinish)
                return;

            if (waitingForPlayerHit && !playerAttackResolved)
            {
                waitingForPlayerHit = false;
                playerAttackResolved = true;

                if (pendingPlayerCardContext != null)
                    cardResolver.Resolve(pendingPlayerCardContext);
            }

            waitingForPlayerFinish = false;
        }

        private void CleanupPlayerAttackState()
        {
            CleanupPlayerViewSubscriptions();
            waitingForPlayerHit = false;
            waitingForPlayerFinish = false;
            playerAttackResolved = false;
            pendingPlayerCardContext = null;
            pendingPrimaryTarget = null;
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            RefreshExternalUI();
        }

        private bool ValidateCardPlay(CardInstance card)
        {
            if (player == null || deckController == null || cardResolver == null || enemyActionSystem == null)
            {
                Debug.LogError("BattleActionRunner missing references.");
                return false;
            }

            if (HasBattleEnded)
                return false;

            if (!player.CanAct || !player.IsAlive)
                return false;

            if (!deckController.IsInHand(card))
                return false;

            if (!player.CanSpendAp(card.Data.ApCost))
                return false;

            return true;
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

        private void RefreshExternalUI()
        {
            handUIController?.RefreshInteractivityExternal();
            battleHUDController?.RefreshUIExternal();
        }

        private void SetBusy(bool value)
        {
            if (IsBusy == value)
                return;

            IsBusy = value;
            OnBusyStateChanged?.Invoke(IsBusy);
        }

        private static bool HasValidAttackHitTarget(CardPlayContext context)
        {
            if (context?.Card?.Data == null)
                return false;

            CardData cardData = context.Card.Data;

            // Single-target attack
            if (cardData.TargetMode == CardTargetMode.SingleEnemy)
            {
                return context.PrimaryTarget != null &&
                    context.PrimaryTarget.IsAlive;
            }

            // All-enemy attack
            if (cardData.TargetMode == CardTargetMode.AllEnemies)
            {
                if (context.Enemies == null)
                    return false;

                for (int i = 0; i < context.Enemies.Count; i++)
                {
                    EnemyBattleUnit enemy = context.Enemies[i];

                    if (enemy != null && enemy.IsAlive)
                        return true;
                }

                return false;
            }

            // Legacy attack cards without the Effects pipeline
            if (cardData.CardType == CardType.Attack)
            {
                if (context.PrimaryTarget != null &&
                    context.PrimaryTarget.IsAlive)
                {
                    return true;
                }

                if (context.Enemies == null)
                    return false;

                for (int i = 0; i < context.Enemies.Count; i++)
                {
                    EnemyBattleUnit enemy = context.Enemies[i];

                    if (enemy != null && enemy.IsAlive)
                        return true;
                }
            }

            return false;
        }
    }
}
```

## FILE: EnemyActionSystem.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Systems/EnemyActionSystem.cs`
```csharp
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        [SerializeField] private GraveyardToDeckVFXController graveyardToDeckVfx;
        [SerializeField] private float postReshuffleDrawDelay = 0.08f;

        [Header("Turn Presentation")]
        [SerializeField] private TurnPresentationController turnPresentation;

        [Header("Status Timing")]
        [SerializeField] private bool tickStatusesOnPlayerRoundStart = true;
        [SerializeField] private bool skipStatusTickOnFirstPlayerRound = true;
        [SerializeField] private bool verboseStatusTickLogs = false;

        private Coroutine runningEnemyActions;

        public PlayerBattleUnit Player => player;
        public IReadOnlyList<EnemyBattleUnit> Enemies => enemies;
        public bool IsResolvingEnemyActions => runningEnemyActions != null;
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
            CurrentTurn = 0;
        }

        /// <summary>Designer helper to register enemies without code.</summary>
        public void RegisterEnemy(EnemyBattleUnit enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void ClearRegisteredEnemies()
        {
            enemies.Clear();
        }

        public void ReplaceRegisteredEnemies(IReadOnlyList<EnemyBattleUnit> newEnemies)
        {
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
            if (player == null)
            {
                Debug.LogError("EnemyActionSystem requires a PlayerBattleUnit reference.");
                yield break;
            }

            CurrentTurn++;
            OnTurnStarted?.Invoke(CurrentTurn);

            if (turnPresentation != null)
                yield return turnPresentation.PlayTurnIntro(CurrentTurn);

            TickTurnDurationStatusesForPlayerRoundStart();

            // Reset enemy flags
            foreach (var enemy in enemies)
                enemy?.ResetRoundCombatFlags();

            // Player round start state
            player.BeginRoundState();

            if (player.DeckController == null)
            {
                Debug.LogError("Player is missing a DeckController.");
                yield break;
            }

            int requestedDraw = Mathf.Max(0, player.DrawPerRound);

            // ==============================
            // STEP A — DRAW FROM DECK FIRST
            // ==============================
            int availableDeck = player.DeckController.GetDeckCount();
            int firstDraw = Mathf.Min(requestedDraw, availableDeck);

            if (firstDraw > 0)
                player.DeckController.DrawCardsImmediate(firstDraw);

            int remaining = requestedDraw - firstDraw;
            if (remaining <= 0)
                yield break;

            // ==============================
            // STEP B — RESHUFFLE PRESENTATION
            // ==============================
            int graveCount = player.DeckController.GetGraveyardCount();
            if (graveCount <= 0)
                yield break;

            if (graveyardToDeckVfx != null)
                yield return graveyardToDeckVfx.PlayReshuffleVfx(graveCount);

            // ==============================
            // STEP C — APPLY REAL RESHUFFLE
            // ==============================
            player.DeckController.ReshuffleGraveyardIntoDeckImmediate();

            // ==============================
            // STEP D — SMALL DELAY (POLISH)
            // ==============================
            if (postReshuffleDrawDelay > 0f)
                yield return new WaitForSeconds(postReshuffleDrawDelay);

            // ==============================
            // STEP E — DRAW REMAINING CARDS
            // ==============================
            int secondDraw = Mathf.Min(remaining, player.DeckController.GetDeckCount());

            if (secondDraw > 0)
                player.DeckController.DrawCardsImmediate(secondDraw);
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
            if (runningEnemyActions != null)
                return;

            runningEnemyActions = StartCoroutine(RunCountdownAttacksSequentially(ready));
        }

        /// <summary>
        /// Runs after the player discards their hand for ending the turn.
        /// Includes end-turn attackers and eligible countdown attackers.
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

                bool isEndTurnAttacker =
                    enemy.Behavior == EnemyBehaviorType.EndTurnAttacker && !enemy.HasAttackedThisPlayerRound;
                bool isEligibleCountdownAttacker =
                    enemy.Behavior == EnemyBehaviorType.CountdownAttacker && enemy.CanExecuteCountdownAttackAtEndTurn();

                if (!isEndTurnAttacker && !isEligibleCountdownAttacker)
                    continue;

                actors.Add(enemy);
            }

            actors.Sort((a, b) => b.Speed.CompareTo(a.Speed));
            if (runningEnemyActions != null)
                return;

            runningEnemyActions = StartCoroutine(RunEndTurnAttacksSequentially(actors));
        }

        private IEnumerator RunCountdownAttacksSequentially(List<EnemyBattleUnit> ready)
        {
            for (int i = 0; i < ready.Count; i++)
            {
                var enemy = ready[i];
                if (enemy == null)
                    continue;

                yield return enemy.ExecuteCountdownAttackRoutine(player);
            }

            runningEnemyActions = null;
        }

        private IEnumerator RunEndTurnAttacksSequentially(List<EnemyBattleUnit> actors)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                var enemy = actors[i];
                if (enemy == null)
                    continue;

                if (enemy.Behavior == EnemyBehaviorType.CountdownAttacker)
                    yield return enemy.ExecuteEndTurnCountdownAttackRoutine(player);
                else
                    yield return enemy.ExecuteEndTurnAttackRoutine(player);
            }

            runningEnemyActions = null;
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

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBattleUnit enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                enemy.TickStatusTurnDuration();
            }

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
```

## FILE: TargetSelectionSystem.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Systems/TargetSelectionSystem.cs`
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class TargetSelectionSystem : MonoBehaviour
    {
        public enum GuideStartSource
        {
            Card,
            Player
        }

        [SerializeField] private GuideStartSource guideStartSource = GuideStartSource.Card;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private EnemyTargetHighlight[] enemyHighlights;
        [SerializeField] private TargetGuideLineUI guideLine;
        [SerializeField] private RectTransform handContainer;
        [SerializeField] private float handPaddingX = 30f;
        [SerializeField] private float handPaddingY = 20f;

        [Header("Battle State")]
        [SerializeField] private BattleOutcomeController battleOutcomeController;

        public bool IsSelectingTarget => _pendingCard != null;

        private CardInstance _pendingCard;
        private RectTransform _selectedCardRect;
        private RectTransform _selectedCardGuideStartAnchor;
        private bool _canShowGuideLine;

        private bool HasBattleEnded =>
            battleOutcomeController != null &&
            battleOutcomeController.IsBattleEnded;

        private void OnEnable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            ForceCancelTargetSelection();
        }

        private void Update()
        {
            if (HasBattleEnded)
            {
                if (IsSelectingTarget)
                    ForceCancelTargetSelection();

                return;
            }

            if (!IsSelectingTarget)
                return;

            // คลิกขวา
            if (Input.GetMouseButtonDown(1))
            {
                CancelTargetSelection();
                return;
            }

            // กด ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargetSelection();
                return;
            }

            bool insideSelectedCard = IsPointerInsideHandArea();
            if (insideSelectedCard)
            {
                _canShowGuideLine = false;
                if (guideLine != null)
                    guideLine.Hide();
                return;
            }

            if (!_canShowGuideLine)
            {
                _canShowGuideLine = true;
                ShowGuideLineFromCurrentStartSource();
            }

            if (_canShowGuideLine && guideLine != null)
            {
                Transform hoveredEnemyAnchor = GetHoveredEnemyAnchor();
                if (hoveredEnemyAnchor != null)
                    guideLine.UpdateTowardEnemy(hoveredEnemyAnchor);
                else
                    guideLine.UpdateTowardScreen(Input.mousePosition);
            }
        }

        /// <summary>This flow only supports picking one enemy; gate entry by card rules.</summary>
        private bool CanUseSingleEnemySelection(CardData data)
        {
            if (data == null)
                return false;

            if (data.HasEffects)
                return data.TargetMode == CardTargetMode.SingleEnemy;

            return data.CardType == CardType.Attack;
        }

        public void BeginTargetSelection(CardInstance card)
        {
            if (HasBattleEnded)
                return;

            if (card?.Data == null)
                return;

            if (!CanUseSingleEnemySelection(card.Data))
                return;

            _pendingCard = card;
            _canShowGuideLine = false;
            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;

            SetHighlight(true);

            if (handUIController != null)
            {
                var view = handUIController.GetViewForCard(card);
                if (view != null)
                {
                    _selectedCardRect = view.LayoutRect;
                    _selectedCardGuideStartAnchor = view.GuideStartAnchor != null
                        ? view.GuideStartAnchor
                        : null;
                }
            }

            if (guideLine != null)
                guideLine.Hide();

            Debug.Log($"Selecting target for card: {card.Data.DisplayName}");
        }

        public void CancelTargetSelection()
        {
            if (_pendingCard == null)
                return;

            Debug.Log("Target selection cancelled.");
            ForceCancelTargetSelection();
        }

        public void ForceCancelTargetSelection()
        {
            _pendingCard = null;
            SetHighlight(false);

            if (handUIController != null)
                handUIController.DeselectCurrentCard();

            if (guideLine != null)
                guideLine.Hide();

            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;
            _canShowGuideLine = false;
        }

        public void ConfirmTarget(EnemyBattleUnit target)
        {
            if (HasBattleEnded ||
                _pendingCard == null ||
                battleActionRunner == null ||
                target == null ||
                !target.IsAlive)
            {
                return;
            }

            SetHighlight(false);

            battleActionRunner.TryPlayCard(_pendingCard, target);
            _pendingCard = null;

            if (guideLine != null)
                guideLine.Hide();

            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;
            _canShowGuideLine = false;
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            ForceCancelTargetSelection();
        }

        private void SetHighlight(bool value)
        {
            if (enemyHighlights == null)
                return;

            for (int i = 0; i < enemyHighlights.Length; i++)
            {
                if (enemyHighlights[i] != null)
                    enemyHighlights[i].SetSelectable(value);
            }
        }

        private Transform GetHoveredEnemyAnchor()
        {
            if (EventSystem.current == null)
                return null;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                var go = raycastResults[i].gameObject;
                if (go == null)
                    continue;

                var unit = go.GetComponentInParent<EnemyBattleUnit>();
                if (unit != null && unit.IsAlive)
                {
                    if (unit.UIAnchorTargetGuide != null)
                        return unit.UIAnchorTargetGuide;

                    if (unit.UIAnchorDamage != null)
                        return unit.UIAnchorDamage;

                    if (unit.UIAnchorHP != null)
                        return unit.UIAnchorHP;

                    return unit.transform;
                }
            }

            // TODO: Swap to a dedicated hover-tracking source if introduced later.
            return null;
        }

        private bool IsPointerInsideHandArea()
        {
            if (handContainer == null)
                return false;

            Canvas canvas = handContainer.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            handContainer.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);

            min.x -= handPaddingX;
            min.y -= handPaddingY;
            max.x += handPaddingX;
            max.y += handPaddingY;

            Vector2 mouse = Input.mousePosition;
            return mouse.x >= min.x && mouse.x <= max.x &&
                   mouse.y >= min.y && mouse.y <= max.y;
        }

        private void ShowGuideLineFromCurrentStartSource()
        {
            if (guideLine == null)
                return;

            if (guideStartSource == GuideStartSource.Card)
            {
                RectTransform startAnchor = _selectedCardGuideStartAnchor != null
                    ? _selectedCardGuideStartAnchor
                    : _selectedCardRect;

                if (startAnchor != null)
                    guideLine.ShowFromCard(startAnchor);
            }
            else
            {
                if (player != null)
                {
                    Transform startWorld = player.UIAnchorTargetGuide != null
                        ? player.UIAnchorTargetGuide
                        : player.transform;

                    if (startWorld != null)
                        guideLine.ShowFromWorld(startWorld);
                }
            }
        }
    }
}
```

## FILE: BattleUnit.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Units/BattleUnit.cs`
```csharp
using System;
using UnityEngine;

namespace CardBattle.Core
{
    public abstract class BattleUnit : MonoBehaviour
    {
        [SerializeField] protected int maxHp = 10;
        [SerializeField] protected int currentHp;
        [SerializeField] private StatusController statusController;
        protected int currentBlock;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public int CurrentBlock => currentBlock;
        public bool IsAlive => currentHp > 0;
        public StatusController StatusController => statusController;

        public event Action<int, int> OnHpChangedEvent;
        public event Action<int> OnBlockChangedEvent;

        public event Action<BattleUnit, int> OnDamageTakenEvent;
        public event Action<BattleUnit, int> OnBlockAbsorbedEvent;
        public event Action<BattleUnit, int> OnHealedEvent;
        public event Action<BattleUnit> OnDefeatedEvent;

        protected virtual void Awake()
        {
            if (statusController == null)
                statusController = GetComponent<StatusController>();

            if (statusController == null)
                statusController = gameObject.AddComponent<StatusController>();

            statusController.SetOwner(this);

            if (currentHp <= 0)
                currentHp = maxHp;

            NotifyHpChanged();
        }

        /// <returns>Damage applied to HP after block (not raw incoming amount).</returns>
        public virtual int TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return 0;

            bool wasAliveBeforeDamage = IsAlive;
            int remaining = amount;

            if (currentBlock > 0)
            {
                int absorbed = Mathf.Min(currentBlock, remaining);
                currentBlock -= absorbed;
                remaining -= absorbed;
                NotifyBlockChanged();

                if (absorbed > 0)
                    OnBlockAbsorbedEvent?.Invoke(this, absorbed);
            }

            int hpDamage = 0;
            if (remaining > 0)
            {
                hpDamage = remaining;
                currentHp = Mathf.Max(0, currentHp - hpDamage);
                OnHpChanged();
                NotifyHpChanged();
            }

            OnDamageTakenEvent?.Invoke(this, hpDamage);

            if (wasAliveBeforeDamage && currentHp == 0)
            {
                OnDefeated();
                OnDefeatedEvent?.Invoke(this);
            }

            return hpDamage;
        }

        public virtual void AddBlock(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentBlock += amount;
            NotifyBlockChanged();
        }

        public virtual void ClearBlock()
        {
            if (currentBlock == 0)
                return;

            currentBlock = 0;
            NotifyBlockChanged();
        }

        private void NotifyBlockChanged()
        {
            OnBlockChangedEvent?.Invoke(currentBlock);
        }

        public virtual void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentHp = Mathf.Min(maxHp, currentHp + amount);
            OnHpChanged();
            NotifyHpChanged();
            OnHealedEvent?.Invoke(this, amount);
        }

        public virtual void SetMaxHp(int value, bool refillToMax = false)
        {
            maxHp = Mathf.Max(1, value);

            if (refillToMax)
                currentHp = maxHp;
            else
                currentHp = Mathf.Min(currentHp, maxHp);

            OnHpChanged();
            NotifyHpChanged();
        }

        /// <summary>
        /// Initializes runtime max/current HP from external run data without combat side effects.
        /// </summary>
        public virtual void InitializeVitals(int newMaxHp, int newCurrentHp)
        {
            maxHp = Mathf.Max(1, newMaxHp);
            currentHp = Mathf.Clamp(newCurrentHp, 0, maxHp);
            ClearStatuses();

            OnHpChanged();
            NotifyHpChanged();
        }

        public virtual void ApplyStatus(StatusEffectType type, int amount, StatusDurationType durationType, int duration)
        {
            ApplyStatus(type, amount, durationType, duration, false);
        }

        public virtual void ApplyStatus(
            StatusEffectType type,
            int amount,
            StatusDurationType durationType,
            int duration,
            bool skipNextTurnTick)
        {
            if (!IsAlive)
                return;

            statusController?.AddStatus(type, amount, durationType, duration, skipNextTurnTick);
        }

        public virtual void ClearStatuses()
        {
            statusController?.ClearAllStatuses();
        }

        public virtual void TickStatusTurnDuration()
        {
            statusController?.TickTurnDurationStatuses();
        }

        public virtual void TickStatusOwnerActionDuration()
        {
            statusController?.TickOwnerActionDurationStatuses();
        }

        public virtual int CalculateOutgoingAttackDamage(int baseDamage, bool consumeOnUse = true)
        {
            if (statusController == null)
                return Mathf.Max(0, baseDamage);

            return statusController.ModifyOutgoingAttackDamage(baseDamage, consumeOnUse);
        }

        public virtual int CalculateIncomingAttackDamage(int incomingDamage)
        {
            if (statusController == null)
                return Mathf.Max(0, incomingDamage);

            return statusController.ModifyIncomingAttackDamage(incomingDamage);
        }

        public virtual int TakeAttackDamage(BattleUnit attacker, int baseDamage)
        {
            int outgoingDamage = baseDamage;
            if (attacker != null)
                outgoingDamage = attacker.CalculateOutgoingAttackDamage(baseDamage, true);

            int finalDamage = CalculateIncomingAttackDamage(outgoingDamage);
            return TakeDamage(finalDamage);
        }

        protected virtual void OnHpChanged() { }
        protected virtual void OnDefeated() { }

        private void NotifyHpChanged()
        {
            OnHpChangedEvent?.Invoke(currentHp, maxHp);
        }
    }
}
```

## FILE: EnemyBattleUnit.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Units/EnemyBattleUnit.cs`
```csharp
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
```

## FILE: PlayerBattleUnit.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Units/PlayerBattleUnit.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Player-facing state: AP pool, deck access, and hooks buff cards can influence.
    /// Turn flow: <see cref="BeginRoundState"/> → play cards until out of AP or <see cref="RequestEndTurn"/> → enemies react.
    /// </summary>
    public class PlayerBattleUnit : BattleUnit
    {
        [Header("Turn Rules")]
        [SerializeField] private int apPerRound = 3;
        [SerializeField] private int drawPerRound = 5;

        [Header("Systems")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private BattleUnitView battleUnitView;
        [SerializeField] private Transform uiAnchorTargetGuide;
        public BattleUnitView View => battleUnitView;

        private int _pendingAttackBonus;
        private bool _turnCommitted;
        public event System.Action<int, int> OnApChangedEvent;
        public event System.Action<bool> OnTurnStateChanged;
        public event System.Action<int> OnDebugBuffChanged;

        public int CurrentAp { get; private set; }
        public int ApPerRound => apPerRound;
        public int DrawPerRound => drawPerRound;
        public bool HasCommittedTurn => _turnCommitted;
        public bool CanAct => !_turnCommitted && IsAlive;

        public DeckController DeckController => deckController;
        public Transform UIAnchorTargetGuide => uiAnchorTargetGuide;
        public int DebugBuffValue => _pendingAttackBonus;
        public int DebugBuffCount => _pendingAttackBonus > 0 ? 1 : 0;

        /// <summary>True when the player may attempt to spend AP on a card.</summary>
        public bool CanSpendAp(int amount) => CanAct && CurrentAp >= amount;

        /// <summary>Reset AP, unlock input, and clear transient modifiers at the start of the player's round.</summary>
        public void BeginRoundState()
        {
            _turnCommitted = false;
            CurrentAp = Mathf.Max(0, apPerRound);
            _pendingAttackBonus = 0;
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
            ClearBlock();
            NotifyApChanged();
            NotifyTurnStateChanged();
        }

        /// <summary>
        /// Attempts to play a card: spends AP, moves it to the graveyard, resolves effects, then notifies enemies.
        /// Returns false if the turn is locked, the card is not in hand, or AP is insufficient.
        /// </summary>
        public bool TryPlayCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            if (!CanAct || card?.Data == null)
                return false;

            if (deckController == null || cardResolver == null || enemyActionSystem == null)
            {
                Debug.LogError("PlayerBattleUnit missing one of its serialized systems.");
                return false;
            }

            if (!deckController.IsInHand(card))
                return false;

            var cost = card.Data.ApCost;
            if (!CanSpendAp(cost))
                return false;

            CurrentAp -= cost;
            NotifyApChanged();
            deckController.PlayCardFromHand(card);

            var context = new CardPlayContext(this, card, enemyActionSystem.Enemies, primaryTarget);
            cardResolver.Resolve(context);

            enemyActionSystem.HandlePlayerSuccessfullyPlayedCard();
            return true;
        }

        /// <summary>
        /// Locks further plays, discards the hand, then lets end-of-turn enemies strike.
        /// Countdown enemies that already attacked this round are skipped automatically.
        /// </summary>
        public void RequestEndTurn()
        {
            if (!IsAlive || _turnCommitted)
                return;

            if (deckController == null || enemyActionSystem == null)
            {
                Debug.LogError("PlayerBattleUnit missing deck or enemy system references.");
                return;
            }

            _turnCommitted = true;
            NotifyTurnStateChanged();
            deckController.DiscardEntireHand();
            enemyActionSystem.ResolveEndTurnAttacks();
        }

        /// <summary>Buff cards add to the next attack's damage; consumed when an attack card resolves.</summary>
        public void ApplyBuffFromCard(CardData data)
        {
            if (data == null)
                return;

            _pendingAttackBonus += Mathf.Max(0, data.BuffPotency);
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
        }

        /// <summary>Called by <see cref="CardResolver"/> when applying attack damage.</summary>
        public int ConsumeDamageBonus()
        {
            var bonus = _pendingAttackBonus;
            ConsumeNextAttackBonus();
            return bonus;
        }

        public void ConsumeNextAttackBonus()
        {
            _pendingAttackBonus = 0;
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
        }

        public void SpendApFromRunner(int amount)
        {
            if (amount <= 0)
                return;

            CurrentAp = Mathf.Max(0, CurrentAp - amount);
            NotifyApChanged();
        }

        public void CommitEndTurnFromRunner()
        {
            if (!IsAlive || _turnCommitted)
                return;

            _turnCommitted = true;
            NotifyTurnStateChanged();

            if (deckController != null)
                deckController.DiscardEntireHand();
        }

        /// <summary>Clears transient combat state before a new encounter battle start. Does not change HP.</summary>
        public void ResetBattleRuntimeStateForNewEncounter()
        {
            _turnCommitted = true;
            CurrentAp = 0;
            _pendingAttackBonus = 0;
            ClearBlock();
            ClearStatuses();
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
            NotifyApChanged();
            NotifyTurnStateChanged();
        }

        private void NotifyApChanged()
        {
            OnApChangedEvent?.Invoke(CurrentAp, ApPerRound);
        }

        private void NotifyTurnStateChanged()
        {
            OnTurnStateChanged?.Invoke(CanAct);
        }

        protected override void OnDefeated()
        {
            base.OnDefeated();
            NotifyTurnStateChanged();
        }
    }
}
```

## FILE: BattleUnitView.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Views/BattleUnitView.cs`
```csharp
using System;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DefenseHash = Animator.StringToHash("Defense");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public event Action OnAttackHit;
        public event Action OnAttackPreHit;
        public event Action OnActionFinished;

        public void PlayAttack()
        {
            if (animator == null) return;
            animator.SetTrigger(AttackHash);
        }

        public void PlayHurt()
        {
            if (animator == null) return;
            animator.SetTrigger(HurtHash);
        }

        public void PlayDefense()
        {
            if (animator == null) return;
            animator.SetTrigger(DefenseHash);
        }

        public void PlayDead()
        {
            if (animator == null) return;
            animator.SetTrigger(DeadHash);
        }

        // Animation Event
        public void AnimEvent_AttackHit()
        {
            OnAttackHit?.Invoke();
        }
        
        public void AnimEvent_AttackPreHit()
        {
            OnAttackPreHit?.Invoke();
        }

        // Animation Event
        public void AnimEvent_ActionFinished()
        {
            OnActionFinished?.Invoke();
        }
    }
}
```

## FILE: BattleCameraFeedbackController.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Presentation/BattleCameraFeedbackController.cs`
```csharp
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
```

## FILE: CameraShakeController.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Presentation/CameraShakeController.cs`
```csharp
using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only camera shake utility. Knows only how to shake a transform.
    /// </summary>
    public class CameraShakeController : MonoBehaviour
    {
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private float defaultDuration = 0.12f;
        [SerializeField] private float defaultStrength = 0.06f;
        [SerializeField] private float defaultFrequency = 38f;

        private Vector3 originalLocalPosition;
        private Coroutine shakeRoutine;

        private void Awake()
        {
            if (shakeTarget == null && Camera.main != null)
                shakeTarget = Camera.main.transform;

            CacheOriginalLocalPosition();
        }

        private void OnEnable()
        {
            CacheOriginalLocalPosition();
        }

        private void OnDisable()
        {
            StopShakeRoutine();
            RestoreOriginalLocalPosition();
        }

        public void Shake()
        {
            Shake(defaultDuration, defaultStrength, defaultFrequency);
        }

        public void Shake(float duration, float strength, float frequency)
        {
            if (shakeTarget == null)
                return;

            if (shakeRoutine != null)
            {
                StopShakeRoutine();
                RestoreOriginalLocalPosition();
            }

            CacheOriginalLocalPosition();
            shakeRoutine = StartCoroutine(CoShake(duration, strength, frequency));
        }

        [ContextMenu("Test Shake")]
        private void TestShake()
        {
            Shake();
        }

        private IEnumerator CoShake(float duration, float strength, float frequency)
        {
            float clampedDuration = Mathf.Max(0.01f, duration);
            float clampedStrength = Mathf.Max(0f, strength);
            float clampedFrequency = Mathf.Max(0f, frequency);
            float elapsed = 0f;

            float seedX = Random.value * 1000f;
            float seedY = Random.value * 1000f + 100f;

            while (elapsed < clampedDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / clampedDuration);
                float fade = 1f - t;

                float sampleTime = Time.unscaledTime * clampedFrequency;
                float offsetX = (Mathf.PerlinNoise(seedX, sampleTime) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(seedY, sampleTime) - 0.5f) * 2f;
                Vector3 offset = new Vector3(offsetX, offsetY, 0f) * (clampedStrength * fade);

                shakeTarget.localPosition = originalLocalPosition + offset;
                yield return null;
            }

            RestoreOriginalLocalPosition();
            shakeRoutine = null;
        }

        private void StopShakeRoutine()
        {
            if (shakeRoutine == null)
                return;

            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        private void CacheOriginalLocalPosition()
        {
            if (shakeTarget != null)
                originalLocalPosition = shakeTarget.localPosition;
        }

        private void RestoreOriginalLocalPosition()
        {
            if (shakeTarget != null)
                shakeTarget.localPosition = originalLocalPosition;
        }
    }
}
```

## FILE: BattleEndPresentationController.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Presentation/BattleEndPresentationController.cs`
```csharp
using System;
using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Delays the presentation-ready signal after battle logic has ended.
    /// Does not open UI, change battle state, or stop gameplay coroutines.
    /// </summary>
    public class BattleEndPresentationController : MonoBehaviour
    {
        [SerializeField] private BattleOutcomeController battleOutcomeController;

        [Header("Timing")]
        [SerializeField] private float encounterClearedDelay = 1.0f;
        [SerializeField] private float playerDefeatedDelay = 1.2f;

        private Coroutine presentationRoutine;

        public bool IsPresentationPending { get; private set; }
        public bool IsPresentationReady { get; private set; }
        public BattleOutcome ReadyOutcome { get; private set; } = BattleOutcome.None;

        public event Action<BattleOutcome> OnBattleEndPresentationReady;

        private void OnEnable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            StopPresentationRoutine();
            IsPresentationPending = false;
        }

        public void ResetPresentation()
        {
            StopPresentationRoutine();
            IsPresentationPending = false;
            IsPresentationReady = false;
            ReadyOutcome = BattleOutcome.None;
            Debug.Log("[BattleEndPresentation] Reset.");
        }

        [ContextMenu("Debug Reset Presentation")]
        private void DebugResetPresentation()
        {
            ResetPresentation();
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.None || IsPresentationPending || IsPresentationReady)
                return;

            IsPresentationPending = true;
            presentationRoutine = StartCoroutine(PresentationDelayRoutine(outcome));
        }

        private IEnumerator PresentationDelayRoutine(BattleOutcome outcome)
        {
            float delay = GetDelay(outcome);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            presentationRoutine = null;
            IsPresentationPending = false;
            IsPresentationReady = true;
            ReadyOutcome = outcome;

            Debug.Log($"[BattleEndPresentation] Ready: {outcome}");
            OnBattleEndPresentationReady?.Invoke(outcome);
        }

        private float GetDelay(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.EncounterCleared:
                    return Mathf.Max(0f, encounterClearedDelay);
                case BattleOutcome.PlayerDefeated:
                    return Mathf.Max(0f, playerDefeatedDelay);
                default:
                    return 0f;
            }
        }

        private void StopPresentationRoutine()
        {
            if (presentationRoutine == null)
                return;

            StopCoroutine(presentationRoutine);
            presentationRoutine = null;
        }
    }
}
```

## FILE: BattleOutcome.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Systems/BattleOutcome.cs`
```csharp
namespace CardBattle.Core
{
    public enum BattleOutcome
    {
        None,
        EncounterCleared,
        PlayerDefeated
    }
}
```

## FILE: BattleOutcomeController.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Systems/BattleOutcomeController.cs`
```csharp
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
```

## FILE: StatusController.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Status/StatusController.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    public class StatusController : MonoBehaviour
    {
        [SerializeField] private BattleUnit owner;
        [SerializeField] private float weakDamageMultiplier = 0.75f;
        [SerializeField] private float vulnerableDamageMultiplier = 1.5f;
        [SerializeField] private List<StatusInstance> statuses = new();

        public event Action OnStatusesChanged;

        public void SetOwner(BattleUnit value)
        {
            owner = value;
        }

        public void AddStatus(StatusEffectType type, int amount, StatusDurationType durationType, int duration)
        {
            AddStatus(type, amount, durationType, duration, false);
        }

        public void AddStatus(
            StatusEffectType type,
            int amount,
            StatusDurationType durationType,
            int duration,
            bool skipNextTurnTick)
        {
            if (amount <= 0 && type != StatusEffectType.Weak && type != StatusEffectType.Vulnerable)
                return;

            var existing = FindStatus(type);
            if (existing != null)
            {
                if (amount > 0)
                    existing.AddAmount(amount);

                if (durationType != StatusDurationType.Encounter)
                    existing.SetRemainingDurationToMax(duration);

                existing.SetSkipNextTurnTick(skipNextTurnTick);
            }
            else
            {
                var created = new StatusInstance(type, amount, durationType, duration);
                created.SetSkipNextTurnTick(skipNextTurnTick);
                statuses.Add(created);
            }

            RemoveExpiredStatuses();
            NotifyChanged();
        }

        public void ClearAllStatuses()
        {
            if (statuses.Count == 0)
                return;

            statuses.Clear();
            NotifyChanged();
        }

        public bool HasStatus(StatusEffectType type)
        {
            return GetTotalAmount(type) > 0 || FindStatus(type) != null;
        }

        public int GetTotalAmount(StatusEffectType type)
        {
            int total = 0;
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status.Type == type && !status.IsExpired)
                    total += status.Amount;
            }

            return total;
        }

        public int ModifyOutgoingAttackDamage(int baseDamage, bool consumeOnUse)
        {
            int damage = baseDamage + GetTotalAmount(StatusEffectType.Strength);

            int nextAttackBonus = GetTotalAmount(StatusEffectType.NextAttackBonus);
            if (nextAttackBonus > 0)
            {
                damage += nextAttackBonus;

                if (consumeOnUse)
                {
                    for (int i = statuses.Count - 1; i >= 0; i--)
                    {
                        var status = statuses[i];
                        if (status.Type != StatusEffectType.NextAttackBonus)
                            continue;

                        status.ConsumeUse();
                    }
                }
            }

            if (HasActiveStatus(StatusEffectType.Weak))
                damage = Mathf.FloorToInt(damage * weakDamageMultiplier);

            RemoveExpiredStatuses();
            NotifyChanged();
            return Mathf.Max(0, damage);
        }

        public int ModifyIncomingAttackDamage(int incomingDamage)
        {
            int damage = incomingDamage;

            if (HasActiveStatus(StatusEffectType.Vulnerable))
                damage = Mathf.CeilToInt(damage * vulnerableDamageMultiplier);

            return Mathf.Max(0, damage);
        }

        public void TickTurnDurationStatuses()
        {
            for (int i = 0; i < statuses.Count; i++)
                statuses[i].TickTurn();

            RemoveExpiredStatuses();
            NotifyChanged();
        }

        public void TickOwnerActionDurationStatuses()
        {
            for (int i = 0; i < statuses.Count; i++)
                statuses[i].TickOwnerAction();

            RemoveExpiredStatuses();
            NotifyChanged();
        }

        public string BuildDebugText()
        {
            if (statuses.Count == 0)
                return "(none)";

            return BuildStatusListText();
        }

        public string BuildStatusDisplayText()
        {
            if (statuses.Count == 0)
                return string.Empty;

            return BuildStatusListText();
        }

        private string BuildStatusListText()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < statuses.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(statuses[i].ToShortText());
            }

            return builder.ToString();
        }

        private StatusInstance FindStatus(StatusEffectType type)
        {
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status.Type == type && !status.IsExpired)
                    return status;
            }

            return null;
        }

        private bool HasActiveStatus(StatusEffectType type)
        {
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status.Type == type && !status.IsExpired)
                    return true;
            }

            return false;
        }

        private void RemoveExpiredStatuses()
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i].IsExpired)
                    statuses.RemoveAt(i);
            }
        }

        private void NotifyChanged()
        {
            OnStatusesChanged?.Invoke();
        }
    }
}
```

## FILE: StatusDurationType.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Status/StatusDurationType.cs`
```csharp
namespace CardBattle.Core
{
    public enum StatusDurationType
    {
        Encounter,
        Turn,
        UseCount,
        OwnerAction
    }
}
```

## FILE: StatusEffectType.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Status/StatusEffectType.cs`
```csharp
namespace CardBattle.Core
{
    public enum StatusEffectType
    {
        Strength,
        Weak,
        Vulnerable,
        NextAttackBonus
    }
}
```

## FILE: StatusInstance.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Status/StatusInstance.cs`
```csharp
using System;
using UnityEngine;

namespace CardBattle.Core
{
    [Serializable]
    public class StatusInstance
    {
        [SerializeField] private StatusEffectType type;
        [SerializeField] private int amount;
        [SerializeField] private StatusDurationType durationType;
        [SerializeField] private int remainingDuration;
        [SerializeField] private bool skipNextTurnTick;

        public StatusEffectType Type => type;
        public int Amount => amount;
        public StatusDurationType DurationType => durationType;
        public int RemainingDuration => remainingDuration;
        public bool SkipNextTurnTick => skipNextTurnTick;

        public StatusInstance(StatusEffectType type, int amount, StatusDurationType durationType, int duration)
        {
            this.type = type;
            this.amount = amount;
            this.durationType = durationType;
            remainingDuration = duration;
        }

        public void AddAmount(int value)
        {
            amount += value;
        }

        public void SetRemainingDurationToMax(int value)
        {
            remainingDuration = Mathf.Max(remainingDuration, value);
        }

        public void SetSkipNextTurnTick(bool value)
        {
            if (durationType == StatusDurationType.Turn)
                skipNextTurnTick = value;
        }

        public void MarkJustAppliedForTurnTick()
        {
            SetSkipNextTurnTick(true);
        }

        public void TickTurn()
        {
            if (durationType != StatusDurationType.Turn)
                return;

            if (skipNextTurnTick)
            {
                skipNextTurnTick = false;
                return;
            }

            remainingDuration = Mathf.Max(0, remainingDuration - 1);
        }

        public void TickOwnerAction()
        {
            if (durationType != StatusDurationType.OwnerAction)
                return;

            remainingDuration = Mathf.Max(0, remainingDuration - 1);
        }

        public void ConsumeUse()
        {
            if (durationType != StatusDurationType.UseCount)
                return;

            remainingDuration--;
        }

        public bool IsExpired =>
            durationType == StatusDurationType.Encounter && amount <= 0
            || durationType == StatusDurationType.Turn && (amount <= 0 || remainingDuration <= 0)
            || durationType == StatusDurationType.UseCount && (amount <= 0 || remainingDuration <= 0)
            || durationType == StatusDurationType.OwnerAction && (amount <= 0 || remainingDuration <= 0);

        public string ToShortText()
        {
            return durationType switch
            {
                StatusDurationType.Encounter => $"{type} {amount}",
                StatusDurationType.Turn => skipNextTurnTick
                    ? $"{type} {amount} ({remainingDuration}T*)"
                    : $"{type} {amount} ({remainingDuration}T)",
                StatusDurationType.UseCount => $"{type} {amount} ({remainingDuration} use)",
                StatusDurationType.OwnerAction => $"{type} {amount} ({remainingDuration} action)",
                _ => $"{type} {amount}"
            };
        }
    }
}
```

## FILE: StatusDebugTest.cs
**Path:** `Assets/Scripts/CardBattle/Battle/Status/Debug/StatusDebugTest.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    public class StatusDebugTest : MonoBehaviour
    {
        [SerializeField] private BattleUnit attacker;
        [SerializeField] private BattleUnit defender;
        [SerializeField] private int testDamage = 10;

        [ContextMenu("Add Strength +3 to Attacker")]
        private void AddStrengthToAttacker()
        {
            attacker?.ApplyStatus(StatusEffectType.Strength, 3, StatusDurationType.Encounter, 0);
        }

        [ContextMenu("Add Weak 2 Turns to Attacker")]
        private void AddWeakToAttacker()
        {
            attacker?.ApplyStatus(StatusEffectType.Weak, 1, StatusDurationType.Turn, 2);
        }

        [ContextMenu("Add Weak 1 Turn to Attacker (skip first tick)")]
        private void AddWeakToAttackerWithSkip()
        {
            attacker?.ApplyStatus(StatusEffectType.Weak, 1, StatusDurationType.Turn, 1, skipNextTurnTick: true);
        }

        [ContextMenu("Add NextAttackBonus +5 to Attacker")]
        private void AddNextAttackBonusToAttacker()
        {
            attacker?.ApplyStatus(StatusEffectType.NextAttackBonus, 5, StatusDurationType.UseCount, 1);
        }

        [ContextMenu("Add Vulnerable 2 Turns to Defender")]
        private void AddVulnerableToDefender()
        {
            defender?.ApplyStatus(StatusEffectType.Vulnerable, 1, StatusDurationType.Turn, 2);
        }

        [ContextMenu("Status Test/Attacker Add Strength +2 OwnerAction")]
        private void AddStrengthOwnerAction()
        {
            attacker?.ApplyStatus(StatusEffectType.Strength, 2, StatusDurationType.OwnerAction, 1);
            DebugPrint();
        }

        [ContextMenu("Status Test/Tick Attacker OwnerAction")]
        private void TickAttackerOwnerAction()
        {
            attacker?.TickStatusOwnerActionDuration();
            DebugPrint();
        }

        [ContextMenu("Status Test/Tick Defender OwnerAction")]
        private void TickDefenderOwnerAction()
        {
            defender?.TickStatusOwnerActionDuration();
            DebugPrint();
        }

        [ContextMenu("Status Test/Tick Both OwnerAction")]
        private void TickBothOwnerAction()
        {
            attacker?.TickStatusOwnerActionDuration();
            defender?.TickStatusOwnerActionDuration();
            DebugPrint();
        }

        [ContextMenu("Deal Test Attack Damage")]
        private void DealTestAttackDamage()
        {
            if (defender == null)
                return;

            int finalDamage = defender.TakeAttackDamage(attacker, testDamage);
            Debug.Log($"StatusDebugTest: base={testDamage}, final HP damage={finalDamage}");
        }

        [ContextMenu("Tick Turn Duration")]
        private void TickTurnDuration()
        {
            attacker?.TickStatusTurnDuration();
            defender?.TickStatusTurnDuration();
        }

        [ContextMenu("Clear All")]
        private void ClearAll()
        {
            attacker?.ClearStatuses();
            defender?.ClearStatuses();
        }

        [ContextMenu("Print")]
        private void Print()
        {
            DebugPrint();
        }

        private void DebugPrint()
        {
            string attackerText = attacker?.StatusController != null
                ? attacker.StatusController.BuildDebugText()
                : "(no attacker)";

            string defenderText = defender?.StatusController != null
                ? defender.StatusController.BuildDebugText()
                : "(no defender)";

            Debug.Log($"StatusDebugTest Attacker: {attackerText}");
            Debug.Log($"StatusDebugTest Defender: {defenderText}");
        }
    }
}
```