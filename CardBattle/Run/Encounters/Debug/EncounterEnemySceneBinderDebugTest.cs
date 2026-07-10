using UnityEngine;

namespace CardBattle.Core
{
    public class EncounterEnemySceneBinderDebugTest : MonoBehaviour
    {
        [SerializeField] private EncounterEnemySceneBinder binder;
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private EncounterData encounterToApply;

        [ContextMenu("Debug Apply Current Encounter Enemies")]
        private void DebugApplyCurrent()
        {
            if (!TryGetBinder())
                return;

            bool applied = binder.TryApplyCurrentEncounterEnemies();
            Debug.Log($"[EncounterEnemySceneBinderDebugTest] TryApplyCurrentEncounterEnemies => {applied}");
            DebugPrintState();
        }

        [ContextMenu("Debug Apply Specific Encounter Enemies")]
        private void DebugApplySpecific()
        {
            if (!TryGetBinder())
                return;

            if (encounterToApply == null)
            {
                Debug.LogError(
                    "[EncounterEnemySceneBinderDebugTest] encounterToApply reference is missing.");
                return;
            }

            bool applied = binder.TryApplyEncounter(encounterToApply);
            Debug.Log($"[EncounterEnemySceneBinderDebugTest] TryApplyEncounter => {applied}");
            DebugPrintState();
        }

        [ContextMenu("Debug Clear Applied Encounter Enemies")]
        private void DebugClear()
        {
            if (!TryGetBinder())
                return;

            binder.ClearAppliedEncounterEnemies();
            DebugPrintState();
        }

        [ContextMenu("Debug Print Enemy Binder State")]
        private void DebugPrintState()
        {
            if (!TryGetBinder())
                return;

            string currentEncounterId = runtimeEncounterContext != null
                ? runtimeEncounterContext.CurrentEncounterId
                : string.Empty;

            int enemySystemCount = enemyActionSystem != null
                ? enemyActionSystem.Enemies.Count
                : -1;

            Debug.Log(
                $"[EncounterEnemySceneBinderDebugTest] --- Enemy Binder State ---\n" +
                $"HasAppliedEncounterEnemies={binder.HasAppliedEncounterEnemies}\n" +
                $"ApplyCount={binder.ApplyCount}\n" +
                $"LastBoundEnemyCount={binder.LastBoundEnemyCount}\n" +
                $"LastMissingSlotCount={binder.LastMissingSlotCount}\n" +
                $"LastApplyError={binder.LastApplyError}\n" +
                $"SpawnedEnemyCount={binder.SpawnedEnemies.Count}\n" +
                $"RegisteredEnemyCount={enemySystemCount}\n" +
                $"CurrentEncounterId={currentEncounterId}");

            PrintRegisteredEnemies();
        }

        private void PrintRegisteredEnemies()
        {
            if (enemyActionSystem == null)
            {
                Debug.Log(
                    "[EncounterEnemySceneBinderDebugTest] EnemyActionSystem reference is missing.");
                return;
            }

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBattleUnit enemy = enemies[i];
                if (enemy == null)
                {
                    Debug.Log($"[EncounterEnemySceneBinderDebugTest] Enemy[{i}]: null");
                    continue;
                }

                EnemyData data = enemy.Data;
                Debug.Log(
                    $"[EncounterEnemySceneBinderDebugTest] Enemy[{i}] | " +
                    $"Name={enemy.name} | " +
                    $"EnemyId={(data != null ? data.EnemyId : "null")} | " +
                    $"DisplayName={(data != null ? data.DisplayName : "n/a")} | " +
                    $"HP={enemy.CurrentHp}/{enemy.MaxHp} | " +
                    $"Behavior={enemy.Behavior} | " +
                    $"Countdown={enemy.CurrentCountdown} | " +
                    $"ActiveSelf={enemy.gameObject.activeSelf}");
            }
        }

        private bool TryGetBinder()
        {
            if (binder != null)
                return true;

            Debug.LogError(
                "[EncounterEnemySceneBinderDebugTest] EncounterEnemySceneBinder reference is missing.");
            return false;
        }
    }
}
