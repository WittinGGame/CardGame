using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [Serializable]
    public class MapNodeData
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string displayName;
        [SerializeField] private MapNodeType nodeType;
        [SerializeField] private string encounterId;
        [SerializeField] private List<string> connectedNodeIds = new List<string>();
        [SerializeField] private Vector2 uiPosition;

        public string NodeId => nodeId;
        public string DisplayName => displayName;
        public MapNodeType NodeType => nodeType;
        public string EncounterId => encounterId;
        public IReadOnlyList<string> ConnectedNodeIds => connectedNodeIds;
        public Vector2 UiPosition => uiPosition;

        public bool HasEncounter => !string.IsNullOrWhiteSpace(encounterId);

        public bool IsBattleNode =>
            nodeType == MapNodeType.NormalBattle ||
            nodeType == MapNodeType.EliteBattle ||
            nodeType == MapNodeType.Boss;

        public bool HasValidNodeId => !string.IsNullOrWhiteSpace(nodeId);

        public bool IsRuntimeValid(out string error)
        {
            error = string.Empty;

            if (!HasValidNodeId)
            {
                error = "Node ID is blank.";
                return false;
            }

            if (connectedNodeIds == null)
            {
                error = $"Node '{nodeId}' has null connectedNodeIds.";
                return false;
            }

            if (nodeType != MapNodeType.Start && IsBattleNode && !HasEncounter)
            {
                error = $"Battle node '{nodeId}' is missing encounterId.";
                return false;
            }

            return true;
        }
    }
}
