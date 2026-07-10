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

        [Header("Enemy Slots")]
        [SerializeField] private EncounterEnemySlotData[] enemySlots;

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
        public int EnemySlotCount => enemySlots != null ? enemySlots.Length : 0;

        public bool HasEnemySlots => GetValidEnemySlotCount() > 0;

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

        public int GetValidEnemyCount() => GetValidEnemySlotCount();

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

            return IsEnemySlotsRuntimeValid(out error);
        }

        private bool IsEnemySlotsRuntimeValid(out string error)
        {
            error = string.Empty;

            if (enemySlots == null || enemySlots.Length == 0)
            {
                error = "EncounterData requires at least one valid Enemy Slot.";
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

                if (slot.SlotIndex < 0)
                {
                    error = $"Enemy slot at index {i} has invalid slot index {slot.SlotIndex}.";
                    return false;
                }

                if (slot.EnemyData == null)
                {
                    error = $"Enemy slot at index {i} has no EnemyData.";
                    return false;
                }

                if (slot.EnemyPrefab == null)
                {
                    error = $"Enemy slot at index {i} has no enemy prefab.";
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
                error = "EncounterData requires at least one valid Enemy Slot.";
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

            if (enemySlots == null || enemySlots.Length == 0)
            {
                Debug.LogWarning(
                    $"[EncounterData] EncounterData requires at least one valid Enemy Slot in '{name}'.",
                    this);
                return;
            }

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

                if (slot.SlotIndex < 0)
                {
                    Debug.LogWarning(
                        $"[EncounterData] Enemy slot at index {i} has invalid slot index in '{name}'.",
                        this);
                }
                else if (!seenSlotIndices.Add(slot.SlotIndex))
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
        }
#endif
    }
}
