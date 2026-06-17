using System;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Validates that an Encounter is fully resolved after Reward Continue,
    /// then raises a single handoff event for future save and scene transition.
    /// </summary>
    public class EncounterCompletionController : MonoBehaviour
    {
        [Header("Flow Sources")]
        [SerializeField] private RewardPanelUI rewardPanelUI;
        [SerializeField] private RewardController rewardController;
        [SerializeField] private BattleRunBridge battleRunBridge;

        [Header("Options")]
        [SerializeField] private bool requireCommittedPlayerHp = true;
        [SerializeField] private bool hideRewardPanelOnSuccess = true;
        [SerializeField] private bool verboseLogs;

        public bool IsCompletionReady { get; private set; }
        public bool HasCompletedEncounterFlow { get; private set; }
        public int CompletionRequestCount { get; private set; }
        public int SuccessfulCompletionCount { get; private set; }

        public event Action OnEncounterCompletionReady;

        private RewardPanelUI subscribedRewardPanelUI;

        private void OnEnable()
        {
            RefreshRewardPanelSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeRewardPanel();
        }

        private void OnDestroy()
        {
            UnsubscribeRewardPanel();
        }

        private void RefreshRewardPanelSubscription()
        {
            UnsubscribeRewardPanel();

            if (rewardPanelUI == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[EncounterCompletion] RewardPanelUI reference is missing. " +
                        "Continue requests cannot be received.");
                }

                return;
            }

            subscribedRewardPanelUI = rewardPanelUI;
            subscribedRewardPanelUI.OnContinueRequested += HandleContinueRequested;
        }

        private void UnsubscribeRewardPanel()
        {
            if (subscribedRewardPanelUI == null)
                return;

            subscribedRewardPanelUI.OnContinueRequested -= HandleContinueRequested;
            subscribedRewardPanelUI = null;
        }

        private void HandleContinueRequested()
        {
            CompletionRequestCount++;

            if (HasCompletedEncounterFlow)
            {
                LogCannotComplete("Encounter flow was already completed.");
                return;
            }

            TryCompleteEncounterFlow();
        }

        public bool CanCompleteEncounterFlow()
        {
            if (HasCompletedEncounterFlow)
            {
                LogCannotComplete("Encounter flow was already completed.");
                return false;
            }

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                LogCannotComplete("RunManager.Instance is null.");
                return false;
            }

            if (!runManager.HasActiveRun)
            {
                LogCannotComplete("no Active Run exists.");
                return false;
            }

            RunState run = runManager.CurrentRun;
            if (run == null)
            {
                LogCannotComplete("CurrentRun is null.");
                return false;
            }

            if (rewardController == null)
            {
                LogCannotComplete("RewardController reference is missing.");
                return false;
            }

            RewardSession session = rewardController.CurrentSession;
            if (session == null)
            {
                LogCannotComplete("Reward Session does not exist.");
                return false;
            }

            if (!session.IsComplete)
            {
                LogCannotComplete("Reward Session is not complete.");
                return false;
            }

            if (!rewardController.IsRewardComplete)
            {
                LogCannotComplete("RewardController reports reward is not complete.");
                return false;
            }

            if (rewardPanelUI == null)
            {
                LogCannotComplete("RewardPanelUI reference is missing.");
                return false;
            }

            if (!rewardPanelUI.IsCompletedState)
            {
                LogCannotComplete("Reward UI is not in completed state.");
                return false;
            }

            if (requireCommittedPlayerHp)
            {
                if (battleRunBridge == null)
                {
                    LogCannotComplete("BattleRunBridge reference is missing.");
                    return false;
                }

                if (!battleRunBridge.HasCommittedEncounterResult)
                {
                    LogCannotComplete("surviving Player HP was not committed.");
                    return false;
                }

                if (battleRunBridge.LastCommittedOutcome != BattleOutcome.EncounterCleared)
                {
                    LogCannotComplete(
                        $"last committed outcome is {battleRunBridge.LastCommittedOutcome}, not EncounterCleared.");
                    return false;
                }

                if (battleRunBridge.LastCommittedCurrentHp <= 0)
                {
                    LogCannotComplete("last committed Player HP is zero or below.");
                    return false;
                }
            }

            if (run.currentHp <= 0)
            {
                LogCannotComplete("Active Run Player HP is zero or below.");
                return false;
            }

            return true;
        }

        public bool TryCompleteEncounterFlow()
        {
            if (HasCompletedEncounterFlow)
            {
                LogCannotComplete("Encounter flow was already completed.");
                return false;
            }

            IsCompletionReady = CanCompleteEncounterFlow();
            if (!IsCompletionReady)
                return false;

            HasCompletedEncounterFlow = true;

            if (hideRewardPanelOnSuccess && rewardPanelUI != null)
                rewardPanelUI.HidePanel();

            SuccessfulCompletionCount++;
            OnEncounterCompletionReady?.Invoke();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[EncounterCompletion] Encounter flow completed. " +
                    $"SuccessfulCompletionCount={SuccessfulCompletionCount}");
            }

            return true;
        }

        public void ResetCompletionState()
        {
            IsCompletionReady = false;
            HasCompletedEncounterFlow = false;

            if (verboseLogs)
                Debug.Log("[EncounterCompletion] Completion state reset.");
        }

        private void LogCannotComplete(string reason)
        {
            if (!verboseLogs)
                return;

            Debug.LogWarning($"[EncounterCompletion] Cannot complete: {reason}");
        }
    }
}
