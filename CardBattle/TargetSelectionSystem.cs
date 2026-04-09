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

        public void BeginTargetSelection(CardInstance card)
        {
            if (card?.Data == null)
                return;

            _pendingCard = card;
            Debug.Log($"Selecting target for card: {card.Data.DisplayName}");
        }

        public void CancelTargetSelection()
        {
            _pendingCard = null;
            Debug.Log("Target selection cancelled.");
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