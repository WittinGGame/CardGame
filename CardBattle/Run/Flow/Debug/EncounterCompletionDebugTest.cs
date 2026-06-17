using UnityEngine;

namespace CardBattle.Core
{
    public class EncounterCompletionDebugTest : MonoBehaviour
    {
        [SerializeField] private EncounterCompletionController encounterCompletionController;
        [SerializeField] private RewardController rewardController;
        [SerializeField] private BattleRunBridge battleRunBridge;

        private int readyEventCount;

        private void OnEnable()
        {
            if (encounterCompletionController == null)
                return;

            encounterCompletionController.OnEncounterCompletionReady += HandleEncounterCompletionReady;
        }

        private void OnDisable()
        {
            if (encounterCompletionController == null)
                return;

            encounterCompletionController.OnEncounterCompletionReady -= HandleEncounterCompletionReady;
        }

        private void HandleEncounterCompletionReady()
        {
            readyEventCount++;
            Debug.Log(
                $"[EncounterCompletionDebugTest] OnEncounterCompletionReady fired " +
                $"(count={readyEventCount}).");
        }

        [ContextMenu("Debug Try Complete Encounter Flow")]
        private void DebugTryComplete()
        {
            if (!TryGetController())
                return;

            bool completed = encounterCompletionController.TryCompleteEncounterFlow();
            Debug.Log($"[EncounterCompletionDebugTest] TryCompleteEncounterFlow => {completed}");
            DebugPrintState();
        }

        [ContextMenu("Debug Print Encounter Completion State")]
        private void DebugPrintState()
        {
            if (!TryGetController())
                return;

            RunManager runManager = RunManager.Instance;
            bool hasActiveRun = runManager != null && runManager.HasActiveRun;
            RunState run = hasActiveRun ? runManager.CurrentRun : null;
            RewardSession session = rewardController != null
                ? rewardController.CurrentSession
                : null;

            Debug.Log(
                $"[EncounterCompletionDebugTest] --- Encounter Completion State ---\n" +
                $"IsCompletionReady={encounterCompletionController.IsCompletionReady}\n" +
                $"HasCompletedEncounterFlow={encounterCompletionController.HasCompletedEncounterFlow}\n" +
                $"CompletionRequestCount={encounterCompletionController.CompletionRequestCount}\n" +
                $"SuccessfulCompletionCount={encounterCompletionController.SuccessfulCompletionCount}\n" +
                $"ReadyEventCount={readyEventCount}\n" +
                $"HasActiveRun={hasActiveRun}\n" +
                $"RunHp={(run != null ? run.currentHp.ToString() : "n/a")}\n" +
                $"HasRewardSession={session != null}\n" +
                $"RewardSessionComplete={session != null && session.IsComplete}\n" +
                $"RewardControllerComplete={rewardController != null && rewardController.IsRewardComplete}\n" +
                $"BridgeCommitted={battleRunBridge != null && battleRunBridge.HasCommittedEncounterResult}\n" +
                $"BridgeOutcome={(battleRunBridge != null ? battleRunBridge.LastCommittedOutcome.ToString() : "n/a")}\n" +
                $"BridgeCommittedHp={(battleRunBridge != null ? battleRunBridge.LastCommittedCurrentHp.ToString() : "n/a")}");
        }

        [ContextMenu("Debug Reset Encounter Completion State")]
        private void DebugResetState()
        {
            if (!TryGetController())
                return;

            encounterCompletionController.ResetCompletionState();
            Debug.Log("[EncounterCompletionDebugTest] ResetCompletionState called.");
            DebugPrintState();
        }

        private bool TryGetController()
        {
            if (encounterCompletionController != null)
                return true;

            Debug.LogError(
                "[EncounterCompletionDebugTest] EncounterCompletionController reference is missing.");
            return false;
        }
    }
}
