using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "MapAct",
        menuName = "Card Battle/Map/Map Act Data",
        order = 40)]
    public class MapActData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string actId;
        [SerializeField] private string displayName;
        [SerializeField] private string startNodeId;

        [Header("Nodes")]
        [SerializeField] private List<MapNodeData> nodes = new List<MapNodeData>();

        public string ActId => actId;
        public string DisplayName => displayName;
        public string StartNodeId => startNodeId;
        public IReadOnlyList<MapNodeData> Nodes => nodes;

        public bool TryGetNode(string nodeId, out MapNodeData node)
        {
            node = null;

            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData candidate = nodes[i];
                if (candidate == null)
                    continue;

                if (string.Equals(candidate.NodeId, nodeId, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool ContainsNode(string nodeId)
        {
            return TryGetNode(nodeId, out _);
        }

        public bool IsRuntimeValid(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(actId))
            {
                error = "Act ID is blank.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(startNodeId))
            {
                error = "Start Node ID is blank.";
                return false;
            }

            if (nodes == null || nodes.Count == 0)
            {
                error = "Nodes list is null or empty.";
                return false;
            }

            var seenNodeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null)
                {
                    error = $"Node at index {i} is null.";
                    return false;
                }

                if (!node.HasValidNodeId)
                {
                    error = $"Node at index {i} has a blank nodeId.";
                    return false;
                }

                if (!seenNodeIds.Add(node.NodeId))
                {
                    error = $"Duplicate node ID '{node.NodeId}'.";
                    return false;
                }

                if (!node.IsRuntimeValid(out string nodeError))
                {
                    error = nodeError;
                    return false;
                }
            }

            if (!ContainsNode(startNodeId))
            {
                error = $"Start node ID '{startNodeId}' does not exist in nodes.";
                return false;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node?.ConnectedNodeIds == null)
                    continue;

                for (int j = 0; j < node.ConnectedNodeIds.Count; j++)
                {
                    string connectedId = node.ConnectedNodeIds[j];
                    if (string.IsNullOrWhiteSpace(connectedId))
                    {
                        error = $"Node '{node.NodeId}' has a blank connected node ID at index {j}.";
                        return false;
                    }

                    if (!ContainsNode(connectedId))
                    {
                        error =
                            $"Node '{node.NodeId}' references missing connected node ID '{connectedId}'.";
                        return false;
                    }
                }
            }

            return true;
        }

        public int CountValidNodes()
        {
            if (nodes == null)
                return 0;

            int count = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node != null && node.IsRuntimeValid(out _))
                    count++;
            }

            return count;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsRuntimeValid(out string error))
            {
                Debug.LogWarning(
                    $"[MapActData] '{name}' is invalid: {error}",
                    this);
            }

            if (nodes == null || nodes.Count == 0)
                return;

            var seenNodeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null)
                {
                    Debug.LogWarning(
                        $"[MapActData] Null node at index {i} in '{name}'.",
                        this);
                    continue;
                }

                if (!node.HasValidNodeId)
                {
                    Debug.LogWarning(
                        $"[MapActData] Node at index {i} has a blank nodeId in '{name}'.",
                        this);
                }
                else if (!seenNodeIds.Add(node.NodeId))
                {
                    Debug.LogWarning(
                        $"[MapActData] Duplicate node ID '{node.NodeId}' in '{name}'.",
                        this);
                }

                if (node.ConnectedNodeIds == null)
                    continue;

                for (int j = 0; j < node.ConnectedNodeIds.Count; j++)
                {
                    string connectedId = node.ConnectedNodeIds[j];
                    if (string.IsNullOrWhiteSpace(connectedId))
                        continue;

                    if (!ContainsNode(connectedId))
                    {
                        Debug.LogWarning(
                            $"[MapActData] Node '{node.NodeId}' references missing connected node ID " +
                            $"'{connectedId}' in '{name}'.",
                            this);
                    }
                }
            }
        }
#endif
    }
}
