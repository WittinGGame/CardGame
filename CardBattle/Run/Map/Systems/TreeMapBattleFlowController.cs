using UnityEngine;

namespace CardBattle.Core
{
    public class TreeMapBattleFlowController : MonoBehaviour
    {
        [Header("Map UI")]
        [SerializeField] private TreeMapUIController treeMapUIController;

        [Header("Map Runtime")]
        [SerializeField] private MapRuntimeController mapRuntimeController;

        [Header("Battle")]
        [SerializeField] private BattleTestBootstrap battleTestBootstrap;

        [Header("Encounter Flow")]
        [SerializeField] private EncounterCompletionController encounterCompletionController;
        [SerializeField] private EncounterFlowResetController encounterFlowResetController;

        [Header("Run End")]
        [SerializeField] private RunEndController runEndController;

        [Header("Options")]
        [SerializeField] private bool hideMapWhenBattleStarts = true;
        [SerializeField] private bool showMapAfterEncounterCompletion = true;
        [SerializeField] private bool completeSelectedNodeOnEncounterCompletion = true;
        [SerializeField] private bool prepareNextEncounterStateAfterCompletion = true;
        [SerializeField] private bool verboseLogs = true;

        private TreeMapUIController subscribedTreeMapUI;
        private EncounterCompletionController subscribedEncounterCompletion;

        private void OnEnable()
        {
            SubscribeTreeMapUI();
            SubscribeEncounterCompletion();
        }

        private void OnDisable()
        {
            UnsubscribeTreeMapUI();
            UnsubscribeEncounterCompletion();
        }

        private void HandleStartBattleRequested(MapNodeData node)
        {
            if (node == null)
            {
                LogError("Cannot start battle: node is null.");
                return;
            }

            if (mapRuntimeController == null)
            {
                LogError("Cannot start battle: MapRuntimeController is missing.");
                return;
            }

            if (!mapRuntimeController.HasSelectedNode)
            {
                LogError("Cannot start battle: no node is selected on the map.");
                return;
            }

            if (battleTestBootstrap == null)
            {
                LogError("Cannot start battle: BattleTestBootstrap is missing.");
                return;
            }

            if (hideMapWhenBattleStarts && treeMapUIController != null)
                treeMapUIController.Hide();

            battleTestBootstrap.StartTestBattle();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[TreeMapBattleFlow] Starting battle from node={node.NodeId} | " +
                    $"encounter={node.EncounterId}");
            }
        }

        private void HandleEncounterCompletionReady()
        {
            MapNodeData completedNode = null;
            if (mapRuntimeController != null &&
                mapRuntimeController.TryGetSelectedNode(out MapNodeData selectedNode))
            {
                completedNode = selectedNode;
            }

            if (completeSelectedNodeOnEncounterCompletion && mapRuntimeController != null)
            {
                bool completed = mapRuntimeController.TryCompleteSelectedNode();

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapBattleFlow] TryCompleteSelectedNode => {completed}");
                }
            }

            bool isBossNode = completedNode != null &&
                              completedNode.NodeType == MapNodeType.Boss;

            if (isBossNode)
            {
                if (runEndController != null)
                {
                    runEndController.TryCompleteRun(completedNode.NodeId);
                }
                else if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[TreeMapBattleFlow] Boss node completed, but RunEndController is missing.");
                }

                if (verboseLogs)
                {
                    Debug.Log(
                        "[TreeMapBattleFlow] Boss node completed. Ending run.");
                }

                return;
            }

            if (prepareNextEncounterStateAfterCompletion && encounterFlowResetController != null)
            {
                bool prepared = encounterFlowResetController.TryPrepareNextEncounterState();

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapBattleFlow] TryPrepareNextEncounterState => {prepared}");
                }
            }

            if (showMapAfterEncounterCompletion && treeMapUIController != null)
            {
                treeMapUIController.Show();
                treeMapUIController.Refresh();
            }

            if (verboseLogs)
            {
                Debug.Log(
                    "[TreeMapBattleFlow] Encounter completed. Returning to map.");
            }
        }

        private void SubscribeTreeMapUI()
        {
            UnsubscribeTreeMapUI();

            if (treeMapUIController == null)
                return;

            subscribedTreeMapUI = treeMapUIController;
            subscribedTreeMapUI.OnStartBattleRequested += HandleStartBattleRequested;
        }

        private void UnsubscribeTreeMapUI()
        {
            if (subscribedTreeMapUI == null)
                return;

            subscribedTreeMapUI.OnStartBattleRequested -= HandleStartBattleRequested;
            subscribedTreeMapUI = null;
        }

        private void SubscribeEncounterCompletion()
        {
            UnsubscribeEncounterCompletion();

            if (encounterCompletionController == null)
                return;

            subscribedEncounterCompletion = encounterCompletionController;
            subscribedEncounterCompletion.OnEncounterCompletionReady += HandleEncounterCompletionReady;
        }

        private void UnsubscribeEncounterCompletion()
        {
            if (subscribedEncounterCompletion == null)
                return;

            subscribedEncounterCompletion.OnEncounterCompletionReady -= HandleEncounterCompletionReady;
            subscribedEncounterCompletion = null;
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TreeMapBattleFlow] {message}");
        }
    }
}
