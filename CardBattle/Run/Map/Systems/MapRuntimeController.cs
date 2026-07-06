using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    public class MapRuntimeController : MonoBehaviour
    {
        [Header("Map Source")]
        [SerializeField] private MapActData actData;

        [Header("Encounter")]
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;

        [Header("Options")]
        [SerializeField] private bool initializeOnStart;
        [SerializeField] private bool lockUnchosenBranchesOnComplete = true;
        [SerializeField] private bool verboseLogs = true;

        public MapActData ActData => actData;
        public RunMapState CurrentMapState { get; private set; }
        public bool HasInitialized { get; private set; }

        public string CurrentNodeId =>
            CurrentMapState != null ? CurrentMapState.CurrentNodeId : string.Empty;

        public string SelectedNodeId =>
            CurrentMapState != null ? CurrentMapState.SelectedNodeId : string.Empty;

        public string CurrentActId
        {
            get
            {
                if (CurrentMapState != null && !string.IsNullOrWhiteSpace(CurrentMapState.ActId))
                    return CurrentMapState.ActId;

                return actData != null ? actData.ActId : string.Empty;
            }
        }

        public bool HasSelectedNode =>
            CurrentMapState != null &&
            !string.IsNullOrWhiteSpace(CurrentMapState.SelectedNodeId);

        public bool HasPendingEncounterNode =>
            HasSelectedNode &&
            CurrentMapState.IsNodeCurrent(CurrentMapState.SelectedNodeId) &&
            !CurrentMapState.IsNodeCompleted(CurrentMapState.SelectedNodeId);

        public event Action<RunMapState> OnMapInitialized;
        public event Action<string, MapNodeState> OnNodeStateChanged;
        public event Action<MapNodeData> OnNodeSelected;
        public event Action<MapNodeData> OnNodeCompleted;
        public event Action OnMapStateChanged;

        private void Start()
        {
            if (initializeOnStart)
                InitializeMap();
        }

        public bool InitializeMap()
        {
            if (actData == null)
            {
                LogError("Cannot initialize map: Act Data is missing.");
                return false;
            }

            if (!actData.IsRuntimeValid(out string error))
            {
                LogError($"Cannot initialize map: {error}");
                return false;
            }

            CurrentMapState = new RunMapState();
            CurrentMapState.InitializeFromAct(actData);
            HasInitialized = true;

            OnMapInitialized?.Invoke(CurrentMapState);
            OnMapStateChanged?.Invoke();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[MapRuntimeController] Map initialized. Act={actData.ActId} | Start={actData.StartNodeId}");

                List<string> available = CurrentMapState.GetNodeIdsByState(MapNodeState.Available);
                Debug.Log(
                    $"[MapRuntimeController] Available Nodes: {FormatNodeIdList(available)}");
            }

            return true;
        }

        public bool CanStartBattleFromNode(string nodeId)
        {
            if (!HasInitialized || CurrentMapState == null ||
                string.IsNullOrWhiteSpace(nodeId))
            {
                return false;
            }

            if (actData == null || !actData.TryGetNode(nodeId, out MapNodeData node))
                return false;

            if (!node.HasEncounter)
                return false;

            if (HasPendingEncounterNode)
            {
                return string.Equals(
                           CurrentMapState.SelectedNodeId,
                           nodeId,
                           StringComparison.Ordinal) &&
                       CurrentMapState.IsNodeCurrent(nodeId);
            }

            return CurrentMapState.IsNodeAvailable(nodeId);
        }

        public bool TryRestorePendingEncounterSelection()
        {
            if (!HasPendingEncounterNode)
                return true;

            if (!TryGetSelectedNode(out MapNodeData node))
            {
                LogError("Cannot restore pending encounter: selected node data is missing.");
                return false;
            }

            bool restored = TryEnsureEncounterSelectedForNode(node);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[MapRuntimeController] Restore pending encounter for node={node.NodeId} => {restored}");
            }

            return restored;
        }

        public bool TrySelectNode(string nodeId)
        {
            if (!HasInitialized || CurrentMapState == null)
            {
                LogError("Cannot select node: Map is not initialized.");
                return false;
            }

            if (HasPendingEncounterNode)
            {
                if (!string.Equals(CurrentMapState.SelectedNodeId, nodeId, StringComparison.Ordinal))
                {
                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            $"[MapRuntimeController] Cannot select node '{nodeId}': " +
                            $"pending encounter is locked to '{CurrentMapState.SelectedNodeId}'.");
                    }

                    return false;
                }

                if (!CurrentMapState.IsNodeCurrent(nodeId))
                {
                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            $"[MapRuntimeController] Cannot re-enter node '{nodeId}': " +
                            "node is not the pending current node.");
                    }

                    return false;
                }

                if (!TryGetSelectedNode(out MapNodeData pendingNode))
                    return false;

                bool encounterReady = TryEnsureEncounterSelectedForNode(pendingNode);

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[MapRuntimeController] Re-enter pending node: {nodeId} | " +
                        $"EncounterReady={encounterReady}");
                }

                return encounterReady;
            }

            if (actData == null || !actData.TryGetNode(nodeId, out MapNodeData node))
            {
                LogError($"Cannot select node: Node ID '{nodeId}' was not found.");
                return false;
            }

            if (!CanSelectNode(nodeId))
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[MapRuntimeController] Cannot select node '{nodeId}': " +
                        $"state is {GetNodeState(nodeId)}, expected Available.");
                }

                return false;
            }

            return TrySelectNodeInternal(node);
        }

        private bool TrySelectNodeInternal(MapNodeData node)
        {
            if (node == null || !node.HasValidNodeId)
                return false;

            string nodeId = node.NodeId;
            bool runtimeEncounterSelected = TryEnsureEncounterSelectedForNode(node);
            if (node.HasEncounter && !runtimeEncounterSelected)
                return false;

            ClearCurrentNodesExcept(nodeId);

            CurrentMapState.SetSelectedNodeId(nodeId);
            CurrentMapState.SetCurrentNodeId(nodeId);
            SetNodeStateInternal(nodeId, MapNodeState.Current);

            OnNodeSelected?.Invoke(node);
            OnMapStateChanged?.Invoke();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[MapRuntimeController] Selected node: {nodeId} | " +
                    $"Encounter={(node.HasEncounter ? node.EncounterId : "none")} | " +
                    $"RuntimeEncounterSelected={runtimeEncounterSelected}");
            }

            return true;
        }

        private bool TryEnsureEncounterSelectedForNode(MapNodeData node)
        {
            if (node == null || !node.HasEncounter)
                return true;

            if (runtimeEncounterContext == null)
            {
                LogError(
                    $"Cannot select node '{node.NodeId}': RuntimeEncounterContext is missing " +
                    $"but node requires encounter '{node.EncounterId}'.");
                return false;
            }

            return runtimeEncounterContext.TrySelectEncounterById(node.EncounterId);
        }

        public bool TryCompleteSelectedNode()
        {
            if (CurrentMapState == null)
            {
                LogError("Cannot complete selected node: Map state is missing.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CurrentMapState.SelectedNodeId))
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[MapRuntimeController] Cannot complete selected node: No selected node.");
                }

                return false;
            }

            return TryCompleteNode(CurrentMapState.SelectedNodeId);
        }

        public bool TryGetSelectedNode(out MapNodeData node)
        {
            node = null;

            if (CurrentMapState == null || actData == null)
                return false;

            if (string.IsNullOrWhiteSpace(CurrentMapState.SelectedNodeId))
                return false;

            return actData.TryGetNode(CurrentMapState.SelectedNodeId, out node);
        }

        public bool TryCompleteNode(string nodeId)
        {
            if (!HasInitialized || CurrentMapState == null)
            {
                LogError("Cannot complete node: Map is not initialized.");
                return false;
            }

            if (actData == null || !actData.TryGetNode(nodeId, out MapNodeData node))
            {
                LogError($"Cannot complete node: Node ID '{nodeId}' was not found.");
                return false;
            }

            MapNodeState state = GetNodeState(nodeId);
            if (state != MapNodeState.Current && state != MapNodeState.Available)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[MapRuntimeController] Cannot complete node '{nodeId}': " +
                        $"state is {state}, expected Current or Available.");
                }

                return false;
            }

            SetNodeStateInternal(nodeId, MapNodeState.Completed);
            CurrentMapState.SetCurrentNodeId(nodeId);

            if (string.Equals(CurrentMapState.SelectedNodeId, nodeId, StringComparison.Ordinal))
                CurrentMapState.ClearSelection();

            var unlockedNodes = new List<string>();
            var lockedUnchosenBranches = new List<string>();

            HashSet<string> nextAvailableIds = BuildConnectedNodeIdSet(node);

            if (lockUnchosenBranchesOnComplete)
                LockUnchosenBranches(nodeId, nextAvailableIds, lockedUnchosenBranches);

            UnlockConnectedNodes(node, unlockedNodes);

            OnNodeCompleted?.Invoke(node);
            OnMapStateChanged?.Invoke();

            if (verboseLogs)
            {
                Debug.Log($"[MapRuntimeController] Completed node: {nodeId}");

                if (lockUnchosenBranchesOnComplete && lockedUnchosenBranches.Count > 0)
                {
                    Debug.Log(
                        $"[MapRuntimeController] Locked unchosen branches: " +
                        $"{FormatNodeIdList(lockedUnchosenBranches)}");
                }

                Debug.Log(
                    $"[MapRuntimeController] Unlocked nodes: {FormatNodeIdList(unlockedNodes)}");
            }

            return true;
        }

        private static HashSet<string> BuildConnectedNodeIdSet(MapNodeData node)
        {
            var connectedIds = new HashSet<string>(StringComparer.Ordinal);

            if (node?.ConnectedNodeIds == null)
                return connectedIds;

            for (int i = 0; i < node.ConnectedNodeIds.Count; i++)
            {
                string connectedId = node.ConnectedNodeIds[i];
                if (!string.IsNullOrWhiteSpace(connectedId))
                    connectedIds.Add(connectedId);
            }

            return connectedIds;
        }

        private void LockUnchosenBranches(
            string completedNodeId,
            HashSet<string> nextAvailableIds,
            List<string> lockedUnchosenBranches)
        {
            IReadOnlyList<RunMapNodeState> allStates = CurrentMapState.NodeStates;
            for (int i = 0; i < allStates.Count; i++)
            {
                RunMapNodeState entry = allStates[i];
                if (entry == null)
                    continue;

                string id = entry.NodeId;
                MapNodeState entryState = entry.State;

                if (entryState == MapNodeState.Completed)
                    continue;

                if (string.Equals(id, completedNodeId, StringComparison.Ordinal))
                    continue;

                if (nextAvailableIds.Contains(id))
                    continue;

                if (entryState != MapNodeState.Available && entryState != MapNodeState.Current)
                    continue;

                SetNodeStateInternal(id, MapNodeState.Locked);
                lockedUnchosenBranches.Add(id);
            }
        }

        private void UnlockConnectedNodes(MapNodeData node, List<string> unlockedNodes)
        {
            if (node.ConnectedNodeIds == null)
                return;

            for (int i = 0; i < node.ConnectedNodeIds.Count; i++)
            {
                string connectedId = node.ConnectedNodeIds[i];
                if (!CurrentMapState.TryGetNodeState(connectedId, out MapNodeState connectedState))
                    continue;

                if (connectedState != MapNodeState.Locked)
                    continue;

                SetNodeStateInternal(connectedId, MapNodeState.Available);
                unlockedNodes.Add(connectedId);
            }
        }

        public bool CanSelectNode(string nodeId)
        {
            if (!HasInitialized || CurrentMapState == null)
                return false;

            return CurrentMapState.IsNodeAvailable(nodeId);
        }

        public MapNodeState GetNodeState(string nodeId)
        {
            if (CurrentMapState == null ||
                !CurrentMapState.TryGetNodeState(nodeId, out MapNodeState state))
            {
                return MapNodeState.Locked;
            }

            return state;
        }

        public RunMapState GetMapSnapshot()
        {
            if (CurrentMapState == null)
                return null;

            RunMapState snapshot = CurrentMapState.Clone();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[MapRuntimeController] Map state snapshot created. Act={snapshot.ActId}");
            }

            return snapshot;
        }

        public bool RestoreMapState(RunMapState restoredState)
        {
            if (restoredState == null)
            {
                LogError("Cannot restore map: restored state is null.");
                return false;
            }

            if (actData == null)
            {
                LogError("Cannot restore map: Act Data is missing.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(restoredState.ActId) &&
                !string.Equals(restoredState.ActId, actData.ActId, StringComparison.Ordinal))
            {
                LogError(
                    $"Cannot restore map: act mismatch saved={restoredState.ActId} " +
                    $"current={actData.ActId}");
                return false;
            }

            CurrentMapState = restoredState.Clone();
            HasInitialized = true;

            OnMapInitialized?.Invoke(CurrentMapState);
            OnMapStateChanged?.Invoke();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[MapRuntimeController] Map state restored. Act={CurrentMapState.ActId}");
            }

            return true;
        }

        public void DebugPrintMapState()
        {
            if (!HasInitialized || CurrentMapState == null)
            {
                Debug.Log("[MapRuntimeController] Map is not initialized.");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("[MapRuntimeController] --- Map State ---");
            builder.AppendLine($"CurrentNodeId={CurrentMapState.CurrentNodeId}");
            builder.AppendLine($"SelectedNodeId={CurrentMapState.SelectedNodeId}");
            builder.AppendLine(
                $"Completed={FormatNodeIdList(CurrentMapState.GetNodeIdsByState(MapNodeState.Completed))}");
            builder.AppendLine(
                $"Available={FormatNodeIdList(CurrentMapState.GetNodeIdsByState(MapNodeState.Available))}");

            List<string> currentNodes = CurrentMapState.GetNodeIdsByState(MapNodeState.Current);
            if (currentNodes.Count > 0)
            {
                builder.AppendLine($"Current={FormatNodeIdList(currentNodes)}");
            }

            builder.AppendLine(
                $"Locked={FormatNodeIdList(CurrentMapState.GetNodeIdsByState(MapNodeState.Locked))}");

            Debug.Log(builder.ToString().TrimEnd());
        }

        private void ClearCurrentNodesExcept(string selectedNodeId)
        {
            List<string> currentNodes = CurrentMapState.GetNodeIdsByState(MapNodeState.Current);
            for (int i = 0; i < currentNodes.Count; i++)
            {
                string currentId = currentNodes[i];
                if (string.Equals(currentId, selectedNodeId, StringComparison.Ordinal))
                    continue;

                SetNodeStateInternal(currentId, MapNodeState.Available);
            }
        }

        private void SetNodeStateInternal(string nodeId, MapNodeState state)
        {
            if (!CurrentMapState.SetNodeState(nodeId, state))
                return;

            OnNodeStateChanged?.Invoke(nodeId, state);
        }

        private static string FormatNodeIdList(List<string> nodeIds)
        {
            if (nodeIds == null || nodeIds.Count == 0)
                return string.Empty;

            return string.Join(", ", nodeIds);
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MapRuntimeController] {message}");
        }
    }
}
