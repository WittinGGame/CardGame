using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    // Layout only. Owned-card selection and eligibility remain in the existing panel/controller.
    public class BonfireUpgradeDeckUI : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private ScrollRect scrollRect; // Retained for existing serialized prefab wiring.
        private void OnEnable() { RefreshLayout(); }
        private void OnRectTransformDimensionsChange() { RefreshLayout(); }
        public void RefreshLayout()
        {
            if (grid == null) return;
            // Cell size and spacing belong to the prefab designer, never the viewport.
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
        }
    }
}
