using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Editor/runtime-safe debug helper for hand-authored MapActData.
    /// It does not mutate map data. It only prints readable authoring diagnostics.
    /// </summary>
    public class MapActAuthoringDebugReporter : MonoBehaviour
    {
        [Header("Map Data")]
        [SerializeField] private MapActData actData;

        [Header("Optional")]
        [SerializeField] private EncounterCatalog encounterCatalog;

        [Header("Options")]
        [SerializeField] private bool checkEncounterCatalog = true;
        [SerializeField] private bool warnBattleNodeWithoutEncounter = true;
        [SerializeField] private bool warnDuplicatePositions = true;
        [SerializeField] private float duplicatePositionTolerance = 0.01f;

        [ContextMenu("Map Debug/Print Summary")]
        public void PrintSummary()
        {
            if (!ValidateActDataReference())
                return;

            var sb = new StringBuilder();

            sb.AppendLine("========== Map Authoring Summary ==========");
            sb.AppendLine($"Asset: {actData.name}");
            sb.AppendLine($"ActId: {actData.ActId}");
            sb.AppendLine($"DisplayName: {actData.DisplayName}");
            sb.AppendLine($"StartNodeId: {actData.StartNodeId}");
            sb.AppendLine($"NodeCount: {(actData.Nodes != null ? actData.Nodes.Count : 0)}");
            sb.AppendLine();

            bool valid = actData.IsRuntimeValid(out string error);
            sb.AppendLine($"RuntimeValid: {valid}");
            if (!valid)
                sb.AppendLine($"ValidationError: {error}");

            sb.AppendLine();
            sb.AppendLine("Nodes:");

            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];

                if (node == null)
                {
                    sb.AppendLine($"[{i}] <NULL NODE>");
                    continue;
                }

                string encounterText = node.HasEncounter
                    ? node.EncounterId
                    : "none";

                string nextText = BuildConnectedText(node);

                sb.AppendLine(
                    $"[{i}] {node.NodeId} | " +
                    $"Name='{node.DisplayName}' | " +
                    $"Type={node.NodeType} | " +
                    $"Encounter={encounterText} | " +
                    $"Pos={FormatVector2(node.UiPosition)} | " +
                    $"Next={nextText}");
            }

            Debug.Log(sb.ToString(), this);
        }

        [ContextMenu("Map Debug/Print Connections Only")]
        public void PrintConnectionsOnly()
        {
            if (!ValidateActDataReference())
                return;

            var sb = new StringBuilder();

            sb.AppendLine("========== Map Connections ==========");
            sb.AppendLine($"Act: {actData.ActId}");
            sb.AppendLine($"Start: {actData.StartNodeId}");
            sb.AppendLine();

            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null)
                    continue;

                sb.AppendLine($"{node.NodeId} -> {BuildConnectedText(node)}");
            }

            Debug.Log(sb.ToString(), this);
        }

        [ContextMenu("Map Debug/Print Positions Only")]
        public void PrintPositionsOnly()
        {
            if (!ValidateActDataReference())
                return;

            var sb = new StringBuilder();

            sb.AppendLine("========== Map Node Positions ==========");
            sb.AppendLine($"Act: {actData.ActId}");
            sb.AppendLine();

            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null)
                    continue;

                sb.AppendLine($"{node.NodeId} = {FormatVector2(node.UiPosition)}");
            }

            Debug.Log(sb.ToString(), this);
        }

        [ContextMenu("Map Debug/Validate Authoring")]
        public void ValidateAuthoring()
        {
            if (!ValidateActDataReference())
                return;

            var problems = new List<string>();

            ValidateRuntimeRules(problems);
            ValidateNodeAuthoringRules(problems);
            ValidateConnections(problems);
            ValidateEncounterIds(problems);
            ValidateDuplicatePositions(problems);

            if (problems.Count == 0)
            {
                Debug.Log(
                    $"[MapActAuthoringDebugReporter] OK: '{actData.name}' has no authoring warnings.",
                    this);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[MapActAuthoringDebugReporter] Found {problems.Count} issue(s) in '{actData.name}':");

            for (int i = 0; i < problems.Count; i++)
                sb.AppendLine($"- {problems[i]}");

            Debug.LogWarning(sb.ToString(), this);
        }

        private void ValidateRuntimeRules(List<string> problems)
        {
            if (!actData.IsRuntimeValid(out string error))
                problems.Add($"Runtime validation failed: {error}");
        }

        private void ValidateNodeAuthoringRules(List<string> problems)
        {
            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            if (nodes == null)
            {
                problems.Add("Nodes list is null.");
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];

                if (node == null)
                {
                    problems.Add($"Node at index {i} is null.");
                    continue;
                }

                if (!node.HasValidNodeId)
                    problems.Add($"Node at index {i} has blank NodeId.");

                if (string.IsNullOrWhiteSpace(node.DisplayName))
                    problems.Add($"Node '{node.NodeId}' has blank DisplayName.");

                if (warnBattleNodeWithoutEncounter &&
                    IsBattleNode(node) &&
                    !node.HasEncounter)
                {
                    problems.Add($"Battle node '{node.NodeId}' has no EncounterId.");
                }
            }
        }

        private void ValidateConnections(List<string> problems)
        {
            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            if (nodes == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null || node.ConnectedNodeIds == null)
                    continue;

                for (int j = 0; j < node.ConnectedNodeIds.Count; j++)
                {
                    string connectedId = node.ConnectedNodeIds[j];

                    if (string.IsNullOrWhiteSpace(connectedId))
                    {
                        problems.Add($"Node '{node.NodeId}' has blank connected node at index {j}.");
                        continue;
                    }

                    if (!actData.ContainsNode(connectedId))
                    {
                        problems.Add($"Node '{node.NodeId}' connects to missing node '{connectedId}'.");
                    }

                    if (string.Equals(node.NodeId, connectedId, StringComparison.Ordinal))
                    {
                        problems.Add($"Node '{node.NodeId}' connects to itself.");
                    }
                }
            }
        }

        private void ValidateEncounterIds(List<string> problems)
        {
            if (!checkEncounterCatalog)
                return;

            if (encounterCatalog == null)
            {
                problems.Add("EncounterCatalog is missing, so encounterId lookup cannot be checked.");
                return;
            }

            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            if (nodes == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null || !node.HasEncounter)
                    continue;

                if (!encounterCatalog.TryGetEncounter(node.EncounterId, out EncounterData encounter) ||
                    encounter == null)
                {
                    problems.Add(
                        $"Node '{node.NodeId}' references missing EncounterId '{node.EncounterId}'.");
                }
            }
        }

        private void ValidateDuplicatePositions(List<string> problems)
        {
            if (!warnDuplicatePositions)
                return;

            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            if (nodes == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData a = nodes[i];
                if (a == null)
                    continue;

                for (int j = i + 1; j < nodes.Count; j++)
                {
                    MapNodeData b = nodes[j];
                    if (b == null)
                        continue;

                    float distance = Vector2.Distance(a.UiPosition, b.UiPosition);
                    if (distance <= duplicatePositionTolerance)
                    {
                        problems.Add(
                            $"Nodes '{a.NodeId}' and '{b.NodeId}' have almost the same UiPosition " +
                            $"{FormatVector2(a.UiPosition)}.");
                    }
                }
            }
        }

        private bool ValidateActDataReference()
        {
            if (actData != null)
                return true;

            Debug.LogError("[MapActAuthoringDebugReporter] ActData reference is missing.", this);
            return false;
        }

        private static bool IsBattleNode(MapNodeData node)
        {
            if (node == null)
                return false;

            return node.NodeType == MapNodeType.NormalBattle ||
                   node.NodeType == MapNodeType.EliteBattle ||
                   node.NodeType == MapNodeType.Boss;
        }

        private static string BuildConnectedText(MapNodeData node)
        {
            if (node == null || node.ConnectedNodeIds == null || node.ConnectedNodeIds.Count == 0)
                return "none";

            return string.Join(", ", node.ConnectedNodeIds);
        }

        private static string FormatVector2(Vector2 value)
        {
            return $"({value.x:0.##}, {value.y:0.##})";
        }
    }
}