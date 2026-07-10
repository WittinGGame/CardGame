using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Spawns encounter enemy prefabs into scene slot anchors and binds EnemyData.
    /// </summary>
    public class EncounterEnemySceneBinder : MonoBehaviour
    {
        [Header("Encounter Source")]
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;

        [Header("Battle Systems")]
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;

        [Header("Spawn Slots")]
        [SerializeField] private bool autoDiscoverSpawnSlots = true;
        [SerializeField] private EnemySpawnSlot[] spawnSlots;

        [Header("UI")]
        [SerializeField] private EnemyUIManager enemyUIManager;

        [Header("Runtime Enemy Dependents")]
        [SerializeField] private BattleOutcomeController battleOutcomeController;
        [SerializeField] private BattleFloatingTextSpawner battleFloatingTextSpawner;
        [SerializeField] private BattlePresentationController battlePresentationController;

        [Header("Options")]
        [SerializeField] private bool applyOnStart;
        [SerializeField] private bool registerBoundEnemiesToEnemyActionSystem = true;
        [SerializeField] private bool verboseLogs;

        public bool HasAppliedEncounterEnemies { get; private set; }
        public int ApplyCount { get; private set; }
        public int LastBoundEnemyCount { get; private set; }
        public int LastMissingSlotCount { get; private set; }
        public string LastApplyError { get; private set; } = string.Empty;
        public IReadOnlyList<EnemyBattleUnit> SpawnedEnemies => spawnedEnemies;

        public event Action<IReadOnlyList<EnemyBattleUnit>> OnEncounterEnemiesApplied;

        private readonly List<EnemyBattleUnit> spawnedEnemies = new List<EnemyBattleUnit>();
        private readonly List<EnemyTargetHighlight> spawnedHighlights = new List<EnemyTargetHighlight>();
        private readonly List<EncounterEnemySlotData> validSlotScratch = new List<EncounterEnemySlotData>();
        private readonly List<EnemyBattleUnit> boundEnemyScratch = new List<EnemyBattleUnit>();
        private readonly Dictionary<int, EnemySpawnSlot> spawnSlotMapScratch =
            new Dictionary<int, EnemySpawnSlot>();

        private void Start()
        {
            if (applyOnStart)
                TryApplyCurrentEncounterEnemies();
        }

        public bool TryApplyCurrentEncounterEnemies()
        {
            if (runtimeEncounterContext == null)
                return FailApply("RuntimeEncounterContext reference is missing.");

            if (!runtimeEncounterContext.HasCurrentEncounter)
                return FailApply("No current encounter is selected.");

            if (!runtimeEncounterContext.IsCurrentEncounterValid)
                return FailApply("Current encounter is not valid.");

            return TryApplyEncounter(runtimeEncounterContext.CurrentEncounter);
        }

        public bool TryApplyEncounter(EncounterData encounter)
        {
            ResetAttemptDiagnostics();
            ClearAppliedEncounterEnemies();

            if (encounter == null)
                return FailApply("Encounter asset is null.");

            if (!encounter.IsRuntimeValid(out string encounterError))
                return FailApply(encounterError);

            if (registerBoundEnemiesToEnemyActionSystem && enemyActionSystem == null)
                return FailApply("EnemyActionSystem reference is missing.");

            return TryApplyEncounterEnemySlots(encounter);
        }

        public void ClearAppliedEncounterEnemies()
        {
            HasAppliedEncounterEnemies = false;
            LastBoundEnemyCount = 0;
            LastMissingSlotCount = 0;
            LastApplyError = string.Empty;

            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                EnemyBattleUnit enemy = spawnedEnemies[i];
                if (enemy == null)
                    continue;

                Destroy(enemy.gameObject);
            }

            spawnedEnemies.Clear();
            spawnedHighlights.Clear();

            if (enemyActionSystem != null)
                enemyActionSystem.ClearRegisteredEnemies();

            if (targetSelectionSystem != null)
                targetSelectionSystem.SetEnemyHighlights(Array.Empty<EnemyTargetHighlight>());

            RefreshRuntimeEnemyDependents();

            if (verboseLogs)
                Debug.Log("[EncounterEnemySceneBinder] Cleared spawned enemies.");
        }

        private bool TryApplyEncounterEnemySlots(EncounterData encounter)
        {
            validSlotScratch.Clear();
            encounter.GetValidEnemySlots(validSlotScratch);

            if (validSlotScratch.Count == 0)
                return FailApply("EncounterData requires at least one valid Enemy Slot.");

            if (!TryBuildSpawnSlotMap(out string spawnSlotError))
                return FailApply(spawnSlotError);

            boundEnemyScratch.Clear();
            int missingSlotCount = 0;
            int firstMissingSlotIndex = -1;

            for (int i = 0; i < validSlotScratch.Count; i++)
            {
                EncounterEnemySlotData slotData = validSlotScratch[i];
                if (slotData == null || !slotData.IsValid)
                    return FailApply($"Encounter enemy slot at index {i} is invalid.");

                if (!spawnSlotMapScratch.TryGetValue(slotData.SlotIndex, out EnemySpawnSlot spawnSlot) ||
                    spawnSlot == null)
                {
                    missingSlotCount++;
                    if (firstMissingSlotIndex < 0)
                        firstMissingSlotIndex = slotData.SlotIndex;

                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            $"[EncounterEnemySceneBinder] Failed: missing EnemySpawnSlot index {slotData.SlotIndex}.");
                    }

                    continue;
                }

                if (!TrySpawnEnemyAtSlot(slotData, spawnSlot, out EnemyBattleUnit spawnedEnemy))
                    continue;

                boundEnemyScratch.Add(spawnedEnemy);
            }

            if (boundEnemyScratch.Count == 0)
            {
                if (missingSlotCount > 0)
                {
                    return FailApply(
                        missingSlotCount == 1
                            ? $"No EnemySpawnSlot found for index {firstMissingSlotIndex}."
                            : $"Encounter has {missingSlotCount} enemy slot(s) with no matching spawn anchor.");
                }

                return FailApply("No encounter enemies could be spawned.");
            }

            if (registerBoundEnemiesToEnemyActionSystem)
                enemyActionSystem.ReplaceRegisteredEnemies(boundEnemyScratch);

            RefreshRuntimeEnemyDependents(logAfterApply: true);

            HasAppliedEncounterEnemies = true;
            ApplyCount++;
            LastBoundEnemyCount = boundEnemyScratch.Count;
            LastMissingSlotCount = missingSlotCount;
            LastApplyError = string.Empty;

            OnEncounterEnemiesApplied?.Invoke(spawnedEnemies);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[EncounterEnemySceneBinder] Registered {LastBoundEnemyCount} enemies.");
            }

            return true;
        }

        private bool TrySpawnEnemyAtSlot(
            EncounterEnemySlotData slotData,
            EnemySpawnSlot spawnSlot,
            out EnemyBattleUnit spawnedEnemy)
        {
            spawnedEnemy = null;

            if (slotData == null || !slotData.IsValid)
            {
                if (verboseLogs)
                {
                    Debug.LogError(
                        "[EncounterEnemySceneBinder] Failed: enemy slot is invalid.");
                }

                return false;
            }

            Transform spawnRoot = spawnSlot.SpawnRoot;
            GameObject instance = Instantiate(slotData.EnemyPrefab.gameObject, spawnRoot);
            instance.transform.localPosition = slotData.LocalPositionOffset;
            instance.transform.localRotation = Quaternion.Euler(slotData.LocalEulerOffset);

            string dataName = slotData.EnemyData.name;
            instance.name = $"{slotData.EnemyPrefab.name}_Slot{slotData.SlotIndex}";

            spawnedEnemy = instance.GetComponentInChildren<EnemyBattleUnit>();
            if (spawnedEnemy == null)
            {
                Destroy(instance);

                if (verboseLogs)
                {
                    Debug.LogError(
                        "[EncounterEnemySceneBinder] Failed: prefab has no EnemyBattleUnit.");
                }

                return false;
            }

            spawnedEnemy.BindEnemyData(slotData.EnemyData);
            spawnedEnemy.gameObject.SetActive(true);

            BindSpawnedEnemyInteractions(spawnedEnemy, instance);

            spawnedEnemies.Add(spawnedEnemy);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[EncounterEnemySceneBinder] Spawned enemy slot={slotData.SlotIndex} " +
                    $"prefab={slotData.EnemyPrefab.name} data={dataName}.");
            }

            return true;
        }

        private void BindSpawnedEnemyInteractions(EnemyBattleUnit enemy, GameObject instanceRoot)
        {
            TargetableEnemy targetable = instanceRoot.GetComponentInChildren<TargetableEnemy>();
            if (targetable != null)
                targetable.Bind(enemy, targetSelectionSystem);

            EnemyTargetHighlight highlight = instanceRoot.GetComponentInChildren<EnemyTargetHighlight>();
            if (highlight != null)
            {
                highlight.Bind(enemy);
                spawnedHighlights.Add(highlight);
            }
        }

        private bool TryBuildSpawnSlotMap(out string error)
        {
            error = string.Empty;
            spawnSlotMapScratch.Clear();

            EnemySpawnSlot[] resolvedSlots = ResolveSpawnSlots();
            if (resolvedSlots == null || resolvedSlots.Length == 0)
            {
                error = "No EnemySpawnSlot anchors are configured in the scene.";
                return false;
            }

            for (int i = 0; i < resolvedSlots.Length; i++)
            {
                EnemySpawnSlot slot = resolvedSlots[i];
                if (slot == null)
                {
                    error = $"EnemySpawnSlot at index {i} is null.";
                    return false;
                }

                if (!spawnSlotMapScratch.TryAdd(slot.SlotIndex, slot))
                {
                    error = $"Duplicate EnemySpawnSlot index {slot.SlotIndex}.";
                    return false;
                }
            }

            return true;
        }

        private EnemySpawnSlot[] ResolveSpawnSlots()
        {
            if (spawnSlots != null && spawnSlots.Length > 0)
                return spawnSlots;

            if (!autoDiscoverSpawnSlots)
                return Array.Empty<EnemySpawnSlot>();

            return FindObjectsByType<EnemySpawnSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        private void RefreshTargetSelectionHighlights()
        {
            if (targetSelectionSystem == null)
                return;

            targetSelectionSystem.SetEnemyHighlights(spawnedHighlights.ToArray());
        }

        private void RefreshRuntimeEnemyDependents(bool logAfterApply = false)
        {
            RefreshTargetSelectionHighlights();
            RefreshEnemyPresentation(logAfterApply: false);

            if (verboseLogs && logAfterApply)
                Debug.Log("[EncounterEnemySceneBinder] Refreshed EnemyUIManager.");

            if (battleFloatingTextSpawner != null)
            {
                battleFloatingTextSpawner.RefreshSubscriptions();

                if (verboseLogs && logAfterApply)
                {
                    Debug.Log(
                        "[EncounterEnemySceneBinder] Refreshed BattleFloatingTextSpawner subscriptions.");
                }
            }

            if (battlePresentationController != null)
            {
                battlePresentationController.RefreshSubscriptions();

                if (verboseLogs && logAfterApply)
                {
                    Debug.Log(
                        "[EncounterEnemySceneBinder] Refreshed BattlePresentationController subscriptions.");
                }
            }

            if (battleOutcomeController != null)
            {
                battleOutcomeController.RefreshEnemyReferences();

                if (verboseLogs && logAfterApply)
                {
                    Debug.Log(
                        "[EncounterEnemySceneBinder] Refreshed BattleOutcomeController enemy references.");
                }
            }

            if (verboseLogs && logAfterApply)
            {
                Debug.Log("[EncounterEnemySceneBinder] Refreshed runtime enemy dependents.");
            }
        }

        private void RefreshEnemyPresentation(bool logAfterApply = false)
        {
            if (enemyUIManager != null)
            {
                enemyUIManager.RebuildUI();
            }
            else
            {
                EnemyUIController[] controllers = FindObjectsByType<EnemyUIController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                for (int i = 0; i < controllers.Length; i++)
                    controllers[i].RefreshNow();

                if (verboseLogs && logAfterApply && controllers.Length > 0)
                {
                    Debug.Log(
                        "[EncounterEnemySceneBinder] Refreshed enemy UI after applying encounter.");
                }
            }
        }

        private void ResetAttemptDiagnostics()
        {
            LastBoundEnemyCount = 0;
            LastMissingSlotCount = 0;
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
        }
#endif
    }
}
