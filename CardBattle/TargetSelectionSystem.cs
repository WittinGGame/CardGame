using UnityEngine;

namespace CardBattle.Core
{
    public class TargetSelectionSystem : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;

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
        }

        public void BeginTargetSelection(CardInstance card)
        {
            if (card?.Data == null)
                return;

            _pendingCard = card;
            Debug.Log($"Selecting target for card: {card.Data.DisplayName}");
        }

        public void CancelTargetSelection()
        {
            if (_pendingCard == null)
                return;

            Debug.Log("Target selection cancelled.");

            _pendingCard = null;

            if (handUIController != null)
                handUIController.DeselectCurrentCard();
        }

        public void ConfirmTarget(EnemyBattleUnit target)
        {
            if (_pendingCard == null || battleActionRunner == null || target == null || !target.IsAlive)
                return;

            battleActionRunner.TryPlayCard(_pendingCard, target);
            _pendingCard = null;

            if (handUIController != null)
                handUIController.RefreshHandUI();
        }
    }
}