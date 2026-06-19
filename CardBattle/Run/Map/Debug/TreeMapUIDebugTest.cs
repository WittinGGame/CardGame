using UnityEngine;

namespace CardBattle.Core
{
    public class TreeMapUIDebugTest : MonoBehaviour
    {
        [SerializeField] private TreeMapUIController treeMapUIController;
        [SerializeField] private MapRuntimeController mapRuntimeController;

        [ContextMenu("Debug Show Map")]
        private void DebugShowMap()
        {
            if (!TryGetTreeMapUI())
                return;

            treeMapUIController.Show();
            Debug.Log("[TreeMapUIDebugTest] Map shown.");
        }

        [ContextMenu("Debug Hide Map")]
        private void DebugHideMap()
        {
            if (!TryGetTreeMapUI())
                return;

            treeMapUIController.Hide();
            Debug.Log("[TreeMapUIDebugTest] Map hidden.");
        }

        [ContextMenu("Debug Rebuild Map UI")]
        private void DebugRebuildMapUI()
        {
            if (!TryGetTreeMapUI())
                return;

            treeMapUIController.Rebuild();
            Debug.Log("[TreeMapUIDebugTest] Map UI rebuilt.");
        }

        [ContextMenu("Debug Refresh Map UI")]
        private void DebugRefreshMapUI()
        {
            if (!TryGetTreeMapUI())
                return;

            treeMapUIController.Refresh();
            Debug.Log("[TreeMapUIDebugTest] Map UI refreshed.");
        }

        [ContextMenu("Debug Print Map State")]
        private void DebugPrintMapState()
        {
            if (mapRuntimeController == null)
            {
                Debug.LogError(
                    "[TreeMapUIDebugTest] MapRuntimeController reference is missing.");
                return;
            }

            mapRuntimeController.DebugPrintMapState();
        }

        private bool TryGetTreeMapUI()
        {
            if (treeMapUIController != null)
                return true;

            Debug.LogError(
                "[TreeMapUIDebugTest] TreeMapUIController reference is missing.");
            return false;
        }
    }
}
