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

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerApText;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private TextMeshProUGUI enemy1Text;
        [SerializeField] private TextMeshProUGUI enemy2Text;
        [SerializeField] private Button endTurnButton;

        private void Start()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnClickEndTurn);
            }

            RefreshUI();
        }

        private void Update()
        {
            RefreshUI();
        }

        private void OnClickEndTurn()
        {
            if (player == null || !player.IsAlive)
                return;

            player.RequestEndTurn();

            if (enemyActionSystem != null && player.IsAlive && HasAliveEnemy())
            {
                enemyActionSystem.StartPlayerRound();
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (playerApText != null && player != null)
            {
                playerApText.text = $"AP: {player.CurrentAp}/{player.ApPerRound}";
            }

            if (playerHpText != null && player != null)
            {
                playerHpText.text = $"Player HP: {player.CurrentHp}/{player.MaxHp}";
            }

            var enemies = enemyActionSystem != null ? enemyActionSystem.Enemies : null;

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
                endTurnButton.interactable = player.CanAct;
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