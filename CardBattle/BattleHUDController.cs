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
        [SerializeField] private TextMeshProUGUI enemy1Text;
        [SerializeField] private TextMeshProUGUI enemy2Text;
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
            RefreshUI();
        }

        private void Update()
        {
            RefreshUI();
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null)
                return;

            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            RefreshUI();
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

        private void RefreshUI()
        {
            if (playerApText != null && player != null)
            {
                //playerApText.text = $"AP: {player.CurrentAp}/{player.ApPerRound}";
                playerApText.text = $"{player.CurrentAp}";
            }

            if (playerHpText != null && player != null)
            {
                playerHpText.text = $"{player.CurrentHp}/{player.MaxHp}";
            }

            if (playerHpBar != null && player != null)
            {
                playerHpBar.SetHp(player.CurrentHp, player.MaxHp);
            }

            var enemies = enemyActionSystem != null ? enemyActionSystem.Enemies : null;
            enemyStatusUI1?.Refresh();
            enemyStatusUI2?.Refresh();

            if (enemy1Text != null)
            {
                enemy1Text.text = BuildEnemyText(enemies, 0);
            }

            if (enemy2Text != null)
            {
                enemy2Text.text = BuildEnemyText(enemies, 1);
            }

            if (endTurnButton != null && player != null)
            {
                bool canClick = player.CanAct;

                if (battleActionRunner != null)
                    canClick = canClick && battleActionRunner.CanAcceptInput;

                endTurnButton.interactable = canClick;
            }
        }

        private string BuildEnemyText(System.Collections.Generic.IReadOnlyList<EnemyBattleUnit> enemies, int index)
        {
            if (enemies == null || index < 0 || index >= enemies.Count || enemies[index] == null)
                return $"Enemy {index + 1}: None";

            var enemy = enemies[index];
            return
                $"{enemy.name}\n" +
                $"HP: {enemy.CurrentHp}/{enemy.MaxHp}\n" +
                $"Type: {enemy.Behavior}\n" +
                $"CD: {enemy.CurrentCountdown}\n" +
                $"SPD: {enemy.Speed}";
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