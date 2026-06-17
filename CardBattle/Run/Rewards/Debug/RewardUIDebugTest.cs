using UnityEngine;

namespace CardBattle.Core
{
    public class RewardUIDebugTest : MonoBehaviour
    {
        [SerializeField] private RewardPanelUI rewardPanelUI;
        [SerializeField] private RewardController rewardController;

        private int continueRequestCount;

        private void OnEnable()
        {
            if (rewardPanelUI == null)
                return;

            rewardPanelUI.OnContinueRequested += HandleContinueRequested;
        }

        private void OnDisable()
        {
            if (rewardPanelUI == null)
                return;

            rewardPanelUI.OnContinueRequested -= HandleContinueRequested;
        }

        private void HandleContinueRequested()
        {
            continueRequestCount++;
            Debug.Log(
                $"[RewardUIDebugTest] OnContinueRequested fired (count={continueRequestCount}).");
        }

        [ContextMenu("Debug Refresh Reward UI")]
        private void DebugRefreshRewardUI()
        {
            if (!TryGetPanel())
                return;

            rewardPanelUI.RefreshFromCurrentSession();
            DebugPrintRewardUIState();
        }

        [ContextMenu("Debug Hide Reward UI")]
        private void DebugHideRewardUI()
        {
            if (!TryGetPanel())
                return;

            rewardPanelUI.HidePanel();
            DebugPrintRewardUIState();
        }

        [ContextMenu("Debug Print Reward UI State")]
        private void DebugPrintRewardUIState()
        {
            if (!TryGetPanel())
                return;

            RewardSession session = rewardController != null
                ? rewardController.CurrentSession
                : null;

            Debug.Log(
                $"[RewardUIDebugTest] --- Reward UI State ---\n" +
                $"IsVisible={rewardPanelUI.IsVisible}\n" +
                $"IsAwaitingChoice={rewardPanelUI.IsAwaitingChoice}\n" +
                $"IsCompletedState={rewardPanelUI.IsCompletedState}\n" +
                $"ContinueRequests={continueRequestCount}\n" +
                $"HasSession={session != null}\n" +
                $"SessionComplete={session != null && session.IsComplete}\n" +
                $"ChoiceCount={session?.ChoiceCount ?? -1}\n" +
                $"GoldAmount={session?.GoldAmount ?? -1}\n" +
                $"WasCardSkipped={session?.WasCardSkipped ?? false}\n" +
                $"SelectedCard={session?.SelectedCard?.CardId ?? "none"}");
        }

        private bool TryGetPanel()
        {
            if (rewardPanelUI != null)
                return true;

            Debug.LogError("[RewardUIDebugTest] RewardPanelUI reference is missing.");
            return false;
        }
    }
}
