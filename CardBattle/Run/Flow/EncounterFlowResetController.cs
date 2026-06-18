using System;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Clears completed in-scene Encounter runtime state so the next Battle can start cleanly
    /// in the same test scene without rolling back Run rewards.
    /// </summary>
    public class EncounterFlowResetController : MonoBehaviour
    {
        [Header("Flow State")]
        [SerializeField] private RewardController rewardController;
        [SerializeField] private EncounterCompletionController encounterCompletionController;
        [SerializeField] private BattleOutcomeController battleOutcomeController;
        [SerializeField] private BattleEndPresentationController battleEndPresentationController;
        [SerializeField] private BattleRunBridge battleRunBridge;

        [Header("Encounter Runtime")]
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;
        [SerializeField] private EncounterEnemySceneBinder encounterEnemySceneBinder;

        [Header("Options")]
        [SerializeField] private bool resetRewardState = true;
        [SerializeField] private bool resetCompletionState = true;
        [SerializeField] private bool resetBattleOutcomeState = true;
        [SerializeField] private bool resetBattleEndPresentationState = true;
        [SerializeField] private bool resetBattleRunBridgeCommitState = true;
        [SerializeField] private bool clearAppliedEncounterEnemies = true;
        [SerializeField] private bool clearRuntimeEncounterSelection = false;
        [SerializeField] private bool autoPrepareAfterEncounterCompletionReady = false;
        [SerializeField] private bool verboseLogs;

        public bool HasPreparedNextEncounterState { get; private set; }
        public int PrepareRequestCount { get; private set; }
        public int SuccessfulPrepareCount { get; private set; }
        public string LastPrepareError { get; private set; } = string.Empty;

        public event Action OnNextEncounterStatePrepared;

        private EncounterCompletionController subscribedEncounterCompletionController;

        private void OnEnable()
        {
            RefreshEncounterCompletionSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeEncounterCompletionController();
        }

        private void OnDestroy()
        {
            UnsubscribeEncounterCompletionController();
        }

        private void RefreshEncounterCompletionSubscription()
        {
            UnsubscribeEncounterCompletionController();

            if (encounterCompletionController == null)
                return;

            subscribedEncounterCompletionController = encounterCompletionController;
            subscribedEncounterCompletionController.OnEncounterCompletionReady +=
                HandleEncounterCompletionReady;
        }

        private void UnsubscribeEncounterCompletionController()
        {
            if (subscribedEncounterCompletionController == null)
                return;

            subscribedEncounterCompletionController.OnEncounterCompletionReady -=
                HandleEncounterCompletionReady;
            subscribedEncounterCompletionController = null;
        }

        private void HandleEncounterCompletionReady()
        {
            if (!autoPrepareAfterEncounterCompletionReady)
            {
                if (verboseLogs)
                {
                    Debug.Log(
                        "[EncounterFlowReset] Encounter completion ready received. Auto prepare is disabled.");
                }

                return;
            }

            TryPrepareNextEncounterState();
        }

        public bool TryPrepareNextEncounterState()
        {
            PrepareRequestCount++;
            HasPreparedNextEncounterState = false;
            LastPrepareError = string.Empty;

            if (!ValidateRequiredReferences(out string validationError))
            {
                LastPrepareError = validationError;
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[EncounterFlowReset] Cannot prepare next encounter: {validationError}");
                }

                return false;
            }

            if (resetRewardState)
                rewardController.ResetRewardState();

            if (resetCompletionState)
                encounterCompletionController.ResetCompletionState();

            if (resetBattleOutcomeState)
                battleOutcomeController.ResetOutcome();

            if (resetBattleEndPresentationState)
                battleEndPresentationController.ResetPresentation();

            if (resetBattleRunBridgeCommitState)
                battleRunBridge.ResetEncounterCommitState();

            if (clearAppliedEncounterEnemies)
                encounterEnemySceneBinder.ClearAppliedEncounterEnemies();

            if (clearRuntimeEncounterSelection)
                runtimeEncounterContext.ClearCurrentEncounter();

            HasPreparedNextEncounterState = true;
            SuccessfulPrepareCount++;

            if (verboseLogs)
                Debug.Log("[EncounterFlowReset] Next encounter state prepared.");

            OnNextEncounterStatePrepared?.Invoke();
            return true;
        }

        private bool ValidateRequiredReferences(out string error)
        {
            error = string.Empty;

            if (resetRewardState && rewardController == null)
            {
                error = "RewardController reference is missing.";
                return false;
            }

            if (resetCompletionState && encounterCompletionController == null)
            {
                error = "EncounterCompletionController reference is missing.";
                return false;
            }

            if (resetBattleOutcomeState && battleOutcomeController == null)
            {
                error = "BattleOutcomeController reference is missing.";
                return false;
            }

            if (resetBattleEndPresentationState && battleEndPresentationController == null)
            {
                error = "BattleEndPresentationController reference is missing.";
                return false;
            }

            if (resetBattleRunBridgeCommitState && battleRunBridge == null)
            {
                error = "BattleRunBridge reference is missing.";
                return false;
            }

            if (clearAppliedEncounterEnemies && encounterEnemySceneBinder == null)
            {
                error = "EncounterEnemySceneBinder reference is missing.";
                return false;
            }

            if (clearRuntimeEncounterSelection && runtimeEncounterContext == null)
            {
                error = "RuntimeEncounterContext reference is missing.";
                return false;
            }

            return true;
        }
    }
}
