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
