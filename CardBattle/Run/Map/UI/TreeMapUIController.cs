using System;
using System.Collections;
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
        [SerializeField] private float lineThickness = 4f;

        [Header("Transition")]
        [SerializeField] private CanvasGroup mapCanvasGroup;
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool disableInteractionDuringTransition = true;
        [SerializeField] private bool deactivatePanelAfterFadeOut = true;
        [SerializeField] private bool showMapImmediateOnFirstOpen = true;
        [SerializeField] private bool verboseTransitionLogs;

        [Header("Options")]
        [SerializeField] private bool initializeMapOnStart = true;
        [SerializeField] private bool rebuildOnMapStateChanged = true;
        [SerializeField] private bool hideStartBattleButtonInGameplay = true;
        [SerializeField] private bool verboseLogs = true;

        private readonly Dictionary<string, TreeMapNodeButtonUI> nodeViews =
            new Dictionary<string, TreeMapNodeButtonUI>(StringComparer.Ordinal);

        private readonly List<TreeMapLineUI> lineViews = new List<TreeMapLineUI>();

        public bool IsVisible { get; private set; }
        public bool IsTransitioning { get; private set; }

        public event Action<string> OnNodeStartRequested;

        private MapRuntimeController subscribedMapController;
        private bool isBattleStartInProgress;
        private bool isMapInputLocked;
        private bool hasShownMapOnce;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            ResolveCanvasGroup();

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
            StopFadeRoutine();
            UnsubscribeMapController();
        }

        public void Show()
        {
            ShowMapImmediate();
        }

        public void Hide()
        {
            HideMapImmediate();
        }

        public void ShowMapImmediate()
        {
            StopFadeRoutine();
            IsTransitioning = false;
            isMapInputLocked = false;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            IsVisible = true;
            SetCanvasGroupState(1f, true, true);
        }

        public void HideMapImmediate()
        {
            StopFadeRoutine();
            IsTransitioning = false;
            isMapInputLocked = false;

            SetCanvasGroupState(0f, false, false);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            IsVisible = false;
        }

        public void ShowMapForRunEntry()
        {
            StopFadeRoutine();
            ShowMapImmediate();
            Refresh();

            if (showMapImmediateOnFirstOpen && !hasShownMapOnce)
            {
                LogTransition("First map open uses immediate show.");
                LogTransition("Map fade in skipped on first open.");
            }

            hasShownMapOnce = true;
        }

        public void ResetFirstMapOpenState()
        {
            hasShownMapOnce = false;
        }

        public void EnsureMapVisibleAndInteractive()
        {
            ShowMapImmediate();
            Refresh();
        }

        public void SetMapVisibleImmediate(bool visible)
        {
            if (visible)
                ShowMapImmediate();
            else
                HideMapImmediate();
        }

        public void LockMapInput()
        {
            isMapInputLocked = true;
            SetBattleStartInProgress(true);
            SetCanvasGroupInteractable(false);
            DisableAllNodeInteraction();
        }

        public void UnlockMapInput()
        {
            isMapInputLocked = false;
            SetBattleStartInProgress(false);
            Refresh();
        }

        public Coroutine FadeInMap()
        {
            StopFadeRoutine();
            fadeRoutine = StartCoroutine(FadeInMapRoutine());
            return fadeRoutine;
        }

        public Coroutine FadeOutMap()
        {
            StopFadeRoutine();
            fadeRoutine = StartCoroutine(FadeOutMapRoutine());
            return fadeRoutine;
        }

        public void PlayFadeIn()
        {
            FadeInMap();
        }

        public void PrepareMapForFadeIn()
        {
            StopFadeRoutine();
            IsTransitioning = false;
            isMapInputLocked = false;
            SetBattleStartInProgress(false);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            IsVisible = true;
            SetCanvasGroupState(0f, false, false);
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

                pair.Value.RefreshState(state, isSelected, interactable, visualState);
            }

            RefreshLineStates(controller, hasPendingSelectedNode, selectedNodeId);

            RefreshSelectedNodeText(controller);
            RefreshStartBattleButton(controller);
            RefreshStatusText(controller);
        }

        private bool IsNodeInteractionAllowed(MapRuntimeController controller, string nodeId)
        {
            if (isBattleStartInProgress || isMapInputLocked || IsTransitioning)
                return false;

            return controller.CanStartBattleFromNode(nodeId);
        }

        private static bool IsNodeCompleted(MapRuntimeController controller, string nodeId)
        {
            if (controller == null || string.IsNullOrWhiteSpace(nodeId))
                return false;

            return controller.GetNodeState(nodeId) == MapNodeState.Completed;
        }

        private static bool IsNodeAvailable(MapRuntimeController controller, string nodeId)
        {
            if (controller == null || string.IsNullOrWhiteSpace(nodeId))
                return false;

            return controller.GetNodeState(nodeId) == MapNodeState.Available;
        }

        private static bool HasUnresolvedPendingNode(MapRuntimeController controller)
        {
            if (controller == null || !controller.HasSelectedNode)
                return false;

            return !IsNodeCompleted(controller, controller.SelectedNodeId);
        }

        private static bool IsPendingSelectedNode(
            MapRuntimeController controller,
            string nodeId,
            string pendingNodeId)
        {
            if (controller == null || string.IsNullOrWhiteSpace(nodeId) ||
                string.IsNullOrWhiteSpace(pendingNodeId))
            {
                return false;
            }

            return string.Equals(nodeId, pendingNodeId, StringComparison.Ordinal);
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
                    line.Bind(start, end, lineThickness, node.NodeId, connectedId);
                    line.SetVisualState(TreeMapLineVisualState.Locked);
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
            if (IsTransitioning)
            {
                if (verboseTransitionLogs)
                {
                    Debug.Log(
                        "[TreeMapTransition] Click rejected because transition is active.");
                }

                return;
            }

            if (isMapInputLocked || isBattleStartInProgress)
            {
                if (verboseLogs || verboseTransitionLogs)
                {
                    Debug.Log(
                        $"[TreeMapUIController] Node click ignored: map input is locked. nodeId={nodeId}");
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

        private void ResolveCanvasGroup()
        {
            if (mapCanvasGroup != null)
                return;

            if (panelRoot != null)
                mapCanvasGroup = panelRoot.GetComponent<CanvasGroup>();

            if (mapCanvasGroup == null)
                mapCanvasGroup = GetComponent<CanvasGroup>();

            if (mapCanvasGroup == null && panelRoot != null)
            {
                mapCanvasGroup = panelRoot.AddComponent<CanvasGroup>();

                if (verboseTransitionLogs)
                {
                    Debug.Log(
                        "[TreeMapTransition] Added CanvasGroup to MapPanel at runtime.");
                }
            }
            else if (mapCanvasGroup == null && verboseTransitionLogs)
            {
                Debug.LogWarning(
                    "[TreeMapTransition] Map CanvasGroup is missing. Fade transitions will be skipped.");
            }
        }

        private IEnumerator FadeInMapRoutine()
        {
            IsTransitioning = true;
            ResolveCanvasGroup();

            if (panelRoot != null)
                panelRoot.SetActive(true);

            IsVisible = true;
            isMapInputLocked = false;

            if (disableInteractionDuringTransition)
                SetCanvasGroupInteractable(false);

            LogTransition("Fade in started.");

            if (mapCanvasGroup == null || fadeInDuration <= 0f)
            {
                SetCanvasGroupState(1f, true, true);
                IsTransitioning = false;
                fadeRoutine = null;
                Refresh();
                LogTransition("Fade in complete.");
                yield break;
            }

            float startAlpha = mapCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                SetCanvasGroupAlpha(Mathf.Lerp(startAlpha, 1f, t));
                yield return null;
            }

            SetCanvasGroupState(1f, true, true);
            IsTransitioning = false;
            fadeRoutine = null;
            Refresh();
            LogTransition("Fade in complete.");
        }

        private IEnumerator FadeOutMapRoutine()
        {
            IsTransitioning = true;
            isMapInputLocked = true;
            ResolveCanvasGroup();

            SetBattleStartInProgress(true);
            SetCanvasGroupInteractable(false);
            DisableAllNodeInteraction();

            LogTransition("Fade out started.");

            if (mapCanvasGroup == null || fadeOutDuration <= 0f)
            {
                SetCanvasGroupState(0f, false, false);

                if (deactivatePanelAfterFadeOut && panelRoot != null)
                    panelRoot.SetActive(false);

                IsVisible = false;
                IsTransitioning = false;
                fadeRoutine = null;
                LogTransition("Fade out complete.");
                yield break;
            }

            float startAlpha = mapCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                SetCanvasGroupAlpha(Mathf.Lerp(startAlpha, 0f, t));
                yield return null;
            }

            SetCanvasGroupState(0f, false, false);

            if (deactivatePanelAfterFadeOut && panelRoot != null)
                panelRoot.SetActive(false);

            IsVisible = false;
            IsTransitioning = false;
            fadeRoutine = null;
            LogTransition("Fade out complete.");
        }

        private void StopFadeRoutine()
        {
            if (fadeRoutine == null)
                return;

            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
            IsTransitioning = false;
        }

        private void SetCanvasGroupState(float alpha, bool interactable, bool blocksRaycasts)
        {
            if (mapCanvasGroup == null)
                return;

            mapCanvasGroup.alpha = alpha;
            mapCanvasGroup.interactable = interactable;
            mapCanvasGroup.blocksRaycasts = blocksRaycasts;
        }

        private void SetCanvasGroupAlpha(float alpha)
        {
            if (mapCanvasGroup == null)
                return;

            mapCanvasGroup.alpha = alpha;
        }

        private void SetCanvasGroupInteractable(bool interactable)
        {
            if (mapCanvasGroup == null)
                return;

            mapCanvasGroup.interactable = interactable;
            mapCanvasGroup.blocksRaycasts = interactable;
        }

        private void DisableAllNodeInteraction()
        {
            foreach (KeyValuePair<string, TreeMapNodeButtonUI> pair in nodeViews)
            {
                if (pair.Value != null)
                    pair.Value.SetInteractable(false);
            }
        }

        private void LogTransition(string message)
        {
            if (!verboseTransitionLogs)
                return;

            Debug.Log($"[TreeMapTransition] {message}");
        }
    }
}
