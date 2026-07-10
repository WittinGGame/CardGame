using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class TreeMapUIController : MonoBehaviour
    {
        [Header("Map Source")]
        [SerializeField] private MapRuntimeController mapRuntimeController;

        [Header("Containers")]
        [SerializeField] private RectTransform nodeContainer;
        [SerializeField] private RectTransform lineContainer;

        [Header("Prefabs")]
        [SerializeField] private TreeMapNodeButtonUI nodeButtonPrefab;
        [SerializeField] private TreeMapLineUI linePrefab;

        [Header("Node Icons")]
        [SerializeField] private List<TreeMapNodeTypeIconEntry> nodeTypeIcons =
            new List<TreeMapNodeTypeIconEntry>();

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button startBattleButton;
        [SerializeField] private TextMeshProUGUI selectedNodeText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Layout")]
        [SerializeField] private float nodePositionScale = 1f;

        [Header("Line Style")]
        [SerializeField] private Color lineColor = new Color(0.55f, 0.55f, 0.6f, 0.85f);
        [SerializeField] private float lineThickness = 4f;

        [Header("Options")]
        [SerializeField] private bool initializeMapOnStart = true;
        [SerializeField] private bool rebuildOnMapStateChanged = true;
        [SerializeField] private bool hideStartBattleButtonInGameplay = true;
        [SerializeField] private bool verboseLogs = true;

        private readonly Dictionary<string, TreeMapNodeButtonUI> nodeViews =
            new Dictionary<string, TreeMapNodeButtonUI>(StringComparer.Ordinal);

        private readonly List<TreeMapLineUI> lineViews = new List<TreeMapLineUI>();

        public bool IsVisible { get; private set; }

        public event Action<string> OnNodeStartRequested;

        private MapRuntimeController subscribedMapController;
        private bool isBattleStartInProgress;

        private void Awake()
        {
            if (startBattleButton != null)
            {
                startBattleButton.onClick.RemoveListener(HandleStartBattleClicked);
                startBattleButton.onClick.AddListener(HandleStartBattleClicked);

                if (hideStartBattleButtonInGameplay)
                    startBattleButton.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (initializeMapOnStart && mapRuntimeController != null && !mapRuntimeController.HasInitialized)
                mapRuntimeController.InitializeMap();

            Rebuild();
        }

        private void OnEnable()
        {
            SubscribeMapController();
        }

        private void OnDisable()
        {
            UnsubscribeMapController();
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            IsVisible = true;
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            IsVisible = false;
        }

        public void Rebuild()
        {
            ClearViews();

            if (!TryGetMapController(out MapRuntimeController controller))
                return;

            if (!controller.HasInitialized || controller.ActData == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[TreeMapUIController] Cannot rebuild: map is not initialized or Act Data is missing.");
                }

                Refresh();
                return;
            }

            IReadOnlyList<MapNodeData> nodes = controller.ActData.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null || !node.HasValidNodeId)
                    continue;

                if (nodeButtonPrefab == null || nodeContainer == null)
                {
                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            "[TreeMapUIController] Node button prefab or node container is missing.");
                    }

                    break;
                }

                TreeMapNodeButtonUI view = Instantiate(nodeButtonPrefab, nodeContainer);
                RectTransform viewRect = view.transform as RectTransform;
                if (viewRect != null)
                {
                    SetCenterAnchors(viewRect);
                    viewRect.anchoredPosition = node.UiPosition * nodePositionScale;
                }

                view.Bind(node);
                TryApplyNodeIcon(view, node);
                view.OnNodeClicked += HandleNodeClicked;
                nodeViews[node.NodeId] = view;
            }

            BuildLines(controller.ActData);
            Refresh();
        }

        public void Refresh()
        {
            if (!TryGetMapController(out MapRuntimeController controller))
                return;

            string selectedNodeId = controller.SelectedNodeId;
            bool hasPendingSelectedNode = controller.HasSelectedNode &&
                                          !IsNodeCompleted(controller, selectedNodeId);

            if (verboseLogs && hasPendingSelectedNode)
            {
                Debug.Log(
                    $"[TreeMapUI] Pending visual lock active. Pending={selectedNodeId}");
            }

            foreach (KeyValuePair<string, TreeMapNodeButtonUI> pair in nodeViews)
            {
                string nodeId = pair.Key;
                MapNodeState state = controller.GetNodeState(nodeId);
                bool isSelected = !string.IsNullOrWhiteSpace(selectedNodeId) &&
                                  string.Equals(nodeId, selectedNodeId, StringComparison.Ordinal);

                TreeMapNodeVisualState visualState;
                bool interactable;

                if (hasPendingSelectedNode)
                {
                    if (isSelected)
                    {
                        visualState = TreeMapNodeVisualState.Current;
                        interactable = !isBattleStartInProgress &&
                                       controller.CanStartBattleFromNode(nodeId);
                    }
                    else if (state == MapNodeState.Completed)
                    {
                        visualState = TreeMapNodeVisualState.Completed;
                        interactable = false;
                    }
                    else
                    {
                        visualState = TreeMapNodeVisualState.Locked;
                        interactable = false;
                    }
                }
                else
                {
                    visualState = TreeMapNodeButtonUI.MapNodeStateToVisualState(state);
                    interactable = !isBattleStartInProgress &&
                                   controller.CanStartBattleFromNode(nodeId);
                }

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapUI] Node {nodeId}: visual={visualState}, interactable={interactable}");
                }

                pair.Value.RefreshState(state, isSelected, interactable, visualState);
            }

            RefreshSelectedNodeText(controller);
            RefreshStartBattleButton(controller);
            RefreshStatusText(controller);
        }

        private static bool IsNodeCompleted(MapRuntimeController controller, string nodeId)
        {
            if (controller == null || string.IsNullOrWhiteSpace(nodeId))
                return false;

            return controller.GetNodeState(nodeId) == MapNodeState.Completed;
        }

        public void SetBattleStartInProgress(bool inProgress)
        {
            isBattleStartInProgress = inProgress;
            Refresh();
        }

        public bool TryGetSelectedNode(out MapNodeData node)
        {
            node = null;

            if (mapRuntimeController == null)
                return false;

            return mapRuntimeController.TryGetSelectedNode(out node);
        }

        private void BuildLines(MapActData actData)
        {
            if (linePrefab == null || lineContainer == null || actData == null)
                return;

            IReadOnlyList<MapNodeData> nodes = actData.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData node = nodes[i];
                if (node == null || node.ConnectedNodeIds == null)
                    continue;

                Vector2 start = node.UiPosition * nodePositionScale;

                for (int j = 0; j < node.ConnectedNodeIds.Count; j++)
                {
                    string connectedId = node.ConnectedNodeIds[j];
                    if (!actData.TryGetNode(connectedId, out MapNodeData connectedNode))
                        continue;

                    TreeMapLineUI line = Instantiate(linePrefab, lineContainer);
                    RectTransform lineRect = line.transform as RectTransform;
                    if (lineRect != null)
                        SetCenterAnchors(lineRect);

                    Vector2 end = connectedNode.UiPosition * nodePositionScale;
                    line.Bind(start, end, lineColor, lineThickness);
                    lineViews.Add(line);
                }
            }
        }

        private void RefreshSelectedNodeText(MapRuntimeController controller)
        {
            if (selectedNodeText == null)
                return;

            if (controller.TryGetSelectedNode(out MapNodeData node))
            {
                string statusPrefix = controller.HasPendingEncounterNode
                    ? "Pending encounter"
                    : "Selected";

                selectedNodeText.text =
                    $"{statusPrefix}: {node.DisplayName}\n" +
                    $"Type: {node.NodeType}\n" +
                    $"Encounter: {(node.HasEncounter ? node.EncounterId : "none")}";
            }
            else
            {
                selectedNodeText.text = "Select an available node to start the encounter.";
            }
        }

        private void RefreshStartBattleButton(MapRuntimeController controller)
        {
            if (startBattleButton == null)
                return;

            if (hideStartBattleButtonInGameplay)
            {
                startBattleButton.interactable = false;
                return;
            }

            bool canStart = controller.HasSelectedNode &&
                            controller.TryGetSelectedNode(out MapNodeData node) &&
                            node.HasEncounter;

            startBattleButton.interactable = canStart;
        }

        private void RefreshStatusText(MapRuntimeController controller)
        {
            if (statusText == null || controller.CurrentMapState == null)
                return;

            RunMapState mapState = controller.CurrentMapState;
            statusText.text =
                $"Current: {mapState.CurrentNodeId}\n" +
                $"Selected: {mapState.SelectedNodeId}\n" +
                $"Available: {FormatNodeIdList(mapState.GetNodeIdsByState(MapNodeState.Available))}";
        }

        private void HandleNodeClicked(string nodeId)
        {
            if (isBattleStartInProgress)
            {
                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapUIController] Node click ignored: battle start already in progress. nodeId={nodeId}");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(nodeId))
                return;

            if (!TryGetMapController(out MapRuntimeController controller))
                return;

            if (!controller.CanStartBattleFromNode(nodeId))
            {
                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapUIController] Node click rejected: unavailable, completed, locked, " +
                        $"or pending different node. nodeId={nodeId}");
                }

                if (statusText != null)
                {
                    statusText.text =
                        $"Cannot start from '{nodeId}'. Choose an available node or re-enter the pending node.";
                }

                return;
            }

            OnNodeStartRequested?.Invoke(nodeId);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[TreeMapUIController] Node start requested: {nodeId}");
            }
        }

        private void HandleStartBattleClicked()
        {
            if (!TryGetSelectedNode(out MapNodeData node))
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[TreeMapUIController] Start Battle clicked with no valid selected node.");
                }

                return;
            }

            if (!node.HasEncounter)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[TreeMapUIController] Start Battle clicked but node '{node.NodeId}' has no encounter.");
                }

                return;
            }

            OnNodeStartRequested?.Invoke(node.NodeId);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[TreeMapUIController] Debug Start Battle requested for node={node.NodeId} | " +
                    $"encounter={node.EncounterId}");
            }
        }

        private void HandleMapInitialized(RunMapState _)
        {
            Rebuild();
        }

        private void HandleMapStateChanged()
        {
            if (rebuildOnMapStateChanged)
                Refresh();
        }

        private void HandleNodeSelected(MapNodeData _)
        {
            Refresh();
        }

        private void HandleNodeStateChanged(string _, MapNodeState __)
        {
            if (rebuildOnMapStateChanged)
                Refresh();
        }

        private void SubscribeMapController()
        {
            UnsubscribeMapController();

            if (mapRuntimeController == null)
                return;

            subscribedMapController = mapRuntimeController;
            subscribedMapController.OnMapInitialized += HandleMapInitialized;
            subscribedMapController.OnMapStateChanged += HandleMapStateChanged;
            subscribedMapController.OnNodeSelected += HandleNodeSelected;
            subscribedMapController.OnNodeStateChanged += HandleNodeStateChanged;
        }

        private void UnsubscribeMapController()
        {
            if (subscribedMapController == null)
                return;

            subscribedMapController.OnMapInitialized -= HandleMapInitialized;
            subscribedMapController.OnMapStateChanged -= HandleMapStateChanged;
            subscribedMapController.OnNodeSelected -= HandleNodeSelected;
            subscribedMapController.OnNodeStateChanged -= HandleNodeStateChanged;
            subscribedMapController = null;
        }

        private void ClearViews()
        {
            foreach (KeyValuePair<string, TreeMapNodeButtonUI> pair in nodeViews)
            {
                if (pair.Value == null)
                    continue;

                pair.Value.OnNodeClicked -= HandleNodeClicked;
                Destroy(pair.Value.gameObject);
            }

            nodeViews.Clear();

            for (int i = 0; i < lineViews.Count; i++)
            {
                if (lineViews[i] != null)
                    Destroy(lineViews[i].gameObject);
            }

            lineViews.Clear();
        }

        private bool TryGetMapController(out MapRuntimeController controller)
        {
            controller = mapRuntimeController;
            if (controller != null)
                return true;

            if (verboseLogs)
            {
                Debug.LogWarning(
                    "[TreeMapUIController] MapRuntimeController reference is missing.");
            }

            return false;
        }

        private static void SetCenterAnchors(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static string FormatNodeIdList(List<string> nodeIds)
        {
            if (nodeIds == null || nodeIds.Count == 0)
                return string.Empty;

            return string.Join(", ", nodeIds);
        }

        private void TryApplyNodeIcon(TreeMapNodeButtonUI view, MapNodeData node)
        {
            if (view == null || node == null)
                return;

            if (TryResolveNodeIcon(node.NodeType, out Sprite icon))
                view.SetNodeIcon(icon);
        }

        private bool TryResolveNodeIcon(MapNodeType nodeType, out Sprite icon)
        {
            icon = null;

            if (nodeTypeIcons == null || nodeTypeIcons.Count == 0)
                return false;

            for (int i = 0; i < nodeTypeIcons.Count; i++)
            {
                TreeMapNodeTypeIconEntry entry = nodeTypeIcons[i];
                if (entry == null || entry.icon == null)
                    continue;

                if (entry.nodeType == nodeType)
                {
                    icon = entry.icon;
                    return true;
                }
            }

            return false;
        }

        [Serializable]
        private class TreeMapNodeTypeIconEntry
        {
            public MapNodeType nodeType;
            public Sprite icon;
        }
    }
}
