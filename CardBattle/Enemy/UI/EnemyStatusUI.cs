using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class EnemyStatusUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private EnemyBattleUnit targetEnemy;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private HpBarUI hpBarUI;

        [Header("Block UI")]
        [SerializeField] private GameObject blockRoot;
        [SerializeField] private TextMeshProUGUI blockValueText;
        [SerializeField] private Image blockIconImage;

        private EnemyBattleUnit subscribedTarget;

        public EnemyBattleUnit TargetEnemy => targetEnemy;

        private void OnEnable()
        {
            SubscribeTarget();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeTarget();
        }

        private void OnDestroy()
        {
            UnsubscribeTarget();
        }

        public void SetTarget(EnemyBattleUnit enemy)
        {
            if (targetEnemy == enemy)
            {
                Refresh();
                return;
            }

            UnsubscribeTarget();
            targetEnemy = enemy;
            SubscribeTarget();
            Refresh();
        }

        public void Refresh()
        {
            if (targetEnemy == null ||
                !targetEnemy.gameObject.activeInHierarchy)
            {
                SetEmptyState();
                gameObject.SetActive(false);
                return;
            }

            if (!targetEnemy.IsAlive)
            {
                RefreshBlock();
                gameObject.SetActive(false);
                return;
            }

            if (enemyNameText != null)
            {
                if (targetEnemy.Data != null)
                    enemyNameText.text = targetEnemy.Data.DisplayName;
                else
                    enemyNameText.text = targetEnemy.name;
            }

            if (hpText != null)
                hpText.text = $"{targetEnemy.CurrentHp}/{targetEnemy.MaxHp}";

            if (hpBarUI != null)
                hpBarUI.SetHp(targetEnemy.CurrentHp, targetEnemy.MaxHp);

            if (countdownText != null)
            {
                if (!targetEnemy.IsAlive)
                {
                    countdownText.text = "-";
                }
                else if (targetEnemy.Behavior == EnemyBehaviorType.CountdownAttacker)
                {
                    countdownText.text = $"{targetEnemy.CurrentCountdown}";
                }
                else
                {
                    countdownText.text = "-";
                }
            }

            RefreshBlock();
        }

        private void RefreshBlock()
        {
            if (blockRoot == null)
                return;

            int block = targetEnemy != null ? targetEnemy.CurrentBlock : 0;
            bool showBlock = targetEnemy != null && targetEnemy.IsAlive && block > 0;

            blockRoot.SetActive(showBlock);

            if (!showBlock)
            {
                if (blockValueText != null)
                    blockValueText.text = string.Empty;

                if (blockIconImage != null)
                    blockIconImage.enabled = false;

                return;
            }

            if (blockValueText != null)
                blockValueText.text = block.ToString();

            if (blockIconImage != null)
                blockIconImage.enabled = blockIconImage.sprite != null;
        }

        private void SetEmptyState()
        {
            if (enemyNameText != null)
                enemyNameText.text = "None";

            if (hpText != null)
                hpText.text = "-";

            if (countdownText != null)
                countdownText.text = "-";

            if (hpBarUI != null)
                hpBarUI.SetHp(0, 1);

            RefreshBlock();
        }

        private void SubscribeTarget()
        {
            if (targetEnemy == null || subscribedTarget == targetEnemy)
                return;

            UnsubscribeTarget();
            subscribedTarget = targetEnemy;
            subscribedTarget.OnBlockChangedEvent += HandleBlockChanged;
        }

        private void UnsubscribeTarget()
        {
            if (subscribedTarget == null)
                return;

            subscribedTarget.OnBlockChangedEvent -= HandleBlockChanged;
            subscribedTarget = null;
        }

        private void HandleBlockChanged(int currentBlock)
        {
            Refresh();
        }
    }
}
