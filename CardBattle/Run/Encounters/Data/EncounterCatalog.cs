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
