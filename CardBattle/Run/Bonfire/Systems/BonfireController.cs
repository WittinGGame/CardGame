using System;
using UnityEngine;

namespace CardBattle.Core
{
    public class BonfireController : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private MapRuntimeController mapRuntimeController;
        [SerializeField] private TreeMapBattleFlowController treeMapBattleFlowController;
        [SerializeField] private ActiveRunAutoSaveController activeRunAutoSaveController;
        [SerializeField] private TreeMapUIController treeMapUIController;

        private RunManager subscribedRunManager;
        private MapRuntimeController subscribedMap;
        private TreeMapBattleFlowController subscribedFlow;
        private RunState sessionRun;
        private RunMapState sessionMap;
        private string sessionRunId;
        private string sessionNodeId;
        private bool choiceCommitted;
        private bool isApplyingChoice;
        private bool checkpointSaved;
        private bool nodeCompleted;
        private string lastError = string.Empty;

        public event Action OnStateChanged;

        public bool IsActive => IsSessionValid();
        public string SelectedNodeId => IsActive ? sessionNodeId : string.Empty;
        public bool HasCommittedChoice => IsActive && choiceCommitted;
        public int CurrentHp => IsActive ? sessionRun.currentHp : 0;
        public int MaxHp => IsActive ? sessionRun.maxHp : 0;
        public int RestHealAmount => IsActive ? (int)((MaxHp * 30L + 99L) / 100L) : 0;
        public int PreviewResultingHp => IsActive
            ? CurrentHp + Mathf.Min(RestHealAmount, MaxHp - CurrentHp)
            : 0;
        public bool IsBusy => isApplyingChoice;
        public bool IsCompletedPendingSave => IsActive && nodeCompleted;
        public string LastError => lastError;
        public bool CanRetrySave => IsActive && !isApplyingChoice &&
                                    (!checkpointSaved || nodeCompleted);
        public bool CanLeave => IsActive && checkpointSaved && !choiceCommitted &&
                                !isApplyingChoice && CurrentHp == MaxHp;
        public bool CanRest => IsActive && checkpointSaved && !choiceCommitted && !isApplyingChoice &&
                               CurrentHp < MaxHp;

        private RunManager ResolveRunManager() => runManager != null ? runManager : RunManager.Instance;

        private void OnEnable()
        {
            Subscribe();
            RefreshState();
        }

        private void OnDisable()
        {
            Unsubscribe();
            // Disabling presentation/its parent must not replenish a consumed pending choice.
        }

        public bool TryOpenSession(MapNodeData node)
        {
            if (!isActiveAndEnabled || isApplyingChoice)
                return false;

            Subscribe();
            if (!IsSessionValid())
                ClearSession();

            RunManager manager = ResolveRunManager();
            if (manager == null || !manager.HasActiveRun ||
                string.IsNullOrWhiteSpace(manager.CurrentRun.runId) ||
                manager.CurrentRun.maxHp <= 0 || manager.CurrentRun.currentHp <= 0 ||
                manager.CurrentRun.currentHp > manager.CurrentRun.maxHp ||
                !IsSelectedRest(node))
                return false;

            if (sessionRun == manager.CurrentRun && sessionMap == mapRuntimeController.CurrentMapState &&
                string.Equals(sessionNodeId, node.NodeId, StringComparison.Ordinal))
            {
                treeMapUIController?.Hide();
                OnStateChanged?.Invoke();
                return true;
            }

            sessionRun = manager.CurrentRun;
            sessionMap = mapRuntimeController.CurrentMapState;
            sessionRunId = sessionRun.runId;
            sessionNodeId = node.NodeId;
            choiceCommitted = false;
            checkpointSaved = false;
            nodeCompleted = false;
            lastError = string.Empty;
            treeMapUIController?.Hide();
            TryRetrySave();
            return true;
        }

        public bool TryRest() => TryResolveChoice(true);
        public bool TryLeave() => TryResolveChoice(false);

        private bool TryResolveChoice(bool heal)
        {
            if (!isActiveAndEnabled || (heal ? !CanRest : !CanLeave))
                return false;

            int resultingHp = PreviewResultingHp;
            choiceCommitted = true;
            isApplyingChoice = true;
            OnStateChanged?.Invoke();
            try
            {
                if (!IsPendingSessionValid())
                    return false;
                if (heal && !ResolveRunManager().SetCurrentHp(resultingHp))
                {
                    choiceCommitted = false;
                    return false;
                }

                // HP events may have replaced the run/map. Never complete a different node.
                if (!IsPendingSessionValid() || !mapRuntimeController.TryCompleteSelectedNode())
                {
                    lastError = "Unable to complete this Rest node.";
                    return false;
                }
                nodeCompleted = true;
                SaveCurrentStage();
                return true; // Choice succeeded even when its final save needs retry.
            }
            finally
            {
                isApplyingChoice = false;
                RefreshState();
            }
        }

        public bool TryRetrySave()
        {
            if (!isActiveAndEnabled || !CanRetrySave)
                return false;
            isApplyingChoice = true;
            OnStateChanged?.Invoke();
            try { return SaveCurrentStage(); }
            finally
            {
                isApplyingChoice = false;
                RefreshState();
            }
        }

        private bool SaveCurrentStage()
        {
            if (!IsSessionValid())
                return false;
            bool saved = activeRunAutoSaveController != null &&
                activeRunAutoSaveController.SaveNow(nodeCompleted ? "BonfireCompleted" : "BonfirePending");
            if (!saved)
            {
                lastError = nodeCompleted
                    ? "Progress could not be saved. Retry Save; your choice will not be applied again."
                    : "Unable to save this Rest stop. Retry Save before choosing an action.";
                return false;
            }
            lastError = string.Empty;
            if (!nodeCompleted)
                checkpointSaved = true;
            else
            {
                ClearSession();
                OnStateChanged?.Invoke();
                treeMapUIController?.EnsureMapVisibleAndInteractive();
            }
            return true;
        }

        // Explicit lifecycle teardown only; do not use this when merely hiding/reopening UI.
        public void ResetSession()
        {
            ClearSession();
            OnStateChanged?.Invoke();
        }

        private void ClearSession()
        {
            sessionRun = null;
            sessionMap = null;
            sessionRunId = string.Empty;
            sessionNodeId = string.Empty;
            choiceCommitted = false;
            checkpointSaved = false;
            nodeCompleted = false;
            lastError = string.Empty;
        }

        private bool IsPendingSessionValid() => IsRunAndMapValid() &&
            mapRuntimeController.TryGetSelectedNode(out MapNodeData node) &&
            node.NodeId == sessionNodeId && IsSelectedRest(node);

        private bool IsSessionValid()
        {
            if (!IsRunAndMapValid())
                return false;
            return nodeCompleted
                ? !mapRuntimeController.HasSelectedNode &&
                  mapRuntimeController.CurrentNodeId == sessionNodeId &&
                  mapRuntimeController.GetNodeState(sessionNodeId) == MapNodeState.Completed
                : IsPendingSessionValid();
        }

        private bool IsRunAndMapValid()
        {
            RunManager manager = ResolveRunManager();
            return sessionRun != null && manager != null && manager.HasActiveRun &&
                   manager.CurrentRun == sessionRun && sessionRun.runId == sessionRunId &&
                   sessionRun.maxHp > 0 && sessionRun.currentHp > 0 &&
                   sessionRun.currentHp <= sessionRun.maxHp && mapRuntimeController != null &&
                   mapRuntimeController.HasInitialized &&
                   mapRuntimeController.CurrentMapState == sessionMap;
        }

        private bool IsSelectedRest(MapNodeData node)
        {
            return node != null && node.NodeType == MapNodeType.Rest && !node.HasEncounter &&
                   mapRuntimeController != null && mapRuntimeController.HasInitialized &&
                   mapRuntimeController.TryGetSelectedNode(out MapNodeData selected) &&
                   ReferenceEquals(selected, node) && mapRuntimeController.CurrentNodeId == node.NodeId &&
                   mapRuntimeController.GetNodeState(node.NodeId) == MapNodeState.Current;
        }

        private void HandleRestNodeSelected(MapNodeData node) => TryOpenSession(node);
        private void HandleRunChanged(RunState _) => RefreshState();
        private void HandleMapInitialized(RunMapState _) => RefreshState();
        private void RefreshState()
        {
            // Completion clears map selection synchronously before nodeCompleted is assigned.
            if (isApplyingChoice)
                return;
            if (!IsSessionValid())
                ClearSession();
            OnStateChanged?.Invoke();
        }

        private void Subscribe()
        {
            Unsubscribe();
            subscribedRunManager = ResolveRunManager();
            if (subscribedRunManager != null)
            {
                subscribedRunManager.OnRunStarted += HandleRunChanged;
                subscribedRunManager.OnRunChanged += HandleRunChanged;
                subscribedRunManager.OnRunCleared += ResetSession;
            }
            subscribedMap = mapRuntimeController;
            if (subscribedMap != null)
            {
                subscribedMap.OnMapInitialized += HandleMapInitialized;
                subscribedMap.OnMapStateChanged += RefreshState;
            }
            subscribedFlow = treeMapBattleFlowController;
            if (subscribedFlow != null)
                subscribedFlow.OnRestNodeSelected += HandleRestNodeSelected;
        }

        private void Unsubscribe()
        {
            if (subscribedRunManager != null)
            {
                subscribedRunManager.OnRunStarted -= HandleRunChanged;
                subscribedRunManager.OnRunChanged -= HandleRunChanged;
                subscribedRunManager.OnRunCleared -= ResetSession;
            }
            if (subscribedMap != null)
            {
                subscribedMap.OnMapInitialized -= HandleMapInitialized;
                subscribedMap.OnMapStateChanged -= RefreshState;
            }
            if (subscribedFlow != null)
                subscribedFlow.OnRestNodeSelected -= HandleRestNodeSelected;
            subscribedRunManager = null;
            subscribedMap = null;
            subscribedFlow = null;
        }
    }
}
