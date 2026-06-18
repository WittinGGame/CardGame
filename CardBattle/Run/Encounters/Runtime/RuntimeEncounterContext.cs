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
