using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class MapRuntimeDebugTest : MonoBehaviour
    {
        [SerializeField] private MapRuntimeController mapRuntimeController;
        [SerializeField] private RuntimeEncounterContext runtimeEncounterContext;
        [SerializeField] private string nodeIdToSelect = "act1_normal_a";
        [SerializeField] private bool autoRunDebugFlowOnStart;

        private void Start()
        {
            if (autoRunDebugFlowOnStart)
                DebugRunPhase6AFlow();
        }

        [ContextMenu("Debug Initialize Map")]
        private void DebugInitializeMap()
        {
            if (!TryGetController())
                return;

            bool initialized = mapRuntimeController.InitializeMap();
            Debug.Log($"[MapRuntimeDebugTest] InitializeMap => {initialized}");
        }

        [ContextMenu("Debug Select Node")]
        private void DebugSelectNode()
        {
            if (!TryGetController())
                return;

            bool selected = mapRuntimeController.TrySelectNode(nodeIdToSelect);
            Debug.Log($"[MapRuntimeDebugTest] TrySelectNode('{nodeIdToSelect}') => {selected}");
        }

        [ContextMenu("Debug Complete Selected Node")]
        private void DebugCompleteSelectedNode()
        {
            if (!TryGetController())
                return;

            bool completed = mapRuntimeController.TryCompleteSelectedNode();
            Debug.Log($"[MapRuntimeDebugTest] TryCompleteSelectedNode => {completed}");
        }

        [ContextMenu("Debug Print Map State")]
        private void DebugPrintMapState()
        {
            if (!TryGetController())
                return;

            mapRuntimeController.DebugPrintMapState();
        }

        [ContextMenu("Debug Run Phase 6A Flow")]
        private void DebugRunPhase6AFlow()
        {
            if (!TryGetController())
                return;

            Debug.Log("[MapRuntimeDebugTest] === Phase 6A Flow Start ===");

            if (!mapRuntimeController.InitializeMap())
            {
                Debug.LogError("[MapRuntimeDebugTest] === Phase 6A Flow Failed (Initialize) ===");
                return;
            }

            mapRuntimeController.DebugPrintMapState();

            List<string> availableNodes =
                mapRuntimeController.CurrentMapState.GetNodeIdsByState(MapNodeState.Available);

            if (availableNodes.Count == 0)
            {
                Debug.LogError(
                    "[MapRuntimeDebugTest] === Phase 6A Flow Failed (No Available Nodes) ===");
                return;
            }

            string firstAvailable = availableNodes[0];

            if (!mapRuntimeController.TrySelectNode(firstAvailable))
            {
                Debug.LogError(
                    "[MapRuntimeDebugTest] === Phase 6A Flow Failed (Select Node) ===");
                return;
            }

            RuntimeEncounterContext context = ResolveEncounterContext();
            if (context != null)
            {
                Debug.Log(
                    $"[MapRuntimeDebugTest] RuntimeEncounterContext.CurrentEncounterId=" +
                    $"{context.CurrentEncounterId}");
            }

            if (!mapRuntimeController.TryCompleteSelectedNode())
            {
                Debug.LogError(
                    "[MapRuntimeDebugTest] === Phase 6A Flow Failed (Complete Node) ===");
                return;
            }

            mapRuntimeController.DebugPrintMapState();
            Debug.Log("[MapRuntimeDebugTest] === Phase 6A Flow Success ===");
        }

        private RuntimeEncounterContext ResolveEncounterContext()
        {
            if (runtimeEncounterContext != null)
                return runtimeEncounterContext;

            return FindFirstObjectByType<RuntimeEncounterContext>();
        }

        private bool TryGetController()
        {
            if (mapRuntimeController != null)
                return true;

            Debug.LogError(
                "[MapRuntimeDebugTest] MapRuntimeController reference is missing.");
            return false;
        }
    }
}
