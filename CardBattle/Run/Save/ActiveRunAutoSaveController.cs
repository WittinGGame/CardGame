using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    public class ActiveRunAutoSaveController : MonoBehaviour
    {
        [Header("Save")]
        [SerializeField] private ActiveRunSaveService saveService;

        [Header("Run")]
        [SerializeField] private RunManager runManager;

        [Header("Map")]
        [SerializeField] private MapRuntimeController mapRuntimeController;

        [Header("Flow")]
        [SerializeField] private EncounterCompletionController encounterCompletionController;
        [SerializeField] private RunEndController runEndController;

        [Header("Options")]
        [SerializeField] private bool saveOnRunStarted;
        [SerializeField] private bool saveOnEncounterCompleted = true;
        [SerializeField] private bool deleteSaveOnRunEnded = true;
        [SerializeField] private bool verboseLogs = true;

        private RunManager subscribedRunManager;
        private EncounterCompletionController subscribedEncounterCompletion;
        private RunEndController subscribedRunEnd;

        private void OnEnable()
        {
            SubscribeRunManager();
            SubscribeEncounterCompletion();
            SubscribeRunEnd();
        }

        private void OnDisable()
        {
            UnsubscribeRunManager();
            UnsubscribeEncounterCompletion();
            UnsubscribeRunEnd();
        }

        private void OnDestroy()
        {
            UnsubscribeRunManager();
            UnsubscribeEncounterCompletion();
            UnsubscribeRunEnd();
        }

        public bool SaveNow(string reason)
        {
            RunManager manager = ResolveRunManager();
            if (manager == null || !manager.HasActiveRun)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[ActiveRunAutoSave] Save skipped. Reason={reason} | No active run.");
                }

                return false;
            }

            if (saveService == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[ActiveRunAutoSave] Save skipped. Reason={reason} | Save service missing.");
                }

                return false;
            }

            if (mapRuntimeController == null || !mapRuntimeController.HasInitialized)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[ActiveRunAutoSave] Save skipped. Reason={reason} | Map not initialized.");
                }

                return false;
            }

            RunState runSnapshot = manager.GetSnapshot();
            RunMapState mapSnapshot = mapRuntimeController.GetMapSnapshot();
            if (runSnapshot == null || mapSnapshot == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[ActiveRunAutoSave] Save skipped. Reason={reason} | Snapshot missing.");
                }

                return false;
            }

            ActiveRunSaveData saveData = ActiveRunSaveData.Create(
                runSnapshot,
                mapSnapshot,
                mapRuntimeController.CurrentActId,
                mapRuntimeController.SelectedNodeId);

            bool success = saveService.TrySave(saveData);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[ActiveRunAutoSave] Save requested. Reason={reason} | Success={success}");
            }

            return success;
        }

        public bool DeleteActiveSave(string reason)
        {
            if (saveService == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[ActiveRunAutoSave] Delete skipped. Reason={reason} | Save service missing.");
                }

                return false;
            }

            bool deleted = saveService.DeleteSave();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[ActiveRunAutoSave] Delete requested. Reason={reason} | Success={deleted}");
            }

            return deleted;
        }

        private void HandleRunStarted(RunState _)
        {
            if (!saveOnRunStarted)
                return;

            SaveNow("RunStarted");
        }

        private void HandleEncounterCompletionReady()
        {
            if (!saveOnEncounterCompleted)
                return;

            StartCoroutine(SaveAfterEncounterCompletionRoutine());
        }

        private IEnumerator SaveAfterEncounterCompletionRoutine()
        {
            yield return null;

            if (runEndController != null && runEndController.IsRunEnded)
                yield break;

            RunManager manager = ResolveRunManager();
            if (manager == null || !manager.HasActiveRun)
                yield break;

            SaveNow("EncounterCompleted");
        }

        private void HandleRunEnded(RunEndType endType)
        {
            if (!deleteSaveOnRunEnded)
                return;

            DeleteActiveSave($"RunEnded:{endType}");
        }

        private void SubscribeRunManager()
        {
            UnsubscribeRunManager();

            RunManager manager = ResolveRunManager();
            if (manager == null)
                return;

            subscribedRunManager = manager;
            subscribedRunManager.OnRunStarted += HandleRunStarted;
        }

        private void UnsubscribeRunManager()
        {
            if (subscribedRunManager == null)
                return;

            subscribedRunManager.OnRunStarted -= HandleRunStarted;
            subscribedRunManager = null;
        }

        private void SubscribeEncounterCompletion()
        {
            UnsubscribeEncounterCompletion();

            if (encounterCompletionController == null)
                return;

            subscribedEncounterCompletion = encounterCompletionController;
            subscribedEncounterCompletion.OnEncounterCompletionReady +=
                HandleEncounterCompletionReady;
        }

        private void UnsubscribeEncounterCompletion()
        {
            if (subscribedEncounterCompletion == null)
                return;

            subscribedEncounterCompletion.OnEncounterCompletionReady -=
                HandleEncounterCompletionReady;
            subscribedEncounterCompletion = null;
        }

        private void SubscribeRunEnd()
        {
            UnsubscribeRunEnd();

            if (runEndController == null)
                return;

            subscribedRunEnd = runEndController;
            subscribedRunEnd.OnRunEnded += HandleRunEnded;
        }

        private void UnsubscribeRunEnd()
        {
            if (subscribedRunEnd == null)
                return;

            subscribedRunEnd.OnRunEnded -= HandleRunEnded;
            subscribedRunEnd = null;
        }

        private RunManager ResolveRunManager()
        {
            if (runManager != null)
                return runManager;

            return RunManager.Instance;
        }
    }
}
