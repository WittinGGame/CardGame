using System.IO;
using UnityEngine;

namespace CardBattle.Core
{
    public class ActiveRunSaveService : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "active_run.json";
        [SerializeField] private bool verboseLogs = true;

        public string SavePath =>
            string.IsNullOrWhiteSpace(saveFileName)
                ? string.Empty
                : Path.Combine(Application.persistentDataPath, saveFileName);

        public bool HasActiveRunSave()
        {
            string path = SavePath;
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public bool TrySave(ActiveRunSaveData saveData)
        {
            if (saveData == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[ActiveRunSave] Cannot save: save data is null.");

                return false;
            }

            string path = SavePath;
            if (string.IsNullOrEmpty(path))
            {
                if (verboseLogs)
                    Debug.LogWarning("[ActiveRunSave] Cannot save: save path is invalid.");

                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(path, json);

                if (verboseLogs)
                    Debug.Log($"[ActiveRunSave] Saved active run: {path}");

                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[ActiveRunSave] Save failed: {exception.Message}");
                return false;
            }
        }

        public bool TryLoad(out ActiveRunSaveData saveData)
        {
            saveData = null;
            string path = SavePath;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                if (verboseLogs)
                    Debug.Log("[ActiveRunSave] No active run save found.");

                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    if (verboseLogs)
                        Debug.LogWarning("[ActiveRunSave] Save file is empty.");

                    return false;
                }

                saveData = JsonUtility.FromJson<ActiveRunSaveData>(json);
                if (saveData == null || !saveData.IsValidForContinue())
                {
                    if (verboseLogs)
                        Debug.LogWarning("[ActiveRunSave] Loaded save data is invalid.");

                    saveData = null;
                    return false;
                }

                if (verboseLogs)
                    Debug.Log($"[ActiveRunSave] Loaded active run: {path}");

                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[ActiveRunSave] Load failed: {exception.Message}");
                saveData = null;
                return false;
            }
        }
        [ContextMenu("Debug Delete Active Run Save")]
        private void DebugDeleteActiveRunSave()
        {
            DeleteSave();
        }

        public bool DeleteSave()
        {
            string path = SavePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                if (verboseLogs)
                    Debug.Log("[ActiveRunSave] No active run save found.");

                return false;
            }

            try
            {
                File.Delete(path);

                if (verboseLogs)
                    Debug.Log("[ActiveRunSave] Deleted active run save.");

                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[ActiveRunSave] Delete failed: {exception.Message}");
                return false;
            }
        }
    }
}
