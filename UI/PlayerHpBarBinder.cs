using UnityEngine;

namespace CardBattle.Core
{
    public class PlayerHpBarBinder : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HpBarUI hpBarUI;

        private void OnEnable()
        {
            if (player != null)
                player.OnHpChangedEvent += HandleHpChanged;
        }

        private void Start()
        {
            RefreshNow();
        }

        private void OnDisable()
        {
            if (player != null)
                player.OnHpChangedEvent -= HandleHpChanged;
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            if (hpBarUI != null)
                hpBarUI.SetHp(currentHp, maxHp);
        }

        [ContextMenu("Refresh Now")]
        public void RefreshNow()
        {
            if (player == null || hpBarUI == null)
                return;

            hpBarUI.SetHp(player.CurrentHp, player.MaxHp);
        }
    }
}