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
        [SerializeField] private EncounterEnemySlotData[] enemySlots;
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

        public IReadOnlyList<EncounterEnemySlotData> EnemySlots => enemySlots;
        public IReadOnlyList<EncounterEnemyEntry> Enemies => enemies;
        public int EnemyCount => enemies != null ? enemies.Count : 0;
        public int EnemySlotCount => enemySlots != null ? enemySlots.Length : 0;

        public bool HasEnemySlotPrefabs => GetValidEnemySlotCount() > 0;

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

        public int GetValidEnemySlotCount()
        {
            if (enemySlots == null)
                return 0;

            int count = 0;
            for (int i = 0; i < enemySlots.Length; i++)
            {
                EncounterEnemySlotData slot = enemySlots[i];
                if (slot != null && slot.IsValid)
                    count++;
            }

            return count;
        }

        public void GetValidEnemySlots(List<EncounterEnemySlotData> output)
        {
            if (output == null)
                return;

            output.Clear();

            if (enemySlots == null)
                return;

            for (int i = 0; i < enemySlots.Length; i++)
            {
                EncounterEnemySlotData slot = enemySlots[i];
                if (slot != null && slot.IsValid)
                    output.Add(slot);
            }

            output.Sort(CompareSlotIndex);
        }

        public int GetValidEnemyCount()
        {
            if (HasEnemySlotPrefabs)
                return GetValidEnemySlotCount();

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

            if (HasEnemySlotPrefabs)
                return IsEnemySlotsRuntimeValid(out error);

            return IsLegacyEnemyEntriesRuntimeValid(out error);
        }

        private bool IsEnemySlotsRuntimeValid(out string error)
        {
            error = string.Empty;

            if (enemySlots == null || enemySlots.Length == 0)
            {
                error = "Encounter has no valid enemy slot entries.";
                return false;
            }

            var seenSlotIndices = new HashSet<int>();
            int validCount = 0;

            for (int i = 0; i < enemySlots.Length; i++)
            {
                EncounterEnemySlotData slot = enemySlots[i];
                if (slot == null)
                {
                    error = $"Enemy slot at index {i} is null.";
                    return false;
                }

                if (!slot.IsValid)
                {
                    error = $"Enemy slot at index {i} has no EnemyData.";
                    return false;
                }

                if (!seenSlotIndices.Add(slot.SlotIndex))
                {
                    error = $"Duplicate enemy slot index {slot.SlotIndex}.";
                    return false;
                }

                validCount++;
            }

            if (validCount == 0)
            {
                error = "Encounter has no valid enemy slot entries.";
                return false;
            }

            return true;
        }

        private bool IsLegacyEnemyEntriesRuntimeValid(out string error)
        {
            error = string.Empty;

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

        private static int CompareSlotIndex(EncounterEnemySlotData a, EncounterEnemySlotData b)
        {
            if (ReferenceEquals(a, b))
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            return a.SlotIndex.CompareTo(b.SlotIndex);
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

            if (enemySlots != null && enemySlots.Length > 0)
            {
                var seenSlotIndices = new HashSet<int>();

                for (int i = 0; i < enemySlots.Length; i++)
                {
                    EncounterEnemySlotData slot = enemySlots[i];
                    if (slot == null)
                    {
                        Debug.LogWarning(
                            $"[EncounterData] Null enemy slot at index {i} in '{name}'.",
                            this);
                        continue;
                    }

                    if (!seenSlotIndices.Add(slot.SlotIndex))
                    {
                        Debug.LogWarning(
                            $"[EncounterData] Duplicate enemy slot index {slot.SlotIndex} in '{name}'.",
                            this);
                    }

                    if (slot.EnemyData == null)
                    {
                        Debug.LogWarning(
                            $"[EncounterData] Enemy slot at index {i} has no EnemyData in '{name}'.",
                            this);
                    }

                    if (slot.EnemyPrefab == null)
                    {
                        Debug.LogWarning(
                            $"[EncounterData] Enemy slot at index {i} has no enemy prefab in '{name}'.",
                            this);
                    }
                }

                return;
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
