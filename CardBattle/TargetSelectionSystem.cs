using UnityEngine;

namespace CardBattle.Core
{
    public class TargetSelectionSystem : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HandUIController handUIController;

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
            if (_pendingCard == null || player == null || target == null || !target.IsAlive)
                return;

            bool success = player.TryPlayCard(_pendingCard, target);

            if (success)
            {
                Debug.Log($"Played {_pendingCard.Data.DisplayName} on {target.name}");
            }
            else
            {
                Debug.LogWarning($"Failed to play {_pendingCard.Data.DisplayName} on {target.name}");
            }

            _pendingCard = null;

            if (handUIController != null)
                handUIController.RefreshHandUI();
        }
    }
}