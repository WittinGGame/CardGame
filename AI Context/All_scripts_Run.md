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