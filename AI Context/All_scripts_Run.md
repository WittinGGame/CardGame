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