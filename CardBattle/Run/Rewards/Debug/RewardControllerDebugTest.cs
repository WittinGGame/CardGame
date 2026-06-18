using UnityEngine;

namespace CardBattle.Core
{
    public class RewardControllerDebugTest : MonoBehaviour
    {
        [SerializeField] private RewardController rewardController;

        private int completionEventCount;

        private void OnEnable()
        {
            if (rewardController == null)
                return;

            rewardController.OnRewardSessionCompleted += HandleRewardSessionCompleted;
        }

        private void OnDisable()
        {
            if (rewardController == null)
                return;

            rewardController.OnRewardSessionCompleted -= HandleRewardSessionCompleted;
        }

        private void HandleRewardSessionCompleted(RewardSession session)
        {
            completionEventCount++;
            Debug.Log(
                $"[RewardControllerDebugTest] OnRewardSessionCompleted fired " +
                $"(count={completionEventCount}) | Complete={session.IsComplete}");
        }

        [ContextMenu("Debug Begin Reward")]
        private void DebugBeginReward()
        {
            if (!TryGetController())
                return;

            bool started = rewardController.TryBeginReward();
            Debug.Log($"[RewardControllerDebugTest] TryBeginReward => {started}");
            DebugPrintReward();
        }

        [ContextMenu("Debug Choose Card 0")]
        private void DebugChooseCard0()
        {
            DebugChooseCard(0);
        }

        [ContextMenu("Debug Choose Card 1")]
        private void DebugChooseCard1()
        {
            DebugChooseCard(1);
        }

        [ContextMenu("Debug Choose Card 2")]
        private void DebugChooseCard2()
        {
            DebugChooseCard(2);
        }

        private void DebugChooseCard(int index)
        {
            if (!TryGetController())
                return;

            bool chosen = rewardController.TryChooseCard(index);
            Debug.Log($"[RewardControllerDebugTest] TryChooseCard({index}) => {chosen}");
            DebugPrintReward();
        }

        [ContextMenu("Debug Skip Card")]
        private void DebugSkipCard()
        {
            if (!TryGetController())
                return;

            bool skipped = rewardController.TrySkipCard();
            Debug.Log($"[RewardControllerDebugTest] TrySkipCard => {skipped}");
            DebugPrintReward();
        }

        [ContextMenu("Debug Print Reward")]
        private void DebugPrintReward()
        {
            if (!TryGetController())
                return;

            RewardSession session = rewardController.CurrentSession;
            RunManager runManager = RunManager.Instance;

            Debug.Log(
                $"[RewardControllerDebugTest] --- Reward State ---\n" +
                $"HasActiveReward={rewardController.HasActiveReward}\n" +
                $"IsRewardComplete={rewardController.IsRewardComplete}\n" +
                $"SessionCreateCount={rewardController.SessionCreateCount}\n" +
                $"GoldGrantCount={rewardController.GoldGrantCount}\n" +
                $"CardGrantCount={rewardController.CardGrantCount}\n" +
                $"CompletionEvents={completionEventCount}\n" +
                $"RunGold={(runManager != null && runManager.HasActiveRun ? runManager.CurrentRun.gold : -1)}\n" +
                $"RunDeckCount={(runManager != null && runManager.HasActiveRun ? runManager.CurrentRun.currentDeck.Count : -1)}");

            if (session == null)
            {
                Debug.Log("[RewardControllerDebugTest] CurrentSession is null.");
                return;
            }

            Debug.Log(
                $"[RewardControllerDebugTest] Session | " +
                $"GoldAmount={session.GoldAmount} | GoldGranted={session.GoldGranted} | " +
                $"CardChoiceResolved={session.CardChoiceResolved} | WasCardSkipped={session.WasCardSkipped} | " +
                $"IsComplete={session.IsComplete} | ChoiceCount={session.ChoiceCount}");

            for (int i = 0; i < session.ChoiceCount; i++)
            {
                CardData card = session.CardChoices[i];
                if (card == null)
                {
                    Debug.Log($"[RewardControllerDebugTest] Choice {i}: null");
                    continue;
                }

                Debug.Log(
                    $"[RewardControllerDebugTest] Choice {i}: " +
                    $"CardId={card.CardId} | DisplayName={card.DisplayName}");
            }

            if (session.SelectedCard != null)
            {
                Debug.Log(
                    $"[RewardControllerDebugTest] Selected: " +
                    $"CardId={session.SelectedCard.CardId} | DisplayName={session.SelectedCard.DisplayName}");
            }
            else
            {
                Debug.Log("[RewardControllerDebugTest] Selected: none");
            }
        }

        [ContextMenu("Debug Reset Reward State")]
        private void DebugResetRewardState()
        {
            if (!TryGetController())
                return;

            rewardController.ResetRewardState();
            Debug.Log("[RewardControllerDebugTest] ResetRewardState called.");
            DebugPrintReward();
        }

        [ContextMenu("Debug Resolve Reward Config")]
        private void DebugResolveRewardConfig()
        {
            if (!TryGetController())
                return;

            bool resolved = rewardController.TryGetCurrentResolvedRewardConfig(
                out EncounterRewardConfig config);
            Debug.Log(
                $"[RewardControllerDebugTest] TryGetCurrentResolvedRewardConfig => {resolved} | " +
                $"Config={(config != null ? config.name : "null")}");
            DebugPrintRewardConfigSource();
        }

        [ContextMenu("Debug Print Reward Config Source")]
        private void DebugPrintRewardConfigSource()
        {
            if (!TryGetController())
                return;

            RewardSession session = rewardController.CurrentSession;

            Debug.Log(
                $"[RewardControllerDebugTest] --- Reward Config Source ---\n" +
                $"LastResolvedRewardConfig=" +
                $"{(rewardController.LastResolvedRewardConfig != null ? rewardController.LastResolvedRewardConfig.name : "null")}\n" +
                $"LastResolvedRewardSource={rewardController.LastResolvedRewardSource}\n" +
                $"LastUsedRuntimeEncounterRewardConfig={rewardController.LastUsedRuntimeEncounterRewardConfig}\n" +
                $"LastRewardConfigResolveError={rewardController.LastRewardConfigResolveError}\n" +
                $"HasCurrentSession={session != null}\n" +
                $"SessionComplete={session != null && session.IsComplete}");
        }

        private bool TryGetController()
        {
            if (rewardController != null)
                return true;

            Debug.LogError("[RewardControllerDebugTest] RewardController reference is missing.");
            return false;
        }
    }
}
