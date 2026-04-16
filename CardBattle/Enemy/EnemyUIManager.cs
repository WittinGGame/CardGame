using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyUIManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [Header("Scene References")]
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private Camera targetCamera;

        [Header("UI Spawn")]
        [SerializeField] private EnemyUIController enemyUIPrefab;
        [SerializeField] private Transform uiContainer;

        [Header("Options")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool clearBeforeBuild = true;
        [SerializeField] private bool verboseLogs = false;

        private readonly List<EnemyUIController> spawnedUIs = new List<EnemyUIController>();

        private void Start()
        {
            if (buildOnStart)
                RebuildUI();
        }

        [ContextMenu("Rebuild Enemy UI")]
        public void RebuildUI()
        {
            if (!ValidateReferences())
                return;

            if (clearBeforeBuild)
                ClearSpawnedUI();

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null)
                    continue;

                var ui = Instantiate(enemyUIPrefab, uiContainer);
                ui.name = $"EnemyUI_{i}_{enemy.name}";
                ui.SetTarget(enemy);
                SetupWorldToUIFollow(ui);

                spawnedUIs.Add(ui);

                if (verboseLogs)
                    Debug.Log($"[EnemyUIManager] Spawned UI for enemy: {enemy.name}");
            }
        }

        [ContextMenu("Clear Enemy UI")]
        public void ClearSpawnedUI()
        {
            for (int i = 0; i < spawnedUIs.Count; i++)
            {
                if (spawnedUIs[i] != null)
                    Destroy(spawnedUIs[i].gameObject);
            }

            spawnedUIs.Clear();
        }

        public EnemyUIController GetUIForEnemy(EnemyBattleUnit enemy)
        {
            if (enemy == null)
                return null;

            for (int i = 0; i < spawnedUIs.Count; i++)
            {
                if (spawnedUIs[i] == null)
                    continue;

                if (spawnedUIs[i].Target == enemy)
                    return spawnedUIs[i];
            }

            return null;
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (enemyActionSystem == null)
            {
                Debug.LogError("EnemyUIManager: EnemyActionSystem reference is missing.");
                valid = false;
            }

            if (enemyUIPrefab == null)
            {
                Debug.LogError("EnemyUIManager: Enemy UI Prefab reference is missing.");
                valid = false;
            }

            if (uiContainer == null)
            {
                Debug.LogError("EnemyUIManager: UI Container reference is missing.");
                valid = false;
            }

            if (parentCanvas == null)
            {
                Debug.LogError("EnemyUIManager: Parent Canvas reference is missing.");
                valid = false;
            }

            if (targetCamera == null)
            {
                Debug.LogError("EnemyUIManager: Target Camera reference is missing.");
                valid = false;
            }

            return valid;
        }

        private void SetupWorldToUIFollow(EnemyUIController ui)
        {
            if (ui == null)
                return;

            AssignFollow(ui.HpFollow);
            AssignFollow(ui.IntentFollow);
            AssignFollow(ui.BuffFollow);
        }

        private void AssignFollow(WorldToUIFollow follow)
        {
            if (follow == null)
                return;

            follow.SetParentCanvas(parentCanvas);
            follow.SetTargetCamera(targetCamera);
        }
    }
}