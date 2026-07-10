using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class EncounterDataDebugTest : MonoBehaviour
    {
        [SerializeField] private EncounterData encounterData;
        [SerializeField] private EncounterCatalog encounterCatalog;
        [SerializeField] private string lookupEncounterId;

        [ContextMenu("Debug Print Encounter")]
        private void DebugPrintEncounter()
        {
            if (encounterData == null)
            {
                Debug.LogError("[EncounterDataDebugTest] EncounterData reference is missing.");
                return;
            }

            bool isValid = encounterData.IsRuntimeValid(out string error);

            Debug.Log(
                $"[EncounterDataDebugTest] --- Encounter ---\n" +
                $"EncounterId={encounterData.EncounterId}\n" +
                $"DisplayName={encounterData.DisplayName}\n" +
                $"EnemySlotCount={encounterData.EnemySlotCount}\n" +
                $"ValidEnemySlotCount={encounterData.GetValidEnemySlotCount()}\n" +
                $"IsRuntimeValid={isValid}\n" +
                $"ValidationError={(isValid ? string.Empty : error)}");

            var sortedSlotScratch = new List<EncounterEnemySlotData>();
            encounterData.GetValidEnemySlots(sortedSlotScratch);

            for (int i = 0; i < sortedSlotScratch.Count; i++)
            {
                EncounterEnemySlotData slot = sortedSlotScratch[i];
                if (slot == null)
                    continue;

                Debug.Log(
                    $"[EncounterDataDebugTest] EnemySlot[{i}] | " +
                    $"SlotIndex={slot.SlotIndex} | " +
                    $"Prefab={(slot.EnemyPrefab != null ? slot.EnemyPrefab.name : "null")} | " +
                    $"EnemyData={(slot.EnemyData != null ? slot.EnemyData.name : "null")} | " +
                    $"DisplayName={(slot.EnemyData != null ? slot.EnemyData.DisplayName : "n/a")}");
            }
        }

        [ContextMenu("Debug Validate Encounter")]
        private void DebugValidateEncounter()
        {
            if (encounterData == null)
            {
                Debug.LogError("[EncounterDataDebugTest] EncounterData reference is missing.");
                return;
            }

            bool isValid = encounterData.IsRuntimeValid(out string error);
            Debug.Log(
                $"[EncounterDataDebugTest] Validate | " +
                $"IsRuntimeValid={isValid} | " +
                $"Error={(isValid ? "none" : error)}");
        }

        [ContextMenu("Debug Catalog Lookup")]
        private void DebugCatalogLookup()
        {
            if (encounterCatalog == null)
            {
                Debug.LogError("[EncounterDataDebugTest] EncounterCatalog reference is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lookupEncounterId))
            {
                Debug.LogWarning("[EncounterDataDebugTest] lookupEncounterId is blank.");
                return;
            }

            bool found = encounterCatalog.TryGetEncounter(lookupEncounterId, out EncounterData foundEncounter);
            Debug.Log(
                $"[EncounterDataDebugTest] Catalog Lookup | " +
                $"Id='{lookupEncounterId}' | Found={found} | " +
                $"Asset={(foundEncounter != null ? foundEncounter.name : "null")} | " +
                $"ValidEncounters={encounterCatalog.CountValidEncounters()}");
        }
    }
}
