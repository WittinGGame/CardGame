using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class TargetSelectionSystem : MonoBehaviour
    {
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private EnemyTargetHighlight[] enemyHighlights;
        [SerializeField] private TargetGuideLineUI guideLine;

        public bool IsSelectingTarget => _pendingCard != null;

        private CardInstance _pendingCard;

        private void Update()
        {
            if (!IsSelectingTarget)
                return;

            // คลิกขวา
            if (Input.GetMouseButtonDown(1))
            {
                CancelTargetSelection();
                return;
            }

            // กด ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargetSelection();
                return;
            }

            if (IsSelectingTarget && guideLine != null)
            {
                Transform hoveredEnemyAnchor = GetHoveredEnemyAnchor();
                if (hoveredEnemyAnchor != null)
                    guideLine.UpdateTowardWorld(hoveredEnemyAnchor);
                else
                    guideLine.UpdateTowardScreen(Input.mousePosition);
            }
        }

        public void BeginTargetSelection(CardInstance card)
        {
            if (card?.Data == null)
                return;

            _pendingCard = card;

            SetHighlight(true);

            if (handUIController != null && guideLine != null)
            {
                var view = handUIController.GetViewForCard(card);
                if (view != null)
                {
                    var startAnchor = view.GuideStartAnchor != null
                        ? view.GuideStartAnchor
                        : view.LayoutRect;

                    guideLine.ShowFromCard(startAnchor);
                }
            }

            Debug.Log($"Selecting target for card: {card.Data.DisplayName}");
        }

        public void CancelTargetSelection()
        {
            if (_pendingCard == null)
                return;

            Debug.Log("Target selection cancelled.");

            _pendingCard = null;

            SetHighlight(false);

            if (handUIController != null)
                handUIController.DeselectCurrentCard();

            if (guideLine != null)
                guideLine.Hide();
        }

        public void ConfirmTarget(EnemyBattleUnit target)
        {
            if (_pendingCard == null || battleActionRunner == null || target == null || !target.IsAlive)
                return;

            SetHighlight(false);

            battleActionRunner.TryPlayCard(_pendingCard, target);
            _pendingCard = null;

            if (guideLine != null)
                guideLine.Hide();
        }

        private void SetHighlight(bool value)
        {
            if (enemyHighlights == null)
                return;

            for (int i = 0; i < enemyHighlights.Length; i++)
            {
                if (enemyHighlights[i] != null)
                    enemyHighlights[i].SetSelectable(value);
            }
        }

        private Transform GetHoveredEnemyAnchor()
        {
            if (EventSystem.current == null)
                return null;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                var go = raycastResults[i].gameObject;
                if (go == null)
                    continue;

                var unit = go.GetComponentInParent<EnemyBattleUnit>();
                if (unit != null && unit.IsAlive)
                {
                    if (unit.UIAnchorTargetGuide != null)
                        return unit.UIAnchorTargetGuide;

                    if (unit.UIAnchorDamage != null)
                        return unit.UIAnchorDamage;

                    if (unit.UIAnchorHP != null)
                        return unit.UIAnchorHP;

                    return unit.transform;
                }
            }

            // TODO: Swap to a dedicated hover-tracking source if introduced later.
            return null;
        }
    }
}