using System.Collections;
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

        [Header("Save")]
        [SerializeField] private ActiveRunAutoSaveController activeRunAutoSaveController;

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
        private bool isStartingBattle;
        private Coroutine battleStartRoutine;

        private void OnEnable()
        {
            SubscribeTreeMapUI();
            SubscribeEncounterCompletion();
        }

        private void OnDisable()
        {
            UnsubscribeTreeMapUI();
            UnsubscribeEncounterCompletion();
            StopBattleStartRoutine();
        }

        public void HandleNodeClickedForBattle(string nodeId)
        {
            if (battleStartRoutine != null)
                return;

            battleStartRoutine = StartCoroutine(HandleNodeClickedForBattleRoutine(nodeId));
        }

        private IEnumerator HandleNodeClickedForBattleRoutine(string nodeId)
        {
            if (isStartingBattle)
            {
                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapBattleFlow] Node click ignored: battle start already in progress. nodeId={nodeId}");
                }

                battleStartRoutine = null;
                yield break;
            }

            if (treeMapUIController != null && treeMapUIController.IsTransitioning)
            {
                if (verboseLogs)
                {
                    Debug.Log(
                        "[TreeMapTransition] Click rejected because transition is active.");
                }

                battleStartRoutine = null;
                yield break;
            }

            if (string.IsNullOrWhiteSpace(nodeId))
            {
                LogError("Cannot start battle: node ID is blank.");
                battleStartRoutine = null;
                yield break;
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[TreeMapBattleFlow] Node clicked start requested: {nodeId}");
            }

            if (mapRuntimeController == null)
            {
                LogError("Cannot start battle: MapRuntimeController is missing.");
                battleStartRoutine = null;
                yield break;
            }

            if (!mapRuntimeController.CanStartBattleFromNode(nodeId))
            {
                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapBattleFlow] Node click rejected: unavailable, completed, locked, " +
                        $"or pending different node. nodeId={nodeId}");
                }

                battleStartRoutine = null;
                yield break;
            }

            isStartingBattle = true;
            treeMapUIController?.LockMapInput();

            bool battleStarted = false;

            try
            {
                if (!mapRuntimeController.TrySelectNode(nodeId))
                {
                    if (verboseLogs)
                    {
                        Debug.Log(
                            $"[TreeMapBattleFlow] Node click rejected: selection failed. nodeId={nodeId}");
                    }

                    yield break;
                }

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[TreeMapBattleFlow] Node selected/locked: {nodeId}");
                }

                treeMapUIController?.Refresh();

                if (activeRunAutoSaveController != null)
                {
                    bool saved = activeRunAutoSaveController.SaveNow("NodeClickedStartBattle");
                    if (!saved)
                    {
                        if (verboseLogs)
                        {
                            Debug.LogWarning(
                                "[TreeMapBattleFlow] Active run save failed before battle start. Aborting battle start.");
                        }

                        yield break;
                    }

                    if (verboseLogs)
                    {
                        Debug.Log(
                            "[TreeMapBattleFlow] Active run saved before battle start.");
                    }
                }

                if (battleTestBootstrap == null)
                {
                    LogError("Cannot start battle: BattleTestBootstrap is missing.");
                    yield break;
                }

                if (hideMapWhenBattleStarts && treeMapUIController != null)
                {
                    yield return treeMapUIController.FadeOutMap();
                }

                if (verboseLogs)
                {
                    if (mapRuntimeController.TryGetSelectedNode(out MapNodeData node))
                    {
                        Debug.Log(
                            $"[TreeMapBattleFlow] Battle start requested for node: {nodeId} | " +
                            $"encounter={node.EncounterId}");
                    }
                    else
                    {
                        Debug.Log(
                            $"[TreeMapBattleFlow] Battle start requested for node: {nodeId}");
                    }
                }

                battleTestBootstrap.StartTestBattle();
                battleStarted = true;
            }
            finally
            {
                if (!battleStarted)
                    treeMapUIController?.UnlockMapInput();

                isStartingBattle = false;
                battleStartRoutine = null;
            }
        }

        private void HandleNodeStartRequested(string nodeId)
        {
            HandleNodeClickedForBattle(nodeId);
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
                treeMapUIController.PrepareMapForFadeIn();
                treeMapUIController.Refresh();
                treeMapUIController.PlayFadeIn();
            }

            if (verboseLogs)
            {
                Debug.Log(
                    "[TreeMapBattleFlow] Encounter completed. Returning to map.");
            }
        }

        private void StopBattleStartRoutine()
        {
            if (battleStartRoutine == null)
                return;

            StopCoroutine(battleStartRoutine);
            battleStartRoutine = null;
            isStartingBattle = false;
        }

        private void SubscribeTreeMapUI()
        {
            UnsubscribeTreeMapUI();

            if (treeMapUIController == null)
                return;

            subscribedTreeMapUI = treeMapUIController;
            subscribedTreeMapUI.OnNodeStartRequested += HandleNodeStartRequested;
        }

        private void UnsubscribeTreeMapUI()
        {
            if (subscribedTreeMapUI == null)
                return;

            subscribedTreeMapUI.OnNodeStartRequested -= HandleNodeStartRequested;
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
