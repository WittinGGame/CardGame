using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CardBattle.Core
{
    /// <summary>
    /// Spawns encounter enemy prefabs into scene slot anchors and binds EnemyData,
    /// or falls back to legacy pre-placed scene enemy binding by slot ID.
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

        [Header("Legacy Scene Enemy Slots")]
        [FormerlySerializedAs("slotBindings")]
        [SerializeField] private List<EncounterEnemySlotBinding> legacySlotBindings =
            new List<EncounterEnemySlotBinding>();

        [Header("UI")]
        [SerializeField] private EnemyUIManager enemyUIManager;

        [Header("Runtime Enemy Dependents")]
        [SerializeField] private BattleOutcomeController battleOutcomeController;
        [SerializeField] private BattleFloatingTextSpawner battleFloatingTextSpawner;
        [SerializeField] private BattlePresentationController battlePresentationController;

        [Header("Options")]
        [SerializeField] private bool applyOnStart;
        [FormerlySerializedAs("disableUnusedEnemyObjects")]
        [SerializeField] private bool disableUnusedLegacyEnemyObjects = true;
        [SerializeField] private bool registerBoundEnemiesToEnemyActionSystem = true;
        [SerializeField] private bool verboseLogs;

        public bool HasAppliedEncounterEnemies { get; private set; }
        public int ApplyCount { get; private set; }
        public int LastBoundEnemyCount { get; private set; }
        public int LastMissingSlotCount { get; private set; }
        public int LastUnusedSceneEnemyCount { get; private set; }
        public string LastApplyError { get; private set; } = string.Empty;
        public IReadOnlyList<EnemyBattleUnit> SpawnedEnemies => spawnedEnemies;

        public event Action<IReadOnlyList<EnemyBattleUnit>> OnEncounterEnemiesApplied;

        private readonly List<EnemyBattleUnit> spawnedEnemies = new List<EnemyBattleUnit>();
        private readonly List<EnemyTargetHighlight> spawnedHighlights = new List<EnemyTargetHighlight>();
        private readonly List<EncounterEnemyEntry> validEntryScratch = new List<EncounterEnemyEntry>();
        private readonly List<EncounterEnemySlotData> validSlotScratch = new List<EncounterEnemySlotData>();
        private readonly List<EnemyBattleUnit> boundEnemyScratch = new List<EnemyBattleUnit>();
        private readonly HashSet<string> validationSlotScratch = new HashSet<string>(StringComparer.Ordinal);
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

            if (encounter.HasEnemySlotPrefabs)
                return TryApplyEncounterEnemySlots(encounter);

            return TryApplyLegacyEncounterEnemies(encounter);
        }

        public void ClearAppliedEncounterEnemies()
        {
            HasAppliedEncounterEnemies = false;
            LastBoundEnemyCount = 0;
            LastMissingSlotCount = 0;
            LastUnusedSceneEnemyCount = 0;
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

            if (disableUnusedLegacyEnemyObjects && legacySlotBindings != null)
            {
                for (int i = 0; i < legacySlotBindings.Count; i++)
                {
                    EncounterEnemySlotBinding binding = legacySlotBindings[i];
                    if (binding == null || binding.EnemyUnit == null)
                        continue;

                    binding.EnemyUnit.gameObject.SetActive(false);
                }
            }

            RefreshRuntimeEnemyDependents();

            if (verboseLogs)
                Debug.Log("[EncounterEnemySceneBinder] Cleared spawned enemies.");
        }

        private bool TryApplyEncounterEnemySlots(EncounterData encounter)
        {
            validSlotScratch.Clear();
            encounter.GetValidEnemySlots(validSlotScratch);

            if (validSlotScratch.Count == 0)
                return FailApply("Encounter has no valid enemy slot entries.");

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

                if (!slotData.HasPrefab)
                {
                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            $"[EncounterEnemySceneBinder] Failed: missing prefab for slot index {slotData.SlotIndex}.");
                    }

                    continue;
                }

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
            LastUnusedSceneEnemyCount = 0;
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
                        "[EncounterEnemySceneBinder] Failed: enemy slot has no EnemyData.");
                }

                return false;
            }

            if (!slotData.HasPrefab)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Failed: missing prefab for slot index {slotData.SlotIndex}.");
                }

                return false;
            }

            Transform spawnRoot = spawnSlot.SpawnRoot;
            GameObject instance = Instantiate(slotData.EnemyPrefab.gameObject, spawnRoot);
            instance.transform.localPosition = slotData.LocalPositionOffset;
            instance.transform.localRotation = Quaternion.Euler(slotData.LocalEulerOffset);

            string dataName = slotData.EnemyData != null ? slotData.EnemyData.name : "null";
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

        private bool TryApplyLegacyEncounterEnemies(EncounterData encounter)
        {
            if (legacySlotBindings == null || legacySlotBindings.Count == 0)
                return FailApply("No scene enemy slot bindings are configured.");

            if (!ValidateLegacySlotBindingsConfiguration(out string bindingError))
                return FailApply(bindingError);

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

                if (!TryGetLegacySlotBinding(entry.SlotId, out EncounterEnemySlotBinding binding))
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
            for (int i = 0; i < legacySlotBindings.Count; i++)
            {
                EncounterEnemySlotBinding binding = legacySlotBindings[i];
                if (binding == null || !binding.IsValid)
                    continue;

                EnemyBattleUnit unit = binding.EnemyUnit;
                if (boundEnemyScratch.Contains(unit))
                    continue;

                unusedCount++;
                if (disableUnusedLegacyEnemyObjects)
                    unit.gameObject.SetActive(false);
            }

            if (registerBoundEnemiesToEnemyActionSystem)
                enemyActionSystem.ReplaceRegisteredEnemies(boundEnemyScratch);

            RefreshRuntimeEnemyDependents(logAfterApply: true);

            HasAppliedEncounterEnemies = true;
            ApplyCount++;
            LastBoundEnemyCount = boundEnemyScratch.Count;
            LastMissingSlotCount = 0;
            LastUnusedSceneEnemyCount = unusedCount;
            LastApplyError = string.Empty;

            OnEncounterEnemiesApplied?.Invoke(boundEnemyScratch);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[EncounterEnemySceneBinder] Applied legacy encounter '{encounter.EncounterId}'. " +
                    $"Bound={LastBoundEnemyCount} | Unused={LastUnusedSceneEnemyCount}");
            }

            return true;
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

        private void RefreshLegacyTargetSelectionHighlights(IReadOnlyList<EnemyBattleUnit> enemies)
        {
            if (targetSelectionSystem == null || enemies == null)
                return;

            var highlights = new List<EnemyTargetHighlight>(enemies.Count);
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBattleUnit enemy = enemies[i];
                if (enemy == null)
                    continue;

                EnemyTargetHighlight highlight = enemy.GetComponentInChildren<EnemyTargetHighlight>();
                if (highlight != null)
                    highlights.Add(highlight);
            }

            targetSelectionSystem.SetEnemyHighlights(highlights.ToArray());
        }

        private void RefreshRuntimeEnemyDependents(bool logAfterApply = false)
        {
            if (spawnedHighlights.Count > 0)
                RefreshTargetSelectionHighlights();
            else if (enemyActionSystem != null)
                RefreshLegacyTargetSelectionHighlights(enemyActionSystem.Enemies);

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

        private bool ValidateLegacySlotBindingsConfiguration(out string error)
        {
            error = string.Empty;
            validationSlotScratch.Clear();
            var usedUnits = new HashSet<EnemyBattleUnit>();

            for (int i = 0; i < legacySlotBindings.Count; i++)
            {
                EncounterEnemySlotBinding binding = legacySlotBindings[i];
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

        private bool TryGetLegacySlotBinding(string slotId, out EncounterEnemySlotBinding binding)
        {
            binding = null;

            if (string.IsNullOrWhiteSpace(slotId) || legacySlotBindings == null)
                return false;

            for (int i = 0; i < legacySlotBindings.Count; i++)
            {
                EncounterEnemySlotBinding candidate = legacySlotBindings[i];
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

            if (legacySlotBindings == null || legacySlotBindings.Count == 0)
                return;

            var seenSlotIds = new HashSet<string>(StringComparer.Ordinal);
            var seenUnits = new HashSet<EnemyBattleUnit>();

            for (int i = 0; i < legacySlotBindings.Count; i++)
            {
                EncounterEnemySlotBinding binding = legacySlotBindings[i];
                if (binding == null)
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Null legacy slot binding at index {i}.",
                        this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.SlotId))
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Legacy slot binding at index {i} has a blank slot ID.",
                        this);
                }
                else if (!seenSlotIds.Add(binding.SlotId))
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Duplicate legacy slot ID '{binding.SlotId}'.",
                        this);
                }

                if (binding.EnemyUnit == null)
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] Legacy slot '{binding.SlotId}' has no EnemyBattleUnit.",
                        this);
                }
                else if (!seenUnits.Add(binding.EnemyUnit))
                {
                    Debug.LogWarning(
                        $"[EncounterEnemySceneBinder] EnemyBattleUnit '{binding.EnemyUnit.name}' is assigned to multiple legacy slots.",
                        this);
                }
            }
        }
#endif
    }
}
