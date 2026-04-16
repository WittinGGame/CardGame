using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class BattleHUDController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private BattleActionRunner battleActionRunner;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerApText;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private EnemyStatusUI enemyStatusUI1;
        [SerializeField] private EnemyStatusUI enemyStatusUI2;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private HpBarUI playerHpBar;

        private void Start()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnClickEndTurn);
            }

            BindEnemyStatusUI();
            if (player != null)
            {
                HandlePlayerHpChanged(player.CurrentHp, player.MaxHp);
                HandlePlayerApChanged(player.CurrentAp, player.ApPerRound);
            }
            RefreshUIExternal();
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.OnHpChangedEvent += HandlePlayerHpChanged;
                player.OnApChangedEvent += HandlePlayerApChanged;
                player.OnTurnStateChanged += HandleTurnStateChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged += HandleBusyStateChanged;
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.OnHpChangedEvent -= HandlePlayerHpChanged;
                player.OnApChangedEvent -= HandlePlayerApChanged;
                player.OnTurnStateChanged -= HandleTurnStateChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null)
                return;

            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            enemyStatusUI1?.Refresh();
            enemyStatusUI2?.Refresh();
            RefreshEndTurnButtonState();
        }

        private void BindEnemyStatusUI()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;

            if (enemyStatusUI1 != null)
            {
                if (enemies.Count > 0 && enemies[0] != null)
                    enemyStatusUI1.SetTarget(enemies[0]);
                else
                    enemyStatusUI1.SetTarget(null);
            }

            if (enemyStatusUI2 != null)
            {
                if (enemies.Count > 1 && enemies[1] != null)
                    enemyStatusUI2.SetTarget(enemies[1]);
                else
                    enemyStatusUI2.SetTarget(null);
            }
        }

        private void HandlePlayerHpChanged(int currentHp, int maxHp)
        {
            if (playerHpText != null)
                playerHpText.text = $"{currentHp}/{maxHp}";

            if (playerHpBar != null)
                playerHpBar.SetHp(currentHp, maxHp);
        }

        private void HandlePlayerApChanged(int currentAp, int maxAp)
        {
            if (playerApText != null)
                playerApText.text = $"{currentAp}";
        }

        private void HandleTurnStateChanged(bool canAct)
        {
            RefreshEndTurnButtonState();
        }

        private void HandleBusyStateChanged(bool isBusy)
        {
            RefreshEndTurnButtonState();
        }

        private void RefreshEndTurnButtonState()
        {
            if (endTurnButton == null || player == null)
                return;

            bool canClick = player.CanAct;

            if (battleActionRunner != null)
                canClick = canClick && battleActionRunner.CanAcceptInput;

            endTurnButton.interactable = canClick;
        }

        private bool HasAliveEnemy()
        {
            if (enemyActionSystem == null)
                return false;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return true;
            }

            return false;
        }
    }
}