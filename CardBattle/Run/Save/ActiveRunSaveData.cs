using System;

namespace CardBattle.Core
{
    [Serializable]
    public class ActiveRunSaveData
    {
        public int schemaVersion = 1;
        public string savedAtUtc = string.Empty;
        public RunState runState;
        public RunMapState mapState;
        public string currentActId = string.Empty;
        public string lastSelectedNodeId = string.Empty;

        public static ActiveRunSaveData Create(
            RunState run,
            RunMapState map,
            string actId,
            string selectedNodeId)
        {
            return new ActiveRunSaveData
            {
                schemaVersion = 1,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                runState = run != null ? run.Clone() : null,
                mapState = map != null ? map.Clone() : null,
                currentActId = actId ?? string.Empty,
                lastSelectedNodeId = selectedNodeId ?? string.Empty
            };
        }

        public bool IsValidForContinue()
        {
            return runState != null && runState.isActive;
        }
    }
}
