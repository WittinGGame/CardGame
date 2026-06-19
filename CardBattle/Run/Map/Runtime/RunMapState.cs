using System;
using System.Collections.Generic;

namespace CardBattle.Core
{
    [Serializable]
    public class RunMapState
    {
        [UnityEngine.SerializeField] private string actId = string.Empty;
        [UnityEngine.SerializeField] private string currentNodeId = string.Empty;
        [UnityEngine.SerializeField] private string selectedNodeId = string.Empty;
        [UnityEngine.SerializeField] private List<RunMapNodeState> nodeStates = new List<RunMapNodeState>();

        public string ActId => actId;
        public string CurrentNodeId => currentNodeId;
        public string SelectedNodeId => selectedNodeId;
        public IReadOnlyList<RunMapNodeState> NodeStates => nodeStates;

        public void InitializeFromAct(MapActData actData)
        {
            if (actData == null)
                throw new ArgumentNullException(nameof(actData));

            actId = actData.ActId;
            currentNodeId = actData.StartNodeId;
            selectedNodeId = string.Empty;
            nodeStates = new List<RunMapNodeState>();

            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null || !node.HasValidNodeId)
                    continue;

                nodeStates.Add(new RunMapNodeState(node.NodeId, MapNodeState.Locked));
            }

            SetNodeState(actData.StartNodeId, MapNodeState.Completed);

            if (actData.TryGetNode(actData.StartNodeId, out MapNodeData startNode) &&
                startNode.ConnectedNodeIds != null)
            {
                for (int i = 0; i < startNode.ConnectedNodeIds.Count; i++)
                {
                    string connectedId = startNode.ConnectedNodeIds[i];
                    SetNodeState(connectedId, MapNodeState.Available);
                }
            }
        }

        public bool TryGetNodeState(string nodeId, out MapNodeState state)
        {
            state = MapNodeState.Locked;

            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            for (int i = 0; i < nodeStates.Count; i++)
            {
                RunMapNodeState entry = nodeStates[i];
                if (entry == null)
                    continue;

                if (string.Equals(entry.NodeId, nodeId, StringComparison.Ordinal))
                {
                    state = entry.State;
                    return true;
                }
            }

            return false;
        }

        public bool SetNodeState(string nodeId, MapNodeState state)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            for (int i = 0; i < nodeStates.Count; i++)
            {
                RunMapNodeState entry = nodeStates[i];
                if (entry == null)
                    continue;

                if (string.Equals(entry.NodeId, nodeId, StringComparison.Ordinal))
                {
                    entry.SetState(state);
                    return true;
                }
            }

            return false;
        }

        public bool IsNodeAvailable(string nodeId)
        {
            return TryGetNodeState(nodeId, out MapNodeState state) &&
                   state == MapNodeState.Available;
        }

        public bool IsNodeCompleted(string nodeId)
        {
            return TryGetNodeState(nodeId, out MapNodeState state) &&
                   state == MapNodeState.Completed;
        }

        public bool IsNodeCurrent(string nodeId)
        {
            return TryGetNodeState(nodeId, out MapNodeState state) &&
                   state == MapNodeState.Current;
        }

        public List<string> GetNodeIdsByState(MapNodeState state)
        {
            var result = new List<string>();

            for (int i = 0; i < nodeStates.Count; i++)
            {
                RunMapNodeState entry = nodeStates[i];
                if (entry == null)
                    continue;

                if (entry.State == state)
                    result.Add(entry.NodeId);
            }

            return result;
        }

        public void ClearSelection()
        {
            selectedNodeId = string.Empty;
        }

        public void SetCurrentNodeId(string nodeId)
        {
            currentNodeId = nodeId ?? string.Empty;
        }

        public void SetSelectedNodeId(string nodeId)
        {
            selectedNodeId = nodeId ?? string.Empty;
        }

        public RunMapState Clone()
        {
            var clone = new RunMapState
            {
                actId = actId,
                currentNodeId = currentNodeId,
                selectedNodeId = selectedNodeId,
                nodeStates = new List<RunMapNodeState>(nodeStates.Count)
            };

            for (int i = 0; i < nodeStates.Count; i++)
            {
                RunMapNodeState entry = nodeStates[i];
                clone.nodeStates.Add(entry != null ? entry.Clone() : null);
            }

            return clone;
        }
    }
}
