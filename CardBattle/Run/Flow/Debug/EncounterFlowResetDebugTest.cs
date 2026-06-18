using UnityEngine;

namespace CardBattle.Core
{
    public class EncounterFlowResetDebugTest : MonoBehaviour
    {
        [SerializeField] private EncounterFlowResetController resetController;
        [SerializeField] private BattleTestBootstrap battleTestBootstrap;
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;
        [SerializeField] private EncounterEnemySceneBinder encounterEnemySceneBinder;
        [SerializeField] private RewardController rewardController;
        [SerializeField] private EncounterCompletionController encounterCompletionController;
        [SerializeField] private BattleOutcomeController battleOutcomeController;
        [SerializeField] private BattleEndPresentationController battleEndPresentationController;
        [SerializeField] private BattleRunBridge battleRunBridge;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [ContextMenu("Debug Prepare Next Encounter State")]
        private void DebugPrepareNextEncounterState()
        {
            if (!TryGetResetController())
                return;

            bool prepared = resetController.TryPrepareNextEncounterState();
            Debug.Log($"[EncounterFlowResetDebugTest] TryPrepareNextEncounterState => {prepared}");
            DebugPrintState();
        }

        [ContextMenu("Debug Print Encounter Flow Reset State")]
        private void DebugPrintState()
        {
            if (!TryGetResetController())
                return;

            RewardSession session = rewardController != null
                ? rewardController.CurrentSession
                : null;

            int enemyCount = enemyActionSystem != null
                ? enemyActionSystem.Enemies.Count
                : -1;

            Debug.Log(
                $"[EncounterFlowResetDebugTest] --- Encounter Flow Reset State ---\n" +
                $"HasPreparedNextEncounterState={resetController.HasPreparedNextEncounterState}\n" +
                $"PrepareRequestCount={resetController.PrepareRequestCount}\n" +
                $"SuccessfulPrepareCount={resetController.SuccessfulPrepareCount}\n" +
                $"LastPrepareError={resetController.LastPrepareError}\n" +
                $"CurrentEncounterId={(runtimeEncounterContext != null ? runtimeEncounterContext.CurrentEncounterId : "n/a")}\n" +
                $"HasCurrentEncounter={runtimeEncounterContext != null && runtimeEncounterContext.HasCurrentEncounter}\n" +
                $"HasRewardSession={session != null}\n" +
                $"RewardComplete={rewardController != null && rewardController.IsRewardComplete}\n" +
                $"HasCompletedEncounterFlow={encounterCompletionController != null && encounterCompletionController.HasCompletedEncounterFlow}\n" +
                $"BattleOutcome={(battleOutcomeController != null ? battleOutcomeController.CurrentOutcome.ToString() : "n/a")}\n" +
                $"BattleEnded={battleOutcomeController != null && battleOutcomeController.IsBattleEnded}\n" +
                $"PresentationReady={battleEndPresentationController != null && battleEndPresentationController.IsPresentationReady}\n" +
                $"BridgeCommitted={battleRunBridge != null && battleRunBridge.HasCommittedEncounterResult}\n" +
                $"BinderApplied={encounterEnemySceneBinder != null && encounterEnemySceneBinder.HasAppliedEncounterEnemies}\n" +
                $"EnemyActionSystemCount={enemyCount}");
        }

        [ContextMenu("Debug Prepare Then Start Battle")]
        private void DebugPrepareThenStartBattle()
        {
            if (!TryGetResetController())
                return;

            if (battleTestBootstrap == null)
            {
                Debug.LogError(
                    "[EncounterFlowResetDebugTest] BattleTestBootstrap reference is missing.");
                return;
            }

            bool prepared = resetController.TryPrepareNextEncounterState();
            Debug.Log($"[EncounterFlowResetDebugTest] TryPrepareNextEncounterState => {prepared}");

            if (!prepared)
            {
                DebugPrintState();
                return;
            }

            battleTestBootstrap.StartTestBattle();
            DebugPrintState();
        }

        private bool TryGetResetController()
        {
            if (resetController != null)
                return true;

            Debug.LogError(
                "[EncounterFlowResetDebugTest] EncounterFlowResetController reference is missing.");
            return false;
        }
    }
}
