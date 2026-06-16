using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Generates and resolves one reward session per encounter clear.
    /// Listens to battle-end presentation independently of BattleRunBridge.
    /// </summary>
    public class RewardController : MonoBehaviour
    {
        [Header("Reward Source")]
        [SerializeField] private EncounterRewardConfig rewardConfig;

        [Header("Battle Result")]
        [SerializeField] private BattleEndPresentationController battleEndPresentationController;

        [Header("Options")]
        [SerializeField] private int rewardSeedOffset;
        [SerializeField] private bool verboseLogs;

        public RewardSession CurrentSession { get; private set; }

        public bool HasActiveReward =>
            CurrentSession != null &&
            !CurrentSession.IsComplete;

        public bool IsRewardComplete =>
            CurrentSession != null &&
            CurrentSession.IsComplete;

        public int SessionCreateCount { get; private set; }
        public int GoldGrantCount { get; private set; }
        public int CardGrantCount { get; private set; }

        public event Action<RewardSession> OnRewardSessionStarted;
        public event Action<int> OnRewardGoldGranted;
        public event Action<CardData> OnRewardCardSelected;
        public event Action OnRewardCardSkipped;
        public event Action<RewardSession> OnRewardSessionCompleted;

        private BattleEndPresentationController subscribedPresentationController;
        private bool rewardCreatedForCurrentEncounter;
        private bool completionEventRaised;

        private void OnEnable()
        {
            RefreshPresentationSubscription();
        }

        private void OnDisable()
        {
            UnsubscribePresentationController();
        }

        private void OnDestroy()
        {
            UnsubscribePresentationController();
        }

        private void RefreshPresentationSubscription()
        {
            UnsubscribePresentationController();

            if (battleEndPresentationController == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardController] BattleEndPresentationController reference is missing. " +
                        "Automatic reward creation is disabled.");
                }

                return;
            }

            subscribedPresentationController = battleEndPresentationController;
            subscribedPresentationController.OnBattleEndPresentationReady +=
                HandleBattleEndPresentationReady;
        }

        private void UnsubscribePresentationController()
        {
            if (subscribedPresentationController == null)
                return;

            subscribedPresentationController.OnBattleEndPresentationReady -=
                HandleBattleEndPresentationReady;
            subscribedPresentationController = null;
        }

        private void HandleBattleEndPresentationReady(BattleOutcome outcome)
        {
            if (outcome != BattleOutcome.EncounterCleared)
                return;

            TryBeginReward();
        }

        public bool TryBeginReward()
        {
            if (CurrentSession != null && !CurrentSession.IsComplete)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardController] TryBeginReward rejected: an active reward session already exists.");
                }

                return false;
            }

            if (rewardCreatedForCurrentEncounter)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardController] TryBeginReward rejected: reward already created for this encounter.");
                }

                return false;
            }

            if (rewardConfig == null)
            {
                Debug.LogError(
                    "[RewardController] EncounterRewardConfig reference is missing. Reward session not created.");
                return false;
            }

            int goldAmount = rewardConfig.GoldReward;
            int requestedCardChoices = rewardConfig.CardChoiceCount;

            if (goldAmount <= 0 && requestedCardChoices <= 0)
            {
                Debug.LogError(
                    "[RewardController] Reward config has no gold and no card choices. Reward session not created.");
                return false;
            }

            if (requestedCardChoices > 0 && rewardConfig.CardRewardPool == null)
            {
                Debug.LogError(
                    "[RewardController] Card choices are requested but CardRewardPool is missing. " +
                    "Reward session not created.");
                return false;
            }

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardController] RunManager.Instance is null. Reward session not created.");
                }

                return false;
            }

            if (!runManager.HasActiveRun)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardController] No active run exists. Reward session not created.");
                }

                return false;
            }

            RunState run = runManager.CurrentRun;
            if (run == null)
            {
                Debug.LogError(
                    "[RewardController] CurrentRun is null. Reward session not created.");
                return false;
            }

            var cardChoices = new List<CardData>();
            if (requestedCardChoices > 0)
            {
                System.Random random = CreateRewardRandom(run);
                int generatedCount = rewardConfig.CardRewardPool.BuildUniqueChoices(
                    requestedCardChoices,
                    random,
                    cardChoices);

                if (generatedCount == 0 && verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardController] Card reward pool produced zero valid choices. " +
                        "Gold will still be granted; player may Skip.");
                }
            }

            RewardSession session = new RewardSession(goldAmount, cardChoices);
            CurrentSession = session;
            rewardCreatedForCurrentEncounter = true;
            SessionCreateCount++;

            OnRewardSessionStarted?.Invoke(CurrentSession);

            if (!TryGrantGoldForCurrentSession())
            {
                Debug.LogError(
                    "[RewardController] Gold grant failed after reward session was created. " +
                    "Use TryGrantPendingGold to retry.");
                return false;
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RewardController] Reward session started. " +
                    $"Gold={session.GoldAmount} | Choices={session.ChoiceCount} | " +
                    $"SessionCreateCount={SessionCreateCount}");
            }

            return true;
        }

        public bool TryGrantPendingGold()
        {
            return TryGrantGoldForCurrentSession();
        }

        private bool TryGrantGoldForCurrentSession()
        {
            if (CurrentSession == null)
                return false;

            if (CurrentSession.GoldGranted)
                return false;

            if (CurrentSession.GoldAmount <= 0)
            {
                CurrentSession.GoldGranted = true;
                return true;
            }

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                Debug.LogError(
                    "[RewardController] RunManager.Instance is null. Gold grant aborted.");
                return false;
            }

            if (!runManager.AddGold(CurrentSession.GoldAmount))
            {
                Debug.LogError(
                    $"[RewardController] RunManager.AddGold({CurrentSession.GoldAmount}) failed. " +
                    "Gold grant aborted.");
                return false;
            }

            CurrentSession.GoldGranted = true;
            GoldGrantCount++;
            OnRewardGoldGranted?.Invoke(CurrentSession.GoldAmount);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RewardController] Gold granted: {CurrentSession.GoldAmount} | " +
                    $"GoldGrantCount={GoldGrantCount}");
            }

            return true;
        }

        public bool TryChooseCard(int choiceIndex)
        {
            if (CurrentSession == null)
                return false;

            if (CurrentSession.CardChoiceResolved)
                return false;

            if (!CurrentSession.GoldGranted)
                return false;

            if (choiceIndex < 0 || choiceIndex >= CurrentSession.ChoiceCount)
                return false;

            CardData selected = CurrentSession.CardChoices[choiceIndex];
            if (selected == null)
                return false;

            string cardId = selected.CardId;
            if (string.IsNullOrWhiteSpace(cardId))
                return false;

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                Debug.LogError(
                    "[RewardController] RunManager.Instance is null. Card selection aborted.");
                return false;
            }

            if (!runManager.HasActiveRun)
            {
                Debug.LogError(
                    "[RewardController] No active run exists. Card selection aborted.");
                return false;
            }

            if (!runManager.AddCard(cardId, 0))
            {
                Debug.LogError(
                    $"[RewardController] RunManager.AddCard('{cardId}') failed. Card selection aborted.");
                return false;
            }

            CurrentSession.SelectedCard = selected;
            CurrentSession.WasCardSkipped = false;
            CurrentSession.CardChoiceResolved = true;
            CardGrantCount++;

            OnRewardCardSelected?.Invoke(selected);
            NotifyCompletionIfReady();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RewardController] Card selected: {cardId} ({selected.DisplayName}) | " +
                    $"CardGrantCount={CardGrantCount}");
            }

            return true;
        }

        public bool TrySkipCard()
        {
            if (CurrentSession == null)
                return false;

            if (CurrentSession.CardChoiceResolved)
                return false;

            if (!CurrentSession.GoldGranted)
                return false;

            CurrentSession.SelectedCard = null;
            CurrentSession.WasCardSkipped = true;
            CurrentSession.CardChoiceResolved = true;

            OnRewardCardSkipped?.Invoke();
            NotifyCompletionIfReady();

            if (verboseLogs)
                Debug.Log("[RewardController] Card reward skipped.");

            return true;
        }

        public void ResetRewardState()
        {
            CurrentSession = null;
            rewardCreatedForCurrentEncounter = false;
            completionEventRaised = false;

            if (verboseLogs)
                Debug.Log("[RewardController] Reward state reset.");
        }

        private void NotifyCompletionIfReady()
        {
            if (completionEventRaised)
                return;

            if (CurrentSession == null)
                return;

            if (!CurrentSession.IsComplete)
                return;

            completionEventRaised = true;
            OnRewardSessionCompleted?.Invoke(CurrentSession);

            if (verboseLogs)
                Debug.Log("[RewardController] Reward session completed.");
        }

        // EncounterData will later supply encounter/node-specific seed input.
        private System.Random CreateRewardRandom(RunState run)
        {
            unchecked
            {
                int seed = run.runSeed;
                seed = seed * 31 + GetStableOrdinalHash(run.runId);
                seed = seed * 31 + rewardSeedOffset;
                seed = seed * 31 + SessionCreateCount;
                return new System.Random(seed);
            }
        }

        private static int GetStableOrdinalHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash;
            }
        }
    }
}
