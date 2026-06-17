## FILE: RunCardRecord.cs
**Path:** `Assets/Scripts/Run/Data/RunCardRecord.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class RunCardRecord
    {
        public string cardId;
        public int upgradeLevel;

        public RunCardRecord()
        {
        }

        public RunCardRecord(string cardId, int upgradeLevel = 0)
        {
            this.cardId = cardId ?? string.Empty;
            this.upgradeLevel = Mathf.Max(0, upgradeLevel);
        }

        public void SetUpgradeLevel(int value)
        {
            upgradeLevel = Mathf.Max(0, value);
        }

        public RunCardRecord Clone()
        {
            return new RunCardRecord(cardId, upgradeLevel);
        }
    }
}
```

## FILE: RunState.cs
**Path:** `Assets/Scripts/Run/Data/RunState.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class RunState
    {
        public string runId;
        public int runSeed;
        public bool isActive;

        public string playerClassId;

        public int currentHp;
        public int maxHp;
        public int gold;

        public List<RunCardRecord> currentDeck;

        public RunState()
        {
            currentDeck = new List<RunCardRecord>();
        }

        public void InitializeNewRun(
            string newRunId,
            int seed,
            string classId,
            int startingMaxHp,
            IEnumerable<RunCardRecord> starterDeck)
        {
            runId = newRunId ?? string.Empty;
            runSeed = seed;
            isActive = true;
            playerClassId = classId ?? string.Empty;

            maxHp = Mathf.Max(1, startingMaxHp);
            currentHp = maxHp;
            gold = 0;

            EnsureDeckInitialized();
            currentDeck.Clear();

            if (starterDeck == null)
                return;

            foreach (var record in starterDeck)
            {
                if (record == null)
                    continue;

                currentDeck.Add(record.Clone());
            }
        }

        public void SetCurrentHp(int value)
        {
            currentHp = Mathf.Clamp(value, 0, maxHp);
        }

        public void SetMaxHp(int value, bool refillToMax = false)
        {
            maxHp = Mathf.Max(1, value);

            if (refillToMax)
                currentHp = maxHp;
            else
                currentHp = Mathf.Min(currentHp, maxHp);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            gold = Mathf.Max(0, gold + amount);
        }

        public bool SpendGold(int amount)
        {
            if (amount < 0)
                return false;

            if (amount == 0)
                return true;

            if (gold < amount)
                return false;

            gold -= amount;
            return true;
        }

        public void AddCard(string cardId, int upgradeLevel = 0)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return;

            EnsureDeckInitialized();
            currentDeck.Add(new RunCardRecord(cardId, upgradeLevel));
        }

        public void AddCard(RunCardRecord record)
        {
            if (record == null)
                return;

            EnsureDeckInitialized();
            currentDeck.Add(record.Clone());
        }

        public bool RemoveCardAt(int index)
        {
            EnsureDeckInitialized();

            if (index < 0 || index >= currentDeck.Count)
                return false;

            currentDeck.RemoveAt(index);
            return true;
        }

        public void ClearRun()
        {
            runId = string.Empty;
            runSeed = 0;
            isActive = false;
            playerClassId = string.Empty;
            currentHp = 0;
            maxHp = 0;
            gold = 0;

            EnsureDeckInitialized();
            currentDeck.Clear();
        }

        public RunState Clone()
        {
            var copy = new RunState
            {
                runId = runId,
                runSeed = runSeed,
                isActive = isActive,
                playerClassId = playerClassId,
                currentHp = currentHp,
                maxHp = maxHp,
                gold = gold,
                currentDeck = new List<RunCardRecord>()
            };

            EnsureDeckInitialized();

            for (int i = 0; i < currentDeck.Count; i++)
            {
                var record = currentDeck[i];
                if (record == null)
                    continue;

                copy.currentDeck.Add(record.Clone());
            }

            return copy;
        }

        private void EnsureDeckInitialized()
        {
            if (currentDeck == null)
                currentDeck = new List<RunCardRecord>();
        }
    }
}
```

## FILE: RunManager.cs
**Path:** `Assets/Scripts/Run/Systems/RunManager.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        public RunState CurrentRun { get; private set; }

        public bool HasActiveRun =>
            CurrentRun != null &&
            CurrentRun.isActive;

        public bool IsPrimaryInstance => Instance == this;

        public event Action<RunState> OnRunStarted;
        public event Action<RunState> OnRunChanged;
        public event Action OnRunCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"Duplicate RunManager destroyed on {gameObject.scene.name}.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (CurrentRun == null)
                CurrentRun = new RunState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool StartNewRun(
            string runId,
            int seed,
            string playerClassId,
            int startingMaxHp,
            IEnumerable<RunCardRecord> starterDeck)
        {
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(playerClassId))
                return false;

            var run = new RunState();
            run.InitializeNewRun(runId, seed, playerClassId, startingMaxHp, starterDeck);
            CurrentRun = run;

            OnRunStarted?.Invoke(CurrentRun);
            NotifyRunChanged();
            return true;
        }

        public bool SetCurrentHp(int value)
        {
            if (!HasActiveRun)
                return false;

            int previousHp = CurrentRun.currentHp;
            CurrentRun.SetCurrentHp(value);

            if (CurrentRun.currentHp != previousHp)
                NotifyRunChanged();

            return true;
        }

        public bool SetMaxHp(int value, bool refillToMax = false)
        {
            if (!HasActiveRun)
                return false;

            int previousMaxHp = CurrentRun.maxHp;
            int previousCurrentHp = CurrentRun.currentHp;
            CurrentRun.SetMaxHp(value, refillToMax);

            if (CurrentRun.maxHp != previousMaxHp || CurrentRun.currentHp != previousCurrentHp)
                NotifyRunChanged();

            return true;
        }

        public bool AddGold(int amount)
        {
            if (!HasActiveRun || amount <= 0)
                return false;

            CurrentRun.AddGold(amount);
            NotifyRunChanged();
            return true;
        }

        public bool SpendGold(int amount)
        {
            if (!HasActiveRun)
                return false;

            if (amount == 0)
                return true;

            if (!CurrentRun.SpendGold(amount))
                return false;

            NotifyRunChanged();
            return true;
        }

        public bool AddCard(string cardId, int upgradeLevel = 0)
        {
            if (!HasActiveRun || string.IsNullOrWhiteSpace(cardId))
                return false;

            CurrentRun.AddCard(cardId, upgradeLevel);
            NotifyRunChanged();
            return true;
        }

        public bool AddCard(RunCardRecord record)
        {
            if (!HasActiveRun || record == null || string.IsNullOrWhiteSpace(record.cardId))
                return false;

            CurrentRun.AddCard(record);
            NotifyRunChanged();
            return true;
        }

        public bool RemoveCardAt(int index)
        {
            if (!HasActiveRun)
                return false;

            if (!CurrentRun.RemoveCardAt(index))
                return false;

            NotifyRunChanged();
            return true;
        }

        public bool ClearRun()
        {
            if (CurrentRun == null || IsRunAlreadyCleared())
                return false;

            CurrentRun.ClearRun();
            OnRunCleared?.Invoke();
            return true;
        }

        public RunState GetSnapshot()
        {
            if (CurrentRun == null)
                return null;

            return CurrentRun.Clone();
        }

        private void NotifyRunChanged()
        {
            if (CurrentRun == null)
                return;

            OnRunChanged?.Invoke(CurrentRun);
        }

        private bool IsRunAlreadyCleared()
        {
            if (CurrentRun == null)
                return true;

            if (CurrentRun.isActive)
                return false;

            if (!string.IsNullOrEmpty(CurrentRun.runId))
                return false;

            if (CurrentRun.runSeed != 0)
                return false;

            if (!string.IsNullOrEmpty(CurrentRun.playerClassId))
                return false;

            if (CurrentRun.currentHp != 0 || CurrentRun.maxHp != 0 || CurrentRun.gold != 0)
                return false;

            if (CurrentRun.currentDeck == null)
                return true;

            return CurrentRun.currentDeck.Count == 0;
        }
    }
}
```

## FILE: BattleRunBridge.cs
**Path:** `Assets/Scripts/Run/Integration/BattleRunBridge.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Transfers persistent run data into battle-scene systems.
    /// </summary>
    public class BattleRunBridge : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CardCatalog cardCatalog;

        [Header("Battle Scene")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private PlayerBattleUnit player;

        [Header("Battle Result")]
        [SerializeField] private BattleEndPresentationController battleEndPresentationController;

        [Header("Options")]
        [SerializeField] private bool verboseLogs;

        public bool HasActiveRun =>
            RunManager.Instance != null &&
            RunManager.Instance.HasActiveRun;

        public int LastResolvedCardCount { get; private set; }
        public int LastMissingCardCount { get; private set; }

        public bool LastPlayerVitalsApplied { get; private set; }
        public int LastAppliedCurrentHp { get; private set; }
        public int LastAppliedMaxHp { get; private set; }

        public bool HasCommittedEncounterResult { get; private set; }
        public BattleOutcome LastCommittedOutcome { get; private set; } = BattleOutcome.None;
        public int LastCommittedCurrentHp { get; private set; }
        public int CommitCount { get; private set; }

        private BattleEndPresentationController subscribedPresentationController;

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
                        "[BattleRunBridge] BattleEndPresentationController reference is missing. " +
                        "Automatic Encounter HP commit is disabled.");
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

        public bool TryInitializeBattleFromActiveRun()
        {
            ResetEncounterCommitState();
            ResetAllDiagnostics();

            if (!TryGetValidActiveRun(out RunState run))
                return false;

            if (!TryResolveDeckFromRun(run, out List<CardData> resolvedCards))
                return false;

            if (!TryValidatePlayerVitalsFromRun(run, out int maxHp, out int currentHp))
                return false;

            deckController.BuildFromCardDataList(resolvedCards);
            player.InitializeVitals(maxHp, currentHp);

            LastPlayerVitalsApplied = true;
            LastAppliedMaxHp = player.MaxHp;
            LastAppliedCurrentHp = player.CurrentHp;

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleRunBridge] Initialized Battle from active run. " +
                    $"Class={run.playerClassId} | " +
                    $"Cards={LastResolvedCardCount} | " +
                    $"HP={LastAppliedCurrentHp}/{LastAppliedMaxHp}");
            }

            return true;
        }

        public bool TryBuildDeckFromActiveRun()
        {
            ResetDeckDiagnostics();

            if (!TryGetValidActiveRun(out RunState run))
                return false;

            if (!TryResolveDeckFromRun(run, out List<CardData> resolvedCards))
                return false;

            deckController.BuildFromCardDataList(resolvedCards);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleRunBridge] Built battle deck from active run. " +
                    $"Class={run.playerClassId} | Records={run.currentDeck.Count} | " +
                    $"Resolved={LastResolvedCardCount} | Missing={LastMissingCardCount}");
            }

            return true;
        }

        public bool TryApplyPlayerVitalsFromActiveRun()
        {
            ResetVitalsDiagnostics();

            if (!TryGetValidActiveRun(out RunState run))
                return false;

            if (!TryValidatePlayerVitalsFromRun(run, out int maxHp, out int currentHp))
                return false;

            player.InitializeVitals(maxHp, currentHp);

            LastPlayerVitalsApplied = true;
            LastAppliedMaxHp = player.MaxHp;
            LastAppliedCurrentHp = player.CurrentHp;

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleRunBridge] Applied player vitals from active run. " +
                    $"HP={LastAppliedCurrentHp}/{LastAppliedMaxHp}");
            }

            return true;
        }

        public void ResetEncounterCommitState()
        {
            HasCommittedEncounterResult = false;
            LastCommittedOutcome = BattleOutcome.None;
            LastCommittedCurrentHp = 0;

            if (verboseLogs)
                Debug.Log("[BattleRunBridge] Encounter commit state reset.");
        }

        public bool TryCommitPlayerHpToActiveRun()
        {
            if (HasCommittedEncounterResult)
                return false;

            if (player == null)
            {
                Debug.LogError(
                    "[BattleRunBridge] PlayerBattleUnit reference is missing. Encounter result commit aborted.");
                return false;
            }

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[BattleRunBridge] RunManager.Instance is null. Encounter result commit skipped.");
                }

                return false;
            }

            if (!runManager.HasActiveRun)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[BattleRunBridge] No active run exists. Encounter result commit skipped.");
                }

                return false;
            }

            RunState run = runManager.CurrentRun;
            if (run == null)
            {
                Debug.LogError(
                    "[BattleRunBridge] CurrentRun is null. Encounter result commit aborted.");
                return false;
            }

            if (!player.IsAlive)
            {
                Debug.LogError(
                    "[BattleRunBridge] Player is not alive. Encounter result commit aborted.");
                return false;
            }

            if (player.CurrentHp <= 0)
            {
                Debug.LogError(
                    "[BattleRunBridge] Player current HP is zero or below. Encounter result commit aborted.");
                return false;
            }

            if (!runManager.SetCurrentHp(player.CurrentHp))
            {
                Debug.LogError(
                    "[BattleRunBridge] RunManager.SetCurrentHp was rejected. Encounter result commit aborted.");
                return false;
            }

            HasCommittedEncounterResult = true;
            LastCommittedOutcome = BattleOutcome.EncounterCleared;
            LastCommittedCurrentHp = run.currentHp;
            CommitCount++;

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleRunBridge] Encounter result committed to active run. " +
                    $"Outcome=EncounterCleared | HP={LastCommittedCurrentHp}/{run.maxHp}");
            }

            return true;
        }

        private void HandleBattleEndPresentationReady(BattleOutcome outcome)
        {
            if (outcome != BattleOutcome.EncounterCleared)
                return;

            TryCommitPlayerHpToActiveRun();
        }

        private bool TryGetValidActiveRun(out RunState run)
        {
            run = null;

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                Debug.LogError("[BattleRunBridge] RunManager.Instance is null.");
                return false;
            }

            if (!runManager.HasActiveRun)
            {
                Debug.LogError("[BattleRunBridge] No active run exists.");
                return false;
            }

            run = runManager.CurrentRun;
            if (run == null)
            {
                Debug.LogError("[BattleRunBridge] CurrentRun is null.");
                return false;
            }

            return true;
        }

        private bool TryResolveDeckFromRun(RunState run, out List<CardData> resolvedCards)
        {
            resolvedCards = null;

            if (deckController == null)
            {
                Debug.LogError("[BattleRunBridge] DeckController reference is missing.");
                return false;
            }

            if (cardCatalog == null)
            {
                Debug.LogError("[BattleRunBridge] CardCatalog reference is missing.");
                return false;
            }

            if (run.currentDeck == null)
            {
                Debug.LogError("[BattleRunBridge] CurrentRun.currentDeck is null.");
                return false;
            }

            resolvedCards = new List<CardData>(run.currentDeck.Count);

            for (int i = 0; i < run.currentDeck.Count; i++)
            {
                RunCardRecord record = run.currentDeck[i];
                if (record == null)
                {
                    Debug.LogWarning("[BattleRunBridge] Skipping null RunCardRecord in current deck.");
                    LastMissingCardCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(record.cardId))
                {
                    Debug.LogWarning("[BattleRunBridge] Skipping RunCardRecord with blank card ID.");
                    LastMissingCardCount++;
                    continue;
                }

                // Phase 3D-A: upgradeLevel is retained in RunState.
                // A later card-instance factory will apply upgrade modifiers.

                if (!cardCatalog.TryGetCard(record.cardId, out CardData data))
                {
                    Debug.LogWarning(
                        $"[BattleRunBridge] Card ID not found in catalog: '{record.cardId}'.");
                    LastMissingCardCount++;
                    continue;
                }

                resolvedCards.Add(data);
            }

            LastResolvedCardCount = resolvedCards.Count;

            if (resolvedCards.Count == 0)
            {
                Debug.LogError(
                    "[BattleRunBridge] Active run deck contains no valid resolvable cards.");
                return false;
            }

            return true;
        }

        private bool TryValidatePlayerVitalsFromRun(RunState run, out int maxHp, out int currentHp)
        {
            maxHp = 0;
            currentHp = 0;

            if (player == null)
            {
                Debug.LogError("[BattleRunBridge] PlayerBattleUnit reference is missing.");
                return false;
            }

            if (run == null)
            {
                Debug.LogError("[BattleRunBridge] RunState is null.");
                return false;
            }

            if (run.maxHp < 1)
            {
                Debug.LogError(
                    $"[BattleRunBridge] Active run max HP is invalid: {run.maxHp}.");
                return false;
            }

            if (run.currentHp <= 0)
            {
                Debug.LogError(
                    "[BattleRunBridge] Active run player HP is zero or below. " +
                    "Battle initialization aborted.");
                return false;
            }

            maxHp = run.maxHp;
            currentHp = run.currentHp;

            if (currentHp > maxHp)
            {
                Debug.LogWarning(
                    $"[BattleRunBridge] Active run current HP ({currentHp}) exceeds max HP ({maxHp}). Clamping.");
                currentHp = maxHp;
            }

            return true;
        }

        private void ResetDeckDiagnostics()
        {
            LastResolvedCardCount = 0;
            LastMissingCardCount = 0;
        }

        private void ResetVitalsDiagnostics()
        {
            LastPlayerVitalsApplied = false;
            LastAppliedCurrentHp = 0;
            LastAppliedMaxHp = 0;
        }

        private void ResetAllDiagnostics()
        {
            ResetDeckDiagnostics();
            ResetVitalsDiagnostics();
        }
    }
}
```

## FILE: CardRewardPool.cs
**Path:** `Assets/Scripts/Run/Rewards/Data/CardRewardPool.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "CardRewardPool",
        menuName = "Card Battle/Rewards/Card Reward Pool",
        order = 20)]
    public class CardRewardPool : ScriptableObject
    {
        [SerializeField] private List<CardData> cards = new List<CardData>();

        public IReadOnlyList<CardData> Cards => cards;

        public int BuildUniqueChoices(
            int requestedCount,
            System.Random random,
            List<CardData> output)
        {
            if (output == null)
                return 0;

            output.Clear();

            if (requestedCount <= 0)
                return 0;

            System.Random rng = random ?? new System.Random();

            var uniqueById = new List<CardData>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;

                string cardId = card.CardId;
                if (string.IsNullOrWhiteSpace(cardId))
                    continue;

                if (!seenIds.Add(cardId))
                    continue;

                uniqueById.Add(card);
            }

            if (uniqueById.Count == 0)
                return 0;

            FisherYatesShuffle(uniqueById, rng);

            int count = Mathf.Min(requestedCount, uniqueById.Count);
            for (int i = 0; i < count; i++)
                output.Add(uniqueById[i]);

            return count;
        }

        private static void FisherYatesShuffle(List<CardData> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                CardData temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cards == null || cards.Count == 0)
                return;

            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                {
                    Debug.LogWarning(
                        $"[CardRewardPool] Null CardData entry at index {i} in '{name}'.",
                        this);
                    continue;
                }

                string cardId = card.CardId;
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    Debug.LogWarning(
                        $"[CardRewardPool] Card at index {i} has a blank CardId in '{name}'.",
                        this);
                    continue;
                }

                if (!seenIds.Add(cardId))
                {
                    Debug.LogWarning(
                        $"[CardRewardPool] Duplicate CardId '{cardId}' at index {i} in '{name}'.",
                        this);
                }
            }
        }
#endif
    }
}
```

## FILE: EncounterRewardConfig.cs
**Path:** `Assets/Scripts/Run/Rewards/Data/EncounterRewardConfig.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "EncounterRewardConfig",
        menuName = "Card Battle/Rewards/Encounter Reward Config",
        order = 21)]
    public class EncounterRewardConfig : ScriptableObject
    {
        [SerializeField] private int goldReward = 20;
        [SerializeField] private int cardChoiceCount = 3;
        [SerializeField] private CardRewardPool cardRewardPool;

        public int GoldReward => Mathf.Max(0, goldReward);
        public int CardChoiceCount => Mathf.Max(0, cardChoiceCount);
        public CardRewardPool CardRewardPool => cardRewardPool;
    }
}
```

## FILE: RewardSession.cs
**Path:** `Assets/Scripts/Run/Rewards/Runtime/RewardSession.cs`
```csharp
using System.Collections.Generic;

namespace CardBattle.Core
{
    public class RewardSession
    {
        private readonly List<CardData> internalChoices = new List<CardData>();

        public int GoldAmount { get; }
        public bool GoldGranted { get; internal set; }

        public IReadOnlyList<CardData> CardChoices => internalChoices;

        public bool CardChoiceResolved { get; internal set; }
        public bool WasCardSkipped { get; internal set; }
        public CardData SelectedCard { get; internal set; }

        public bool IsComplete =>
            GoldGranted &&
            CardChoiceResolved;

        public int ChoiceCount => internalChoices.Count;
        public bool HasCardChoices => internalChoices.Count > 0;

        public RewardSession(int goldAmount, IEnumerable<CardData> cardChoices)
        {
            GoldAmount = goldAmount < 0 ? 0 : goldAmount;

            if (cardChoices != null)
            {
                foreach (CardData card in cardChoices)
                {
                    if (card == null)
                        continue;

                    internalChoices.Add(card);
                }
            }

            GoldGranted = false;
            CardChoiceResolved = false;
            WasCardSkipped = false;
            SelectedCard = null;
        }
    }
}
```

## FILE: RewardController.cs
**Path:** `Assets/Scripts/Run/Rewards/Systems/RewardController.cs`
```csharp
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
```

## FILE: RewardCardChoiceView.cs
**Path:** `Assets/Scripts/Run/Rewards/UI/RewardCardChoiceView.cs`
```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class RewardCardChoiceView : MonoBehaviour
    {
        [Header("Card Display")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image cardTypeIcon;
        [SerializeField] private Button chooseButton;

        [Header("State")]
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Interaction")]
        [SerializeField] private float disabledAlpha = 0.45f;
        [SerializeField] private float enabledAlpha = 1f;

        public CardData CardData { get; private set; }
        public int ChoiceIndex { get; private set; } = -1;
        public bool IsInteractable { get; private set; }

        public event Action<int> OnChoiceRequested;

        private void Awake()
        {
            if (chooseButton != null)
            {
                chooseButton.onClick.RemoveListener(HandleChooseClicked);
                chooseButton.onClick.AddListener(HandleChooseClicked);
            }
        }

        private void OnDestroy()
        {
            if (chooseButton != null)
                chooseButton.onClick.RemoveListener(HandleChooseClicked);
        }

        public void Bind(CardData cardData, int choiceIndex)
        {
            CardData = cardData;
            ChoiceIndex = choiceIndex;

            if (cardData == null)
            {
                ClearView();
                return;
            }

            if (nameText != null)
                nameText.text = cardData.DisplayName;

            if (costText != null)
                costText.text = cardData.ApCost.ToString();

            if (descriptionText != null)
                descriptionText.text = CardDescriptionBuilder.Build(cardData);

            if (artworkImage != null)
            {
                Sprite artwork = cardData.Artwork;
                artworkImage.sprite = artwork;
                artworkImage.enabled = artwork != null;
            }

            if (cardTypeIcon != null)
                cardTypeIcon.enabled = cardTypeIcon.sprite != null;

            SetSelected(false);
            SetInteractable(true);
        }

        public void SetInteractable(bool value)
        {
            IsInteractable = value;

            if (chooseButton != null)
                chooseButton.interactable = value;

            if (canvasGroup != null)
                canvasGroup.alpha = value ? enabledAlpha : disabledAlpha;
        }

        public void SetSelected(bool value)
        {
            if (selectedRoot != null)
                selectedRoot.SetActive(value);
        }

        public void ClearView()
        {
            CardData = null;
            ChoiceIndex = -1;

            if (nameText != null)
                nameText.text = string.Empty;

            if (costText != null)
                costText.text = string.Empty;

            if (descriptionText != null)
                descriptionText.text = string.Empty;

            if (artworkImage != null)
            {
                artworkImage.sprite = null;
                artworkImage.enabled = false;
            }

            if (cardTypeIcon != null)
                cardTypeIcon.enabled = false;

            SetSelected(false);
            SetInteractable(false);
        }

        private void HandleChooseClicked()
        {
            if (!IsInteractable || CardData == null || ChoiceIndex < 0)
                return;

            OnChoiceRequested?.Invoke(ChoiceIndex);
        }
    }
}
```

## FILE: RewardPanelUI.cs
**Path:** `Assets/Scripts/Run/Rewards/UI/RewardPanelUI.cs`
```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class RewardPanelUI : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] private RewardController rewardController;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Reward Summary")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI goldAmountText;
        [SerializeField] private GameObject goldRoot;

        [Header("Card Choices")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private RewardCardChoiceView choicePrefab;
        [SerializeField] private GameObject noChoicesRoot;

        [Header("Actions")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Options")]
        [SerializeField] private bool hidePanelOnStart = true;
        [SerializeField] private bool disableNonSelectedCardsAfterResolution = true;
        [SerializeField] private bool verboseLogs;

        public bool IsVisible { get; private set; }
        public bool IsAwaitingChoice { get; private set; }
        public bool IsCompletedState { get; private set; }

        public event Action OnContinueRequested;

        private readonly List<RewardCardChoiceView> spawnedChoices = new List<RewardCardChoiceView>();
        private RewardController subscribedRewardController;

        private void Awake()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipClicked);
                skipButton.onClick.AddListener(HandleSkipClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinueClicked);
                continueButton.onClick.AddListener(HandleContinueClicked);
            }
        }

        private void Start()
        {
            if (hidePanelOnStart)
                HidePanel();
            else
                SetContinueVisible(false);

            if (rewardController != null && rewardController.CurrentSession != null)
                ShowSession(rewardController.CurrentSession);
        }

        private void OnEnable()
        {
            RefreshRewardSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeRewardController();
        }

        private void OnDestroy()
        {
            UnsubscribeRewardController();

            if (skipButton != null)
                skipButton.onClick.RemoveListener(HandleSkipClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinueClicked);

            ClearSpawnedChoices();
        }

        private void RefreshRewardSubscription()
        {
            UnsubscribeRewardController();

            if (rewardController == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RewardPanelUI] RewardController reference is missing. Event subscription skipped.");
                }

                return;
            }

            subscribedRewardController = rewardController;
            subscribedRewardController.OnRewardSessionStarted += HandleRewardSessionStarted;
            subscribedRewardController.OnRewardCardSelected += HandleRewardCardSelected;
            subscribedRewardController.OnRewardCardSkipped += HandleRewardCardSkipped;
            subscribedRewardController.OnRewardSessionCompleted += HandleRewardSessionCompleted;
        }

        private void UnsubscribeRewardController()
        {
            if (subscribedRewardController == null)
                return;

            subscribedRewardController.OnRewardSessionStarted -= HandleRewardSessionStarted;
            subscribedRewardController.OnRewardCardSelected -= HandleRewardCardSelected;
            subscribedRewardController.OnRewardCardSkipped -= HandleRewardCardSkipped;
            subscribedRewardController.OnRewardSessionCompleted -= HandleRewardSessionCompleted;
            subscribedRewardController = null;
        }

        private void HandleRewardSessionStarted(RewardSession session)
        {
            ShowSession(session);
        }

        public void ShowSession(RewardSession session)
        {
            if (session == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[RewardPanelUI] ShowSession called with null session.");

                HidePanel();
                return;
            }

            if (panelRoot == null)
            {
                Debug.LogError("[RewardPanelUI] Panel Root reference is missing.");
                return;
            }

            ClearSpawnedChoices();

            panelRoot.SetActive(true);
            IsVisible = true;

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }

            IsAwaitingChoice = !session.CardChoiceResolved;
            IsCompletedState = session.IsComplete;

            if (titleText != null)
                titleText.text = "Reward";

            UpdateGoldDisplay(session.GoldAmount);
            BuildChoiceViews(session);
            UpdateNoChoicesDisplay(session.ChoiceCount == 0);
            UpdateResultTextForSession(session);

            if (session.IsComplete)
                ApplyCompletedState(session);
            else
            {
                SetContinueVisible(false);
                SetChoiceInteraction(true);
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RewardPanelUI] ShowSession | Gold={session.GoldAmount} | " +
                    $"Choices={session.ChoiceCount} | Awaiting={IsAwaitingChoice} | Complete={IsCompletedState}");
            }
        }

        public void HidePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            IsVisible = false;
        }

        public void RefreshFromCurrentSession()
        {
            if (rewardController == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[RewardPanelUI] RefreshFromCurrentSession: RewardController is missing.");

                HidePanel();
                return;
            }

            RewardSession session = rewardController.CurrentSession;
            if (session == null)
            {
                HidePanel();
                return;
            }

            ShowSession(session);
        }

        public void SetContinueInteractable(bool value)
        {
            if (continueButton == null)
                return;

            continueButton.interactable = value;
        }

        private void HandleChoiceRequested(int choiceIndex)
        {
            if (!IsAwaitingChoice)
                return;

            if (rewardController == null)
            {
                Debug.LogError("[RewardPanelUI] RewardController reference is missing.");
                return;
            }

            SetChoiceInteraction(false);

            bool accepted = rewardController.TryChooseCard(choiceIndex);

            if (accepted)
                return;

            RewardSession session = rewardController.CurrentSession;
            if (session != null && !session.CardChoiceResolved)
                SetChoiceInteraction(true);

            if (verboseLogs)
            {
                Debug.LogWarning(
                    $"[RewardPanelUI] TryChooseCard({choiceIndex}) was rejected.");
            }
        }

        private void HandleSkipClicked()
        {
            if (!IsAwaitingChoice)
                return;

            if (rewardController == null)
            {
                Debug.LogError("[RewardPanelUI] RewardController reference is missing.");
                return;
            }

            SetChoiceInteraction(false);

            bool accepted = rewardController.TrySkipCard();

            if (accepted)
                return;

            RewardSession session = rewardController.CurrentSession;
            if (session != null && !session.CardChoiceResolved)
                SetChoiceInteraction(true);

            if (verboseLogs)
                Debug.LogWarning("[RewardPanelUI] TrySkipCard was rejected.");
        }

        private void HandleRewardCardSelected(CardData selectedCard)
        {
            IsAwaitingChoice = false;
            SetSkipVisible(false);
            SetChoiceInteraction(false);

            MarkSelectedCardView(selectedCard);

            if (resultText != null && selectedCard != null)
            {
                resultText.text = $"Selected: {selectedCard.DisplayName}";
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RewardPanelUI] Card selected: " +
                    $"{(selectedCard != null ? selectedCard.CardId : "null")}");
            }
        }

        private void HandleRewardCardSkipped()
        {
            IsAwaitingChoice = false;
            SetSkipVisible(false);
            SetChoiceInteraction(false);
            ClearAllSelectedStates();

            if (resultText != null)
                resultText.text = "Skipped card reward.";

            if (verboseLogs)
                Debug.Log("[RewardPanelUI] Card reward skipped.");
        }

        private void HandleRewardSessionCompleted(RewardSession session)
        {
            IsAwaitingChoice = false;
            IsCompletedState = true;
            SetChoiceInteraction(false);
            SetSkipVisible(false);
            SetContinueVisible(true);

            if (session != null)
                UpdateResultTextForSession(session);

            if (verboseLogs)
                Debug.Log("[RewardPanelUI] Reward session completed. Continue is available.");
        }

        private void HandleContinueClicked()
        {
            if (rewardController == null)
            {
                Debug.LogError("[RewardPanelUI] RewardController reference is missing.");
                return;
            }

            RewardSession session = rewardController.CurrentSession;
            if (session == null || !session.IsComplete || !IsCompletedState)
                return;

            SetContinueVisible(false);
            OnContinueRequested?.Invoke();

            if (verboseLogs)
                Debug.Log("[RewardPanelUI] OnContinueRequested invoked.");
        }

        private void BuildChoiceViews(RewardSession session)
        {
            if (session.ChoiceCount <= 0)
                return;

            if (choiceContainer == null)
            {
                Debug.LogError("[RewardPanelUI] Choice Container reference is missing.");
                return;
            }

            if (choicePrefab == null)
            {
                Debug.LogError("[RewardPanelUI] Choice Prefab reference is missing.");
                return;
            }

            IReadOnlyList<CardData> choices = session.CardChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                CardData cardData = choices[i];
                if (cardData == null)
                    continue;

                RewardCardChoiceView view = Instantiate(choicePrefab, choiceContainer);
                view.Bind(cardData, i);
                view.OnChoiceRequested += HandleChoiceRequested;
                spawnedChoices.Add(view);
            }
        }

        private void ClearSpawnedChoices()
        {
            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view == null)
                    continue;

                view.OnChoiceRequested -= HandleChoiceRequested;
                Destroy(view.gameObject);
            }

            spawnedChoices.Clear();
        }

        private void SetChoiceInteraction(bool value)
        {
            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view != null)
                    view.SetInteractable(value);
            }

            UpdateSkipInteractable(value);
        }

        private void UpdateSkipInteractable(bool value)
        {
            if (skipButton == null)
                return;

            RewardSession session = rewardController != null
                ? rewardController.CurrentSession
                : null;

            bool canSkip = value &&
                           session != null &&
                           !session.CardChoiceResolved;

            skipButton.interactable = canSkip;
            SetSkipVisible(canSkip || (session != null && !session.CardChoiceResolved));
        }

        private void SetSkipVisible(bool visible)
        {
            if (skipButton != null)
                skipButton.gameObject.SetActive(visible);
        }

        private void SetContinueVisible(bool visible)
        {
            if (continueButton == null)
                return;

            continueButton.gameObject.SetActive(visible);
            continueButton.interactable = visible;
        }

        private void UpdateGoldDisplay(int goldAmount)
        {
            bool hasGold = goldAmount > 0;

            if (goldRoot != null)
                goldRoot.SetActive(hasGold);

            if (goldAmountText != null)
                goldAmountText.text = hasGold ? $"+{goldAmount} Gold" : string.Empty;
        }

        private void UpdateNoChoicesDisplay(bool showNoChoices)
        {
            if (noChoicesRoot != null)
                noChoicesRoot.SetActive(showNoChoices);
        }

        private void ApplyCompletedState(RewardSession session)
        {
            IsAwaitingChoice = false;
            IsCompletedState = true;
            SetChoiceInteraction(false);
            SetSkipVisible(false);
            SetContinueVisible(true);
            UpdateResultTextForSession(session);

            if (session.WasCardSkipped)
            {
                ClearAllSelectedStates();
                return;
            }

            if (session.SelectedCard != null)
                MarkSelectedCardView(session.SelectedCard);
        }

        private void MarkSelectedCardView(CardData selectedCard)
        {
            if (selectedCard == null)
                return;

            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view == null || view.CardData == null)
                    continue;

                bool isSelected = ReferenceEquals(view.CardData, selectedCard) ||
                                  string.Equals(
                                      view.CardData.CardId,
                                      selectedCard.CardId,
                                      StringComparison.Ordinal);

                view.SetSelected(isSelected);

                if (disableNonSelectedCardsAfterResolution && !isSelected)
                    view.SetInteractable(false);
            }
        }

        private void ClearAllSelectedStates()
        {
            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                RewardCardChoiceView view = spawnedChoices[i];
                if (view != null)
                    view.SetSelected(false);
            }
        }

        private void UpdateResultTextForSession(RewardSession session)
        {
            if (resultText == null || session == null)
                return;

            if (!session.CardChoiceResolved)
            {
                resultText.text = string.Empty;
                return;
            }

            if (session.WasCardSkipped)
            {
                resultText.text = "Skipped card reward.";
                return;
            }

            if (session.SelectedCard != null)
                resultText.text = $"Selected: {session.SelectedCard.DisplayName}";
        }
    }
}
```