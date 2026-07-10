using System;
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private CanvasGroup mapCanvasGroup;

        [Header("Layout")]
        [SerializeField] private float nodePositionScale = 1f;
        [SerializeField] private float lineThickness = 4f;

        [Header("Options")]
        [SerializeField] private bool initializeMapOnStart = true;
        [SerializeField] private bool rebuildOnMapStateChanged = true;
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
            ResolveCanvasGroup();
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
            SetCanvasGroupState(1f, true, true);
        }

        public void Hide()
        {
            SetCanvasGroupState(0f, false, false);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            IsVisible = false;
        }

        public void EnsureMapVisibleAndInteractive()
        {
            Show();
            Refresh();
        }

        public void SetMapVisibleImmediate(bool visible)
        {
            if (visible)
                Show();
            else
                Hide();
        }

        public void ResetMapInteractionStateForNewRun()
        {
            isBattleStartInProgress = false;
            SetCanvasGroupState(IsVisible ? 1f : 0f, IsVisible, IsVisible);

            if (!isActiveAndEnabled)
                return;

            Refresh();
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
            if (!isActiveAndEnabled)
                return;

            if (!TryGetMapController(out MapRuntimeController controller))
                return;

            RemoveDestroyedViewReferences();

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
                if (pair.Value == null)
                    continue;

                string nodeId = pair.Key;
                MapNodeState state = controller.GetNodeState(nodeId);

                TreeMapNodeVisualState visualState;
                bool interactable;

                if (hasPendingSelectedNode)
                {
                    bool isSelected = string.Equals(nodeId, selectedNodeId, StringComparison.Ordinal);

                    if (isSelected)
                    {
                        visualState = TreeMapNodeVisualState.Current;
                        interactable = IsNodeInteractionAllowed(controller, nodeId);
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
                    interactable = IsNodeInteractionAllowed(controller, nodeId);
                }

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapUI] Node {nodeId}: visual={visualState}, interactable={interactable}");
                }

                pair.Value.RefreshState(state, interactable, visualState);
            }

            RefreshLineStates(controller, hasPendingSelectedNode, selectedNodeId);
        }

        private bool IsNodeInteractionAllowed(MapRuntimeController controller, string nodeId)
        {
            if (isBattleStartInProgress)
                return false;

            return controller.CanStartBattleFromNode(nodeId);
        }

        private static bool IsNodeCompleted(MapRuntimeController controller, string nodeId)
        {
            if (controller == null || string.IsNullOrWhiteSpace(nodeId))
                return false;

            return controller.GetNodeState(nodeId) == MapNodeState.Completed;
        }

        private void RefreshLineStates(
            MapRuntimeController controller,
            bool hasPendingSelectedNode,
            string pendingNodeId)
        {
            for (int i = 0; i < lineViews.Count; i++)
            {
                TreeMapLineUI line = lineViews[i];
                if (line == null)
                    continue;

                TreeMapLineVisualState lineVisualState = ResolveLineVisualState(
                    controller,
                    line.FromNodeId,
                    line.ToNodeId,
                    hasPendingSelectedNode,
                    pendingNodeId);

                line.SetVisualState(lineVisualState);

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapUI] Line {line.FromNodeId}->{line.ToNodeId}: visual={lineVisualState}");
                }
            }
        }

        private static TreeMapLineVisualState ResolveLineVisualState(
            MapRuntimeController controller,
            string fromNodeId,
            string toNodeId,
            bool hasPendingSelectedNode,
            string pendingNodeId)
        {
            if (controller == null ||
                string.IsNullOrWhiteSpace(fromNodeId) ||
                string.IsNullOrWhiteSpace(toNodeId))
            {
                return TreeMapLineVisualState.Locked;
            }

            MapNodeState fromState = controller.GetNodeState(fromNodeId);
            MapNodeState toState = controller.GetNodeState(toNodeId);

            if (hasPendingSelectedNode)
            {
                if (IsLineCurrentPath(fromState, toState, fromNodeId, toNodeId, pendingNodeId))
                    return TreeMapLineVisualState.Current;

                if (IsLineCompletedPath(fromState, toState))
                    return TreeMapLineVisualState.Completed;

                return TreeMapLineVisualState.Locked;
            }

            if (IsLineCompletedPath(fromState, toState))
                return TreeMapLineVisualState.Completed;

            if (fromState == MapNodeState.Completed && toState == MapNodeState.Available)
                return TreeMapLineVisualState.Available;

            return TreeMapLineVisualState.Locked;
        }

        private static bool IsLineCurrentPath(
            MapNodeState fromState,
            MapNodeState toState,
            string fromNodeId,
            string toNodeId,
            string pendingNodeId)
        {
            if (string.IsNullOrWhiteSpace(pendingNodeId))
                return false;

            if (!string.Equals(toNodeId, pendingNodeId, StringComparison.Ordinal))
                return false;

            return fromState == MapNodeState.Completed &&
                   (toState == MapNodeState.Current || toState == MapNodeState.Available);
        }

        private static bool IsLineCompletedPath(MapNodeState fromState, MapNodeState toState)
        {
            return fromState == MapNodeState.Completed && toState == MapNodeState.Completed;
        }

        public void SetBattleStartInProgress(bool inProgress)
        {
            isBattleStartInProgress = inProgress;

            if (!isActiveAndEnabled)
                return;

            Refresh();
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
                    line.Bind(start, end, lineThickness, node.NodeId, connectedId);
                    line.SetVisualState(TreeMapLineVisualState.Locked);
                    lineViews.Add(line);
                }
            }
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

                return;
            }

            OnNodeStartRequested?.Invoke(nodeId);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[TreeMapUIController] Node start requested: {nodeId}");
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

        private void RemoveDestroyedViewReferences()
        {
            if (nodeViews.Count > 0)
            {
                var destroyedNodeIds = new List<string>();

                foreach (KeyValuePair<string, TreeMapNodeButtonUI> pair in nodeViews)
                {
                    if (pair.Value == null)
                        destroyedNodeIds.Add(pair.Key);
                }

                for (int i = 0; i < destroyedNodeIds.Count; i++)
                    nodeViews.Remove(destroyedNodeIds[i]);
            }

            for (int i = lineViews.Count - 1; i >= 0; i--)
            {
                if (lineViews[i] == null)
                    lineViews.RemoveAt(i);
            }
        }

        private void ResolveCanvasGroup()
        {
            if (mapCanvasGroup != null)
                return;

            if (panelRoot != null)
                mapCanvasGroup = panelRoot.GetComponent<CanvasGroup>();

            if (mapCanvasGroup == null)
                mapCanvasGroup = GetComponent<CanvasGroup>();
        }

        private void SetCanvasGroupState(float alpha, bool interactable, bool blocksRaycasts)
        {
            if (mapCanvasGroup == null)
                return;

            mapCanvasGroup.alpha = alpha;
            mapCanvasGroup.interactable = interactable;
            mapCanvasGroup.blocksRaycasts = blocksRaycasts;
        }

        [Serializable]
        private class TreeMapNodeTypeIconEntry
        {
            public MapNodeType nodeType;
            public Sprite icon;
        }
    }
}
