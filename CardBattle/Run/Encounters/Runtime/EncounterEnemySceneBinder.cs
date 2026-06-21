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

            RefreshAllEnemyUIControllers(logAfterApply: true);

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

            RefreshAllEnemyUIControllers();

            if (verboseLogs)
                Debug.Log("[EncounterEnemySceneBinder] Cleared applied encounter enemies.");
        }

        private void RefreshAllEnemyUIControllers(bool logAfterApply = false)
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
