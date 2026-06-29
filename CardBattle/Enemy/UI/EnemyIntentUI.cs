using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyIntentUI : MonoBehaviour
    {
        [SerializeField] private EnemyBattleUnit target;

        [Header("UI")]
        [SerializeField] private GameObject intentRoot;
        [SerializeField] private GameObject countdownRoot;
        [SerializeField] private TextMeshProUGUI attackValueText;
        [SerializeField] private TextMeshProUGUI countdownValueText;

        private EnemyBattleUnit subscribedTarget;

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
            if (target == enemy)
            {
                Refresh();
                return;
            }

            UnsubscribeTarget();
            target = enemy;
            SubscribeTarget();
            Refresh();
        }

        public void Refresh()
        {
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                !target.IsAlive)
            {
                if (intentRoot != null)
                    intentRoot.SetActive(false);

                return;
            }

            if (intentRoot != null)
                intentRoot.SetActive(true);

            int damage = ResolveIntentValue(target);

            if (attackValueText != null)
                attackValueText.text = damage.ToString();

            bool showCountdown = target.Behavior == EnemyBehaviorType.CountdownAttacker;

            if (countdownRoot != null)
                countdownRoot.SetActive(showCountdown);

            if (countdownValueText != null)
            {
                countdownValueText.gameObject.SetActive(showCountdown);

                if (showCountdown)
                    countdownValueText.text = target.CurrentCountdown.ToString();
            }
        }

        private void SubscribeTarget()
        {
            if (target == null || subscribedTarget == target)
                return;

            UnsubscribeTarget();
            subscribedTarget = target;
            subscribedTarget.OnPlannedActionChanged += HandlePlannedActionChanged;
        }

        private void UnsubscribeTarget()
        {
            if (subscribedTarget == null)
                return;

            subscribedTarget.OnPlannedActionChanged -= HandlePlannedActionChanged;
            subscribedTarget = null;
        }

        private void HandlePlannedActionChanged(EnemyBattleUnit enemy)
        {
            if (enemy != target)
                return;

            Refresh();
        }

        private static int ResolveIntentValue(EnemyBattleUnit enemy)
        {
            if (enemy == null)
                return 0;

            EnemyActionData action = enemy.CurrentPlannedAction;
            if (action == null && enemy.Data != null)
                action = enemy.Data.DefaultAction;

            if (action != null)
            {
                if (action.IntentValue > 0)
                    return action.IntentValue;

                if (action.DealsAttackDamage)
                    return action.Damage;

                return 0;
            }

            return enemy.Data != null ? enemy.Data.AttackDamage : 0;
        }
    }
}
