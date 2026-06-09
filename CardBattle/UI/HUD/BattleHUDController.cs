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

        [Header("Audio")]
        [SerializeField] private UISFXController uiSfx;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerApText;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private GameObject playerBlockRoot;
        [SerializeField] private TextMeshProUGUI playerBlockText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private HpBarUI playerHpBar;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;
        [SerializeField] private GameObject buffRoot;
        [SerializeField] private TextMeshProUGUI buffText;

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
                HandlePlayerBlockChanged(player.CurrentBlock);
                UpdateBuffUI(player.DebugBuffCount);
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
                player.OnBlockChangedEvent += HandlePlayerBlockChanged;
                player.OnDebugBuffChanged += UpdateBuffUI;
                UpdateBuffUI(player.DebugBuffCount);
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
                player.OnBlockChangedEvent -= HandlePlayerBlockChanged;
                player.OnDebugBuffChanged -= UpdateBuffUI;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null || player == null)
                return;

            if (!player.CanAct || !player.IsAlive || !battleActionRunner.CanAcceptInput)
                return;

            if (targetSelectionSystem != null && targetSelectionSystem.IsSelectingTarget)
                targetSelectionSystem.CancelTargetSelection();

            uiSfx?.PlayEndTurn();
            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            RefreshEndTurnButtonState();
        }

        private void BindEnemyStatusUI()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;
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

        private void HandlePlayerBlockChanged(int currentBlock)
        {
            if (playerBlockText != null)
                playerBlockText.text = currentBlock.ToString();

            if (playerBlockRoot != null)
                playerBlockRoot.SetActive(currentBlock > 0);
        }

        private void UpdateBuffUI(int value)
        {
            if (buffRoot == null || buffText == null)
                return;

            if (value > 0)
            {
                buffRoot.SetActive(true);
                buffText.text = value.ToString();
            }
            else
            {
                buffRoot.SetActive(false);
            }
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