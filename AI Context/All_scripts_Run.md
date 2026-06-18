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
        [Header("Encounter Source")]
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;
        [SerializeField] private bool preferRuntimeEncounterRewardConfig = true;

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

        public EncounterRewardConfig LastResolvedRewardConfig { get; private set; }
        public string LastResolvedRewardSource { get; private set; } = string.Empty;
        public bool LastUsedRuntimeEncounterRewardConfig { get; private set; }
        public string LastRewardConfigResolveError { get; private set; } = string.Empty;

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

        public bool TryGetCurrentResolvedRewardConfig(out EncounterRewardConfig resolvedConfig)
        {
            return TryResolveRewardConfig(out resolvedConfig);
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

            if (!TryResolveRewardConfig(out EncounterRewardConfig resolvedRewardConfig))
            {
                Debug.LogError(
                    $"[RewardController] Reward config resolution failed. {LastRewardConfigResolveError}");
                return false;
            }

            int goldAmount = resolvedRewardConfig.GoldReward;
            int requestedCardChoices = resolvedRewardConfig.CardChoiceCount;

            if (goldAmount <= 0 && requestedCardChoices <= 0)
            {
                Debug.LogError(
                    "[RewardController] Reward config has no gold and no card choices. Reward session not created.");
                return false;
            }

            if (requestedCardChoices > 0 && resolvedRewardConfig.CardRewardPool == null)
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
                int generatedCount = resolvedRewardConfig.CardRewardPool.BuildUniqueChoices(
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
                    $"Source={LastResolvedRewardSource} | " +
                    $"Gold={session.GoldAmount} | Choices={session.ChoiceCount} | " +
                    $"SessionCreateCount={SessionCreateCount}");
            }

            return true;
        }

        public bool TryGrantPendingGold()
        {
            return TryGrantGoldForCurrentSession();
        }

        private bool TryResolveRewardConfig(out EncounterRewardConfig resolvedConfig)
        {
            resolvedConfig = null;
            LastResolvedRewardConfig = null;
            LastResolvedRewardSource = string.Empty;
            LastUsedRuntimeEncounterRewardConfig = false;
            LastRewardConfigResolveError = string.Empty;

            if (preferRuntimeEncounterRewardConfig &&
                runtimeEncounterContext != null &&
                runtimeEncounterContext.HasCurrentEncounter &&
                runtimeEncounterContext.IsCurrentEncounterValid)
            {
                EncounterRewardConfig encounterConfig = runtimeEncounterContext.CurrentRewardConfig;
                if (encounterConfig == null)
                {
                    LastRewardConfigResolveError = "Current encounter has no RewardConfig.";
                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            $"[RewardController] Reward config resolution failed: {LastRewardConfigResolveError}");
                    }

                    return false;
                }

                resolvedConfig = encounterConfig;
                LastResolvedRewardConfig = encounterConfig;
                LastResolvedRewardSource =
                    $"RuntimeEncounter: {runtimeEncounterContext.CurrentEncounterId}";
                LastUsedRuntimeEncounterRewardConfig = true;

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[RewardController] Using reward config from RuntimeEncounter: " +
                        $"{runtimeEncounterContext.CurrentEncounterId} | Config={encounterConfig.name}");
                }

                return true;
            }

            if (rewardConfig != null)
            {
                resolvedConfig = rewardConfig;
                LastResolvedRewardConfig = rewardConfig;
                LastResolvedRewardSource = "Fallback Inspector";
                LastUsedRuntimeEncounterRewardConfig = false;

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[RewardController] Using fallback Inspector reward config: {rewardConfig.name}");
                }

                return true;
            }

            LastRewardConfigResolveError =
                "No reward config available from runtime encounter or fallback.";
            if (verboseLogs)
            {
                Debug.LogWarning(
                    $"[RewardController] Reward config resolution failed: {LastRewardConfigResolveError}");
            }

            return false;
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
            LastResolvedRewardConfig = null;
            LastResolvedRewardSource = string.Empty;
            LastUsedRuntimeEncounterRewardConfig = false;
            LastRewardConfigResolveError = string.Empty;

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

        private System.Random CreateRewardRandom(RunState run)
        {
            unchecked
            {
                int seed = run.runSeed;
                seed = seed * 31 + GetStableOrdinalHash(run.runId);

                if (runtimeEncounterContext != null &&
                    runtimeEncounterContext.HasCurrentEncounter)
                {
                    seed = seed * 31 +
                           GetStableOrdinalHash(runtimeEncounterContext.CurrentEncounterId);
                }

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

## FILE: EncounterCompletionController.cs
**Path:** `Assets/Scripts/Run/Flow/EncounterCompletionController.cs`
```csharp
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
```

## FILE: RewardControllerDebugTest.cs
**Path:** `Assets/Scripts/Run/Rewards/Debug/RewardControllerDebugTest.cs`
```csharp
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
```

## FILE: EncounterCompletionDebugTest.cs
**Path:** `Assets/Scripts/Run/Flow/Debug/EncounterCompletionDebugTest.cs`
```csharp
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
```

## FILE: EncounterCatalog.cs
**Path:** `Assets/Scripts/Run/Encounters/Data/EncounterCatalog.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "EncounterCatalog",
        menuName = "Card Battle/Encounters/Encounter Catalog",
        order = 31)]
    public class EncounterCatalog : ScriptableObject
    {
        [SerializeField] private List<EncounterData> encounters = new List<EncounterData>();

        public IReadOnlyList<EncounterData> Encounters => encounters;

        public bool TryGetEncounter(string encounterId, out EncounterData encounter)
        {
            encounter = null;

            if (string.IsNullOrWhiteSpace(encounterId) || encounters == null)
                return false;

            for (int i = 0; i < encounters.Count; i++)
            {
                EncounterData candidate = encounters[i];
                if (candidate == null)
                    continue;

                if (string.Equals(candidate.EncounterId, encounterId, StringComparison.Ordinal))
                {
                    encounter = candidate;
                    return true;
                }
            }

            return false;
        }

        public int CountValidEncounters()
        {
            if (encounters == null)
                return 0;

            int count = 0;
            for (int i = 0; i < encounters.Count; i++)
            {
                EncounterData encounter = encounters[i];
                if (encounter != null && encounter.IsRuntimeValid(out _))
                    count++;
            }

            return count;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (encounters == null || encounters.Count == 0)
                return;

            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < encounters.Count; i++)
            {
                EncounterData encounter = encounters[i];
                if (encounter == null)
                {
                    Debug.LogWarning(
                        $"[EncounterCatalog] Null EncounterData entry at index {i} in '{name}'.",
                        this);
                    continue;
                }

                string id = encounter.EncounterId;
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning(
                        $"[EncounterCatalog] Encounter at index {i} has a blank Encounter ID in '{name}'.",
                        this);
                }
                else if (!seenIds.Add(id))
                {
                    Debug.LogWarning(
                        $"[EncounterCatalog] Duplicate Encounter ID '{id}' in '{name}'.",
                        this);
                }

                if (!encounter.IsRuntimeValid(out string error))
                {
                    Debug.LogWarning(
                        $"[EncounterCatalog] Encounter '{encounter.name}' is invalid: {error}",
                        this);
                }
            }
        }
#endif
    }
}
```

## FILE: EncounterData.cs
**Path:** `Assets/Scripts/Run/Encounters/Data/EncounterData.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "Encounter",
        menuName = "Card Battle/Encounters/Encounter Data",
        order = 30)]
    public class EncounterData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string encounterId;
        [SerializeField] private string displayName;
        [SerializeField] private EncounterType encounterType = EncounterType.Normal;

        [Header("Environment")]
        [SerializeField] private string environmentId;
        [SerializeField] private string environmentSceneName;

        [Header("Enemies")]
        [SerializeField] private List<EncounterEnemyEntry> enemies = new List<EncounterEnemyEntry>();

        [Header("Reward")]
        [SerializeField] private EncounterRewardConfig rewardConfig;

        [Header("Randomization")]
        [SerializeField] private int encounterSeedOffset;

        public string EncounterId =>
            string.IsNullOrWhiteSpace(encounterId)
                ? name
                : encounterId;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;

        public EncounterType EncounterType => encounterType;
        public string EnvironmentId => environmentId;
        public string EnvironmentSceneName => environmentSceneName;
        public EncounterRewardConfig RewardConfig => rewardConfig;
        public int EncounterSeedOffset => encounterSeedOffset;

        public IReadOnlyList<EncounterEnemyEntry> Enemies => enemies;
        public int EnemyCount => enemies != null ? enemies.Count : 0;

        public bool TryGetEnemyEntry(string slotId, out EncounterEnemyEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(slotId) || enemies == null)
                return false;

            for (int i = 0; i < enemies.Count; i++)
            {
                EncounterEnemyEntry candidate = enemies[i];
                if (candidate == null || !candidate.IsValid)
                    continue;

                if (string.Equals(candidate.SlotId, slotId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        public int GetValidEnemyCount()
        {
            if (enemies == null)
                return 0;

            int count = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                EncounterEnemyEntry entry = enemies[i];
                if (entry != null && entry.IsValid)
                    count++;
            }

            return count;
        }

        public void GetValidEnemyEntries(List<EncounterEnemyEntry> output)
        {
            if (output == null)
                return;

            output.Clear();

            if (enemies == null)
                return;

            for (int i = 0; i < enemies.Count; i++)
            {
                EncounterEnemyEntry entry = enemies[i];
                if (entry != null && entry.IsValid)
                    output.Add(entry);
            }

            output.Sort(CompareSpawnOrder);
        }

        public bool IsRuntimeValid(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(EncounterId))
            {
                error = "Encounter ID is blank.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(environmentId))
            {
                error = "Environment ID is blank.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(environmentSceneName))
            {
                error = "Environment Scene Name is blank.";
                return false;
            }

            if (rewardConfig == null)
            {
                error = "Reward Config is missing.";
                return false;
            }

            if (enemies == null || enemies.Count == 0)
            {
                error = "Encounter has no valid enemy entries.";
                return false;
            }

            var seenSlotIds = new HashSet<string>(StringComparer.Ordinal);
            int validCount = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                EncounterEnemyEntry entry = enemies[i];
                if (entry == null)
                {
                    error = $"Enemy entry at index {i} is null.";
                    return false;
                }

                if (!entry.IsValid)
                {
                    if (string.IsNullOrWhiteSpace(entry.SlotId))
                    {
                        error = $"Enemy entry at index {i} has a blank slot ID.";
                        return false;
                    }

                    error = $"Enemy entry at index {i} has no EnemyData.";
                    return false;
                }

                if (!seenSlotIds.Add(entry.SlotId))
                {
                    error = $"Duplicate slot ID '{entry.SlotId}'.";
                    return false;
                }

                validCount++;
            }

            if (validCount == 0)
            {
                error = "Encounter has no valid enemy entries.";
                return false;
            }

            return true;
        }

        private static int CompareSpawnOrder(EncounterEnemyEntry a, EncounterEnemyEntry b)
        {
            if (ReferenceEquals(a, b))
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            return a.SpawnOrder.CompareTo(b.SpawnOrder);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                Debug.LogWarning(
                    $"[EncounterData] Encounter ID is blank in '{name}'.",
                    this);
            }

            if (string.IsNullOrWhiteSpace(environmentId))
            {
                Debug.LogWarning(
                    $"[EncounterData] Environment ID is blank in '{name}'.",
                    this);
            }

            if (string.IsNullOrWhiteSpace(environmentSceneName))
            {
                Debug.LogWarning(
                    $"[EncounterData] Environment Scene Name is blank in '{name}'.",
                    this);
            }

            if (rewardConfig == null)
            {
                Debug.LogWarning(
                    $"[EncounterData] Reward Config is missing in '{name}'.",
                    this);
            }

            if (enemies == null || enemies.Count == 0)
            {
                Debug.LogWarning(
                    $"[EncounterData] No enemy entries configured in '{name}'.",
                    this);
                return;
            }

            var seenSlotIds = new HashSet<string>(StringComparer.Ordinal);
            var seenSpawnOrders = new HashSet<int>();

            for (int i = 0; i < enemies.Count; i++)
            {
                EncounterEnemyEntry entry = enemies[i];
                if (entry == null)
                {
                    Debug.LogWarning(
                        $"[EncounterData] Null enemy entry at index {i} in '{name}'.",
                        this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.SlotId))
                {
                    Debug.LogWarning(
                        $"[EncounterData] Enemy entry at index {i} has a blank slot ID in '{name}'.",
                        this);
                }
                else if (!seenSlotIds.Add(entry.SlotId))
                {
                    Debug.LogWarning(
                        $"[EncounterData] Duplicate slot ID '{entry.SlotId}' in '{name}'.",
                        this);
                }

                if (!seenSpawnOrders.Add(entry.SpawnOrder))
                {
                    Debug.LogWarning(
                        $"[EncounterData] Duplicate spawn order {entry.SpawnOrder} in '{name}'.",
                        this);
                }

                if (entry.EnemyData == null)
                {
                    Debug.LogWarning(
                        $"[EncounterData] Enemy entry at index {i} has no EnemyData in '{name}'.",
                        this);
                }
            }
        }
#endif
    }
}
```

## FILE: EncounterEnemyEntry.cs
**Path:** `Assets/Scripts/Run/Encounters/Data/EncounterEnemyEntry.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class EncounterEnemyEntry
    {
        [SerializeField] private string slotId;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private int spawnOrder;

        public string SlotId => slotId;
        public EnemyData EnemyData => enemyData;
        public int SpawnOrder => spawnOrder;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(slotId) &&
            enemyData != null;

        public string EnemyId =>
            enemyData != null ? enemyData.EnemyId : string.Empty;
    }
}
```

## FILE: EncounterType.cs
**Path:** `Assets/Scripts/Run/Encounters/Data/EncounterType.cs`
```csharp
namespace CardBattle.Core
{
    public enum EncounterType
    {
        Normal,
        Elite,
        Boss
    }
}
```

## FILE: EncounterDataDebugTest.cs
**Path:** `Assets/Scripts/Run/Encounters/Debug/EncounterDataDebugTest.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class EncounterDataDebugTest : MonoBehaviour
    {
        [SerializeField] private EncounterData encounterData;
        [SerializeField] private EncounterCatalog encounterCatalog;
        [SerializeField] private string lookupEncounterId;

        private readonly List<EncounterEnemyEntry> sortedEnemyScratch = new List<EncounterEnemyEntry>();

        [ContextMenu("Debug Print Encounter")]
        private void DebugPrintEncounter()
        {
            if (encounterData == null)
            {
                Debug.LogError("[EncounterDataDebugTest] EncounterData reference is missing.");
                return;
            }

            bool isValid = encounterData.IsRuntimeValid(out string error);

            Debug.Log(
                $"[EncounterDataDebugTest] --- Encounter ---\n" +
                $"EncounterId={encounterData.EncounterId}\n" +
                $"DisplayName={encounterData.DisplayName}\n" +
                $"Type={encounterData.EncounterType}\n" +
                $"EnvironmentId={encounterData.EnvironmentId}\n" +
                $"EnvironmentSceneName={encounterData.EnvironmentSceneName}\n" +
                $"RewardConfig={(encounterData.RewardConfig != null ? encounterData.RewardConfig.name : "null")}\n" +
                $"EncounterSeedOffset={encounterData.EncounterSeedOffset}\n" +
                $"EnemyCount={encounterData.EnemyCount}\n" +
                $"ValidEnemyCount={encounterData.GetValidEnemyCount()}\n" +
                $"IsRuntimeValid={isValid}\n" +
                $"ValidationError={(isValid ? string.Empty : error)}");

            sortedEnemyScratch.Clear();
            encounterData.GetValidEnemyEntries(sortedEnemyScratch);

            for (int i = 0; i < sortedEnemyScratch.Count; i++)
            {
                EncounterEnemyEntry entry = sortedEnemyScratch[i];
                if (entry == null)
                    continue;

                Debug.Log(
                    $"[EncounterDataDebugTest] Enemy[{i}] | " +
                    $"SlotId={entry.SlotId} | " +
                    $"EnemyId={entry.EnemyId} | " +
                    $"DisplayName={(entry.EnemyData != null ? entry.EnemyData.DisplayName : "n/a")} | " +
                    $"SpawnOrder={entry.SpawnOrder}");
            }
        }

        [ContextMenu("Debug Validate Encounter")]
        private void DebugValidateEncounter()
        {
            if (encounterData == null)
            {
                Debug.LogError("[EncounterDataDebugTest] EncounterData reference is missing.");
                return;
            }

            bool isValid = encounterData.IsRuntimeValid(out string error);
            Debug.Log(
                $"[EncounterDataDebugTest] Validate | " +
                $"IsRuntimeValid={isValid} | " +
                $"Error={(isValid ? "none" : error)}");
        }

        [ContextMenu("Debug Catalog Lookup")]
        private void DebugCatalogLookup()
        {
            if (encounterCatalog == null)
            {
                Debug.LogError("[EncounterDataDebugTest] EncounterCatalog reference is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lookupEncounterId))
            {
                Debug.LogWarning("[EncounterDataDebugTest] lookupEncounterId is blank.");
                return;
            }

            bool found = encounterCatalog.TryGetEncounter(lookupEncounterId, out EncounterData foundEncounter);
            Debug.Log(
                $"[EncounterDataDebugTest] Catalog Lookup | " +
                $"Id='{lookupEncounterId}' | Found={found} | " +
                $"Asset={(foundEncounter != null ? foundEncounter.name : "null")} | " +
                $"ValidEncounters={encounterCatalog.CountValidEncounters()}");
        }
    }
}
```

## FILE: RuntimeEncounterContext.cs
**Path:** `Assets/Scripts/Run/Encounters/Runtime/RuntimeEncounterContext.cs`
```csharp
using System;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Holds the currently selected EncounterData for the active Battle flow.
    /// Does not spawn enemies, load scenes, or mutate RunState.
    /// </summary>
    public class RuntimeEncounterContext : MonoBehaviour
    {
        [Header("Encounter Source")]
        [SerializeField] private EncounterCatalog encounterCatalog;

        [Header("Optional Default")]
        [SerializeField] private EncounterData defaultEncounter;
        [SerializeField] private string defaultEncounterId;

        [Header("Options")]
        [SerializeField] private bool selectDefaultOnStart;
        [SerializeField] private bool verboseLogs;

        public EncounterCatalog EncounterCatalog => encounterCatalog;
        public EncounterData CurrentEncounter { get; private set; }
        public string CurrentEncounterId { get; private set; } = string.Empty;
        public bool HasCurrentEncounter => CurrentEncounter != null;
        public bool IsCurrentEncounterValid { get; private set; }
        public string LastValidationError { get; private set; } = string.Empty;
        public int SelectionCount { get; private set; }
        public int ClearCount { get; private set; }

        public event Action<EncounterData> OnEncounterSelected;
        public event Action OnEncounterCleared;

        public EncounterType CurrentEncounterType =>
            CurrentEncounter != null ? CurrentEncounter.EncounterType : EncounterType.Normal;

        public string CurrentEnvironmentId =>
            CurrentEncounter != null ? CurrentEncounter.EnvironmentId : string.Empty;

        public string CurrentEnvironmentSceneName =>
            CurrentEncounter != null ? CurrentEncounter.EnvironmentSceneName : string.Empty;

        public EncounterRewardConfig CurrentRewardConfig =>
            CurrentEncounter != null ? CurrentEncounter.RewardConfig : null;

        public int CurrentValidEnemyCount =>
            CurrentEncounter != null ? CurrentEncounter.GetValidEnemyCount() : 0;

        private void Start()
        {
            if (!selectDefaultOnStart)
                return;

            if (defaultEncounter != null)
            {
                TrySelectEncounter(defaultEncounter);
                return;
            }

            if (!string.IsNullOrWhiteSpace(defaultEncounterId))
            {
                TrySelectEncounterById(defaultEncounterId);
                return;
            }

            if (verboseLogs)
            {
                Debug.LogWarning(
                    "[RuntimeEncounterContext] Select Default On Start is enabled, " +
                    "but no default encounter or default encounter ID is assigned.");
            }
        }

        public bool TrySelectEncounter(EncounterData encounter)
        {
            if (encounter == null)
            {
                LastValidationError = "Encounter asset is null.";
                IsCurrentEncounterValid = false;

                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RuntimeEncounterContext] Cannot select encounter: Encounter asset is null.");
                }

                return false;
            }

            if (!encounter.IsRuntimeValid(out string error))
            {
                IsCurrentEncounterValid = false;
                LastValidationError = error;

                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[RuntimeEncounterContext] Cannot select encounter: {error}");
                }

                return false;
            }

            CurrentEncounter = encounter;
            CurrentEncounterId = encounter.EncounterId;
            IsCurrentEncounterValid = true;
            LastValidationError = string.Empty;
            SelectionCount++;

            OnEncounterSelected?.Invoke(CurrentEncounter);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RuntimeEncounterContext] Selected encounter: " +
                    $"{CurrentEncounterId} ({encounter.DisplayName})");
            }

            return true;
        }

        public bool TrySelectEncounterById(string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RuntimeEncounterContext] Cannot select encounter: Encounter ID is blank.");
                }

                return false;
            }

            if (encounterCatalog == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RuntimeEncounterContext] Cannot select encounter: EncounterCatalog is missing.");
                }

                return false;
            }

            if (!encounterCatalog.TryGetEncounter(encounterId, out EncounterData encounter))
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[RuntimeEncounterContext] Cannot select encounter: " +
                        $"Encounter ID '{encounterId}' was not found in catalog.");
                }

                return false;
            }

            return TrySelectEncounter(encounter);
        }

        public void ClearCurrentEncounter()
        {
            if (CurrentEncounter == null)
                return;

            CurrentEncounter = null;
            CurrentEncounterId = string.Empty;
            IsCurrentEncounterValid = false;
            LastValidationError = string.Empty;
            ClearCount++;

            OnEncounterCleared?.Invoke();

            if (verboseLogs)
                Debug.Log("[RuntimeEncounterContext] Current encounter cleared.");
        }

        public bool ValidateCurrentEncounter()
        {
            if (CurrentEncounter == null)
            {
                IsCurrentEncounterValid = false;
                LastValidationError = "No current encounter is selected.";
                return false;
            }

            bool isValid = CurrentEncounter.IsRuntimeValid(out string error);
            IsCurrentEncounterValid = isValid;
            LastValidationError = isValid ? string.Empty : error;
            return isValid;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!selectDefaultOnStart)
                return;

            if (defaultEncounter == null && string.IsNullOrWhiteSpace(defaultEncounterId))
            {
                Debug.LogWarning(
                    "[RuntimeEncounterContext] Select Default On Start is enabled, " +
                    "but both Default Encounter and Default Encounter ID are blank.",
                    this);
            }

            if (defaultEncounter != null && !defaultEncounter.IsRuntimeValid(out string encounterError))
            {
                Debug.LogWarning(
                    $"[RuntimeEncounterContext] Default Encounter '{defaultEncounter.name}' is invalid: {encounterError}",
                    this);
            }

            if (!string.IsNullOrWhiteSpace(defaultEncounterId) && encounterCatalog == null)
            {
                Debug.LogWarning(
                    "[RuntimeEncounterContext] Default Encounter ID is set, but Encounter Catalog is missing.",
                    this);
            }
        }
#endif
    }
}
```

## FILE: RuntimeEncounterContextDebugTest.cs
**Path:** `Assets/Scripts/Run/Encounters/Debug/RuntimeEncounterContextDebugTest.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    public class RuntimeEncounterContextDebugTest : MonoBehaviour
    {
        [SerializeField] private RuntimeEncounterContext context;
        [SerializeField] private EncounterData encounterToSelect;
        [SerializeField] private string encounterIdToSelect;

        public int SelectedEventCount { get; private set; }
        public int ClearedEventCount { get; private set; }

        private RuntimeEncounterContext subscribedContext;

        private void OnEnable()
        {
            RefreshSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeContext();
        }

        private void RefreshSubscription()
        {
            UnsubscribeContext();

            if (context == null)
                return;

            subscribedContext = context;
            subscribedContext.OnEncounterSelected += HandleEncounterSelected;
            subscribedContext.OnEncounterCleared += HandleEncounterCleared;
        }

        private void UnsubscribeContext()
        {
            if (subscribedContext == null)
                return;

            subscribedContext.OnEncounterSelected -= HandleEncounterSelected;
            subscribedContext.OnEncounterCleared -= HandleEncounterCleared;
            subscribedContext = null;
        }

        private void HandleEncounterSelected(EncounterData encounter)
        {
            SelectedEventCount++;
            Debug.Log(
                $"[RuntimeEncounterContextDebugTest] OnEncounterSelected " +
                $"(count={SelectedEventCount}) | Id={encounter?.EncounterId}");
        }

        private void HandleEncounterCleared()
        {
            ClearedEventCount++;
            Debug.Log(
                $"[RuntimeEncounterContextDebugTest] OnEncounterCleared " +
                $"(count={ClearedEventCount}).");
        }

        [ContextMenu("Debug Select Encounter Asset")]
        private void DebugSelectEncounterAsset()
        {
            if (!TryGetContext())
                return;

            if (encounterToSelect == null)
            {
                Debug.LogError(
                    "[RuntimeEncounterContextDebugTest] encounterToSelect reference is missing.");
                return;
            }

            bool selected = context.TrySelectEncounter(encounterToSelect);
            Debug.Log($"[RuntimeEncounterContextDebugTest] TrySelectEncounter => {selected}");
            DebugPrintContext();
        }

        [ContextMenu("Debug Select Encounter By ID")]
        private void DebugSelectEncounterById()
        {
            if (!TryGetContext())
                return;

            bool selected = context.TrySelectEncounterById(encounterIdToSelect);
            Debug.Log($"[RuntimeEncounterContextDebugTest] TrySelectEncounterById => {selected}");
            DebugPrintContext();
        }

        [ContextMenu("Debug Validate Current Encounter")]
        private void DebugValidateCurrentEncounter()
        {
            if (!TryGetContext())
                return;

            bool isValid = context.ValidateCurrentEncounter();
            Debug.Log($"[RuntimeEncounterContextDebugTest] ValidateCurrentEncounter => {isValid}");
            DebugPrintContext();
        }

        [ContextMenu("Debug Clear Current Encounter")]
        private void DebugClearCurrentEncounter()
        {
            if (!TryGetContext())
                return;

            context.ClearCurrentEncounter();
            DebugPrintContext();
        }

        [ContextMenu("Debug Print Runtime Encounter Context")]
        private void DebugPrintContext()
        {
            if (!TryGetContext())
                return;

            EncounterData encounter = context.CurrentEncounter;

            Debug.Log(
                $"[RuntimeEncounterContextDebugTest] --- Runtime Encounter Context ---\n" +
                $"HasCurrentEncounter={context.HasCurrentEncounter}\n" +
                $"CurrentEncounterId={context.CurrentEncounterId}\n" +
                $"DisplayName={(encounter != null ? encounter.DisplayName : "n/a")}\n" +
                $"EncounterType={context.CurrentEncounterType}\n" +
                $"EnvironmentId={context.CurrentEnvironmentId}\n" +
                $"EnvironmentSceneName={context.CurrentEnvironmentSceneName}\n" +
                $"RewardConfig={(context.CurrentRewardConfig != null ? context.CurrentRewardConfig.name : "null")}\n" +
                $"ValidEnemyCount={context.CurrentValidEnemyCount}\n" +
                $"IsCurrentEncounterValid={context.IsCurrentEncounterValid}\n" +
                $"LastValidationError={context.LastValidationError}\n" +
                $"SelectionCount={context.SelectionCount}\n" +
                $"ClearCount={context.ClearCount}\n" +
                $"SelectedEventCount={SelectedEventCount}\n" +
                $"ClearedEventCount={ClearedEventCount}");
        }

        private bool TryGetContext()
        {
            if (context != null)
                return true;

            Debug.LogError(
                "[RuntimeEncounterContextDebugTest] RuntimeEncounterContext reference is missing.");
            return false;
        }
    }
}
```

## FILE: EncounterEnemySceneBinder.cs
**Path:** `Assets/Scripts/Run/Encounters/Runtime/EncounterEnemySceneBinder.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Binds EncounterData enemy entries onto pre-placed scene EnemyBattleUnit objects by slot ID.
    /// Does not spawn enemies or load scenes.
    /// </summary>
    public class EncounterEnemySceneBinder : MonoBehaviour
    {
        [Header("Encounter Source")]
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;

        [Header("Battle Systems")]
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Scene Enemy Slots")]
        [SerializeField] private List<EncounterEnemySlotBinding> slotBindings =
            new List<EncounterEnemySlotBinding>();

        [Header("Options")]
        [SerializeField] private bool applyOnStart;
        [SerializeField] private bool disableUnusedEnemyObjects = true;
        [SerializeField] private bool registerBoundEnemiesToEnemyActionSystem = true;
        [SerializeField] private bool verboseLogs;

        public bool HasAppliedEncounterEnemies { get; private set; }
        public int ApplyCount { get; private set; }
        public int LastBoundEnemyCount { get; private set; }
        public int LastMissingSlotCount { get; private set; }
        public int LastUnusedSceneEnemyCount { get; private set; }
        public string LastApplyError { get; private set; } = string.Empty;

        public event Action OnEncounterEnemiesApplied;

        private readonly List<EncounterEnemyEntry> validEntryScratch = new List<EncounterEnemyEntry>();
        private readonly List<EnemyBattleUnit> boundEnemyScratch = new List<EnemyBattleUnit>();
        private readonly HashSet<string> validationSlotScratch = new HashSet<string>(StringComparer.Ordinal);

        private void Start()
        {
            if (applyOnStart)
                TryApplyCurrentEncounterEnemies();
        }

        public bool TryApplyCurrentEncounterEnemies()
        {
            if (runtimeEncounterContext == null)
            {
                return FailApply("RuntimeEncounterContext reference is missing.");
            }

            if (!runtimeEncounterContext.HasCurrentEncounter)
            {
                return FailApply("No current encounter is selected.");
            }

            if (!runtimeEncounterContext.IsCurrentEncounterValid)
            {
                return FailApply("Current encounter is not valid.");
            }

            return TryApplyEncounter(runtimeEncounterContext.CurrentEncounter);
        }

        public bool TryApplyEncounter(EncounterData encounter)
        {
            ResetAttemptDiagnostics();

            if (encounter == null)
                return FailApply("Encounter asset is null.");

            if (!encounter.IsRuntimeValid(out string encounterError))
                return FailApply(encounterError);

            if (slotBindings == null || slotBindings.Count == 0)
                return FailApply("No scene enemy slot bindings are configured.");

            if (!ValidateSlotBindingsConfiguration(out string bindingError))
                return FailApply(bindingError);

            if (registerBoundEnemiesToEnemyActionSystem && enemyActionSystem == null)
                return FailApply("EnemyActionSystem reference is missing.");

            validEntryScratch.Clear();
            encounter.GetValidEnemyEntries(validEntryScratch);

            if (validEntryScratch.Count == 0)
                return FailApply("Encounter has no valid enemy entries.");

            boundEnemyScratch.Clear();
            int missingSlotCount = 0;
            string firstMissingSlotId = null;

            for (int i = 0; i < validEntryScratch.Count; i++)
            {
                EncounterEnemyEntry entry = validEntryScratch[i];
                if (entry == null || !entry.IsValid)
                    return FailApply($"Encounter enemy entry at index {i} is invalid.");

                if (!TryGetSlotBinding(entry.SlotId, out EncounterEnemySlotBinding binding))
                {
                    missingSlotCount++;
                    if (firstMissingSlotId == null)
                        firstMissingSlotId = entry.SlotId;
                    continue;
                }

                boundEnemyScratch.Add(binding.EnemyUnit);
            }

            if (missingSlotCount > 0)
            {
                LastMissingSlotCount = missingSlotCount;
                if (missingSlotCount == 1)
                {
                    return FailApply(
                        $"No scene binding found for encounter slot '{firstMissingSlotId}'.");
                }

                return FailApply(
                    $"Encounter has {missingSlotCount} enemy slot(s) with no matching scene binding.");
            }

            for (int i = 0; i < boundEnemyScratch.Count; i++)
            {
                EnemyBattleUnit unit = boundEnemyScratch[i];
                EncounterEnemyEntry entry = validEntryScratch[i];
                unit.BindEnemyData(entry.EnemyData);
                unit.gameObject.SetActive(true);
            }

            int unusedCount = 0;
            for (int i = 0; i < slotBindings.Count; i++)
            {
                EncounterEnemySlotBinding binding = slotBindings[i];
                if (binding == null || !binding.IsValid)
                    continue;

                EnemyBattleUnit unit = binding.EnemyUnit;
                if (boundEnemyScratch.Contains(unit))
                    continue;

                unusedCount++;
                if (disableUnusedEnemyObjects)
                    unit.gameObject.SetActive(false);
            }

            if (registerBoundEnemiesToEnemyActionSystem)
                enemyActionSystem.ReplaceRegisteredEnemies(boundEnemyScratch);

            HasAppliedEncounterEnemies = true;
            ApplyCount++;
            LastBoundEnemyCount = boundEnemyScratch.Count;
            LastMissingSlotCount = 0;
            LastUnusedSceneEnemyCount = unusedCount;
            LastApplyError = string.Empty;

            OnEncounterEnemiesApplied?.Invoke();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[EncounterEnemySceneBinder] Applied encounter '{encounter.EncounterId}'. " +
                    $"Bound={LastBoundEnemyCount} | Unused={LastUnusedSceneEnemyCount}");
            }

            return true;
        }

        public void ClearAppliedEncounterEnemies()
        {
            HasAppliedEncounterEnemies = false;
            LastBoundEnemyCount = 0;
            LastMissingSlotCount = 0;
            LastUnusedSceneEnemyCount = 0;
            LastApplyError = string.Empty;

            if (enemyActionSystem != null)
                enemyActionSystem.ClearRegisteredEnemies();

            if (disableUnusedEnemyObjects && slotBindings != null)
            {
                for (int i = 0; i < slotBindings.Count; i++)
                {
                    EncounterEnemySlotBinding binding = slotBindings[i];
                    if (binding == null || binding.EnemyUnit == null)
                        continue;

                    binding.EnemyUnit.gameObject.SetActive(false);
                }
            }

            if (verboseLogs)
                Debug.Log("[EncounterEnemySceneBinder] Cleared applied encounter enemies.");
        }

        private bool ValidateSlotBindingsConfiguration(out string error)
        {
            error = string.Empty;
            validationSlotScratch.Clear();
            var usedUnits = new HashSet<EnemyBattleUnit>();

            for (int i = 0; i < slotBindings.Count; i++)
            {
                EncounterEnemySlotBinding binding = slotBindings[i];
                if (binding == null)
                {
                    error = $"Scene slot binding at index {i} is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(binding.SlotId))
                {
                    error = $"Scene slot binding at index {i} has a blank slot ID.";
                    return false;
                }

                if (binding.EnemyUnit == null)
                {
                    error = $"Scene slot binding '{binding.SlotId}' has no EnemyBattleUnit.";
                    return false;
                }

                if (!validationSlotScratch.Add(binding.SlotId))
                {
                    error = $"Duplicate scene slot ID '{binding.SlotId}'.";
                    return false;
                }

                if (!usedUnits.Add(binding.EnemyUnit))
                {
                    error = $"EnemyBattleUnit '{binding.EnemyUnit.name}' is assigned to multiple slots.";
                    return false;
                }
            }

            return true;
        }

        private bool TryGetSlotBinding(string slotId, out EncounterEnemySlotBinding binding)
        {
            binding = null;

            if (string.IsNullOrWhiteSpace(slotId) || slotBindings == null)
                return false;

            for (int i = 0; i < slotBindings.Count; i++)
            {
                EncounterEnemySlotBinding candidate = slotBindings[i];
                if (candidate == null || !candidate.IsValid)
                    continue;

                if (string.Equals(candidate.SlotId, slotId, StringComparison.Ordinal))
                {
                    binding = candidate;
                    return true;
                }
            }

            return false;
        }

        private void ResetAttemptDiagnostics()
        {
            LastBoundEnemyCount = 0;
            LastMissingSlotCount = 0;
            LastUnusedSceneEnemyCount = 0;
            LastApplyError = string.Empty;
        }

        private bool FailApply(string error)
        {
            LastApplyError = error;
            LastBoundEnemyCount = 0;

            if (verboseLogs)
                Debug.LogWarning($"[EncounterEnemySceneBinder] Apply failed: {error}");

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (applyOnStart && runtimeEncounterContext == null)
            {
                Debug.LogWarning(
                    "[EncounterEnemySceneBinder] Apply On Start is enabled, but RuntimeEncounterContext is missing.",
                    this);
            }

            if (registerBoundEnemiesToEnemyActionSystem && enemyActionSystem == null)
            {
                Debug.LogWarning(
                    "[EncounterEnemySceneBinder] Enemy registration is enabled, but EnemyActionSystem is missing.",
                    this);
            }

            if (slotBindings == null || slotBindings.Count == 0)
                return;

            var seenSlotIds = new HashSet<string>(StringComparer.Ordinal);
            var seenUnits = new HashSet<EnemyBattleUnit>();

            for (int i = 0; i < slotBindings.Count; i++)
            {
                EncounterEnemySlotBinding binding = slotBindings[i];
                if (binding == null)
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Null slot binding at index {i}.",
                        this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.SlotId))
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Slot binding at index {i} has a blank slot ID.",
                        this);
                }
                else if (!seenSlotIds.Add(binding.SlotId))
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Duplicate slot ID '{binding.SlotId}'.",
                        this);
                }

                if (binding.EnemyUnit == null)
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Slot '{binding.SlotId}' has no EnemyBattleUnit.",
                        this);
                }
                else if (!seenUnits.Add(binding.EnemyUnit))
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] EnemyBattleUnit '{binding.EnemyUnit.name}' is assigned to multiple slots.",
                        this);
                }
            }
        }
#endif
    }
}
```

## FILE: EncounterEnemySlotBinding.cs
**Path:** `Assets/Scripts/Run/Encounters/Runtime/EncounterEnemySlotBinding.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class EncounterEnemySlotBinding
    {
        [SerializeField] private string slotId;
        [SerializeField] private EnemyBattleUnit enemyUnit;

        public string SlotId => slotId;
        public EnemyBattleUnit EnemyUnit => enemyUnit;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(slotId) &&
            enemyUnit != null;
    }
}
```

## FILE: EncounterEnemySceneBinderDebugTest.cs
**Path:** `Assets/Scripts/Run/Encounters/Debug/EncounterEnemySceneBinderDebugTest.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    public class EncounterEnemySceneBinderDebugTest : MonoBehaviour
    {
        [SerializeField] private EncounterEnemySceneBinder binder;
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private EncounterData encounterToApply;

        [ContextMenu("Debug Apply Current Encounter Enemies")]
        private void DebugApplyCurrent()
        {
            if (!TryGetBinder())
                return;

            bool applied = binder.TryApplyCurrentEncounterEnemies();
            Debug.Log($"[EncounterEnemySceneBinderDebugTest] TryApplyCurrentEncounterEnemies => {applied}");
            DebugPrintState();
        }

        [ContextMenu("Debug Apply Specific Encounter Enemies")]
        private void DebugApplySpecific()
        {
            if (!TryGetBinder())
                return;

            if (encounterToApply == null)
            {
                Debug.LogError(
                    "[EncounterEnemySceneBinderDebugTest] encounterToApply reference is missing.");
                return;
            }

            bool applied = binder.TryApplyEncounter(encounterToApply);
            Debug.Log($"[EncounterEnemySceneBinderDebugTest] TryApplyEncounter => {applied}");
            DebugPrintState();
        }

        [ContextMenu("Debug Clear Applied Encounter Enemies")]
        private void DebugClear()
        {
            if (!TryGetBinder())
                return;

            binder.ClearAppliedEncounterEnemies();
            DebugPrintState();
        }

        [ContextMenu("Debug Print Enemy Binder State")]
        private void DebugPrintState()
        {
            if (!TryGetBinder())
                return;

            string currentEncounterId = runtimeEncounterContext != null
                ? runtimeEncounterContext.CurrentEncounterId
                : string.Empty;

            int enemySystemCount = enemyActionSystem != null
                ? enemyActionSystem.Enemies.Count
                : -1;

            Debug.Log(
                $"[EncounterEnemySceneBinderDebugTest] --- Enemy Binder State ---\n" +
                $"HasAppliedEncounterEnemies={binder.HasAppliedEncounterEnemies}\n" +
                $"ApplyCount={binder.ApplyCount}\n" +
                $"LastBoundEnemyCount={binder.LastBoundEnemyCount}\n" +
                $"LastMissingSlotCount={binder.LastMissingSlotCount}\n" +
                $"LastUnusedSceneEnemyCount={binder.LastUnusedSceneEnemyCount}\n" +
                $"LastApplyError={binder.LastApplyError}\n" +
                $"CurrentEncounterId={currentEncounterId}\n" +
                $"EnemyActionSystemCount={enemySystemCount}");

            PrintRegisteredEnemies();
        }

        private void PrintRegisteredEnemies()
        {
            if (enemyActionSystem == null)
            {
                Debug.Log(
                    "[EncounterEnemySceneBinderDebugTest] EnemyActionSystem reference is missing.");
                return;
            }

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBattleUnit enemy = enemies[i];
                if (enemy == null)
                {
                    Debug.Log($"[EncounterEnemySceneBinderDebugTest] Enemy[{i}]: null");
                    continue;
                }

                EnemyData data = enemy.Data;
                Debug.Log(
                    $"[EncounterEnemySceneBinderDebugTest] Enemy[{i}] | " +
                    $"Name={enemy.name} | " +
                    $"EnemyId={(data != null ? data.EnemyId : "null")} | " +
                    $"DisplayName={(data != null ? data.DisplayName : "n/a")} | " +
                    $"HP={enemy.CurrentHp}/{enemy.MaxHp} | " +
                    $"Behavior={enemy.Behavior} | " +
                    $"Countdown={enemy.CurrentCountdown} | " +
                    $"ActiveSelf={enemy.gameObject.activeSelf}");
            }
        }

        private bool TryGetBinder()
        {
            if (binder != null)
                return true;

            Debug.LogError(
                "[EncounterEnemySceneBinderDebugTest] EncounterEnemySceneBinder reference is missing.");
            return false;
        }
    }
}
```