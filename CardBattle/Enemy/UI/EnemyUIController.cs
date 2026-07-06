using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyUIController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private EnemyBattleUnit target;

        [Header("UI Parts")]
        [SerializeField] private EnemyStatusUI hpUI;
        [SerializeField] private EnemyIntentUI intentUI;

        [Header("Status Icon UI")]
        [SerializeField] private BattleStatusIconPanelUI statusIconPanelUI;

        [Header("Follow Components")]
        [SerializeField] private WorldToUIFollow hpFollow;
        [SerializeField] private WorldToUIFollow intentFollow;
        [SerializeField] private WorldToUIFollow buffFollow;

        [Header("Options")]
        [SerializeField] private bool verboseLogs;

        public EnemyBattleUnit Target => target;
        public WorldToUIFollow HpFollow => hpFollow;
        public WorldToUIFollow IntentFollow => intentFollow;
        public WorldToUIFollow BuffFollow => buffFollow;

        private EnemyBattleUnit subscribedTarget;

        private void Start()
        {
            if (target != null)
                BindAll();

            RefreshAll();
        }

        private void OnEnable()
        {
            SubscribeTargetEvents();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeTargetEvents();
        }

        public void SetTarget(EnemyBattleUnit enemy)
        {
            UnsubscribeTargetEvents();
            target = enemy;
            BindAll();
            SubscribeTargetEvents();
            RefreshAll();
        }

        public void RefreshNow()
        {
            RefreshAll();
        }

        public void RefreshExternal()
        {
            RefreshAll();
        }

        private void BindAll()
        {
            if (target == null)
            {
                HideAll();
                return;
            }

            if (hpUI != null)
                hpUI.SetTarget(target);

            if (intentUI != null)
                intentUI.SetTarget(target);

            if (statusIconPanelUI != null)
                statusIconPanelUI.SetTarget(target);

            BindFollow();
        }

        private void BindFollow()
        {
            if (target == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[EnemyUIController] Target is null.");

                return;
            }

            if (verboseLogs)
                Debug.Log($"[EnemyUIController] Binding follow for {target.name}");

            if (hpFollow != null && target.UIAnchorHP != null)
            {
                hpFollow.SetTarget(target.UIAnchorHP);

                if (verboseLogs)
                    Debug.Log($"HP Follow -> {target.UIAnchorHP.name}");
            }
            else
            {
                Debug.LogWarning(
                    $"HP Follow missing | hpFollow={(hpFollow != null)} | anchor={(target.UIAnchorHP != null)}");
            }

            if (intentFollow != null && target.UIAnchorIntent != null)
            {
                intentFollow.SetTarget(target.UIAnchorIntent);

                if (verboseLogs)
                    Debug.Log($"Intent Follow -> {target.UIAnchorIntent.name}");
            }
            else
            {
                Debug.LogWarning(
                    $"Intent Follow missing | intentFollow={(intentFollow != null)} | anchor={(target.UIAnchorIntent != null)}");
            }

            if (buffFollow != null && target.UIAnchorBuff != null)
            {
                buffFollow.SetTarget(target.UIAnchorBuff);

                if (verboseLogs)
                    Debug.Log($"Buff Follow -> {target.UIAnchorBuff.name}");
            }
            else
            {
                Debug.LogWarning(
                    $"Buff Follow missing | buffFollow={(buffFollow != null)} | anchor={(target.UIAnchorBuff != null)}");
            }
        }

        private void SubscribeTargetEvents()
        {
            if (target == null || subscribedTarget == target)
                return;

            if (subscribedTarget != null)
                UnsubscribeTargetEvents();

            target.OnHpChangedEvent += HandleHpChanged;
            target.OnBlockChangedEvent += HandleBlockChanged;
            target.OnEnemyStateChanged += HandleEnemyStateChanged;

            if (target.StatusController != null)
                target.StatusController.OnStatusesChanged += HandleStatusesChanged;

            subscribedTarget = target;
        }

        private void UnsubscribeTargetEvents()
        {
            if (subscribedTarget == null)
                return;

            subscribedTarget.OnHpChangedEvent -= HandleHpChanged;
            subscribedTarget.OnBlockChangedEvent -= HandleBlockChanged;
            subscribedTarget.OnEnemyStateChanged -= HandleEnemyStateChanged;

            if (subscribedTarget.StatusController != null)
                subscribedTarget.StatusController.OnStatusesChanged -= HandleStatusesChanged;

            subscribedTarget = null;
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            RefreshAll();
        }

        private void HandleBlockChanged(int currentBlock)
        {
            RefreshAll();
        }

        private void HandleEnemyStateChanged()
        {
            RefreshAll();
        }

        private void HandleStatusesChanged()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (!RefreshVisibility())
                return;

            hpUI?.Refresh();
            intentUI?.Refresh();
            statusIconPanelUI?.Refresh();
        }

        private bool RefreshVisibility()
        {
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                !target.IsAlive)
            {
                if (verboseLogs && target != null && !target.gameObject.activeInHierarchy)
                {
                    Debug.Log(
                        $"[EnemyUIController] Hidden because target is inactive: {target.name}");
                }

                HideAll();
                return false;
            }

            ShowAll();
            return true;
        }

        private void HideAll()
        {
            if (hpUI != null)
                hpUI.gameObject.SetActive(false);

            if (intentUI != null)
                intentUI.gameObject.SetActive(false);

            statusIconPanelUI?.Refresh();

            SetFollowEnabled(false);
        }

        private void ShowAll()
        {
            if (hpUI != null && !hpUI.gameObject.activeSelf)
                hpUI.gameObject.SetActive(true);

            if (intentUI != null && !intentUI.gameObject.activeSelf)
                intentUI.gameObject.SetActive(true);

            SetFollowEnabled(true);
        }

        private void SetFollowEnabled(bool enabled)
        {
            SetFollowComponentEnabled(hpFollow, enabled);
            SetFollowComponentEnabled(intentFollow, enabled);
            SetFollowComponentEnabled(buffFollow, enabled);
        }

        private static void SetFollowComponentEnabled(WorldToUIFollow follow, bool enabled)
        {
            if (follow == null)
                return;

            follow.enabled = enabled;

            if (enabled)
            {
                if (!follow.gameObject.activeSelf)
                    follow.gameObject.SetActive(true);
            }
            else if (follow.gameObject.activeSelf)
            {
                follow.gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Refresh UI")]
        private void DebugRefresh()
        {
            if (target == null)
            {
                Debug.LogWarning("EnemyUIController: No target assigned.");
                return;
            }

            hpUI?.Refresh();
            intentUI?.Refresh();
            statusIconPanelUI?.Refresh();

            Debug.Log($"[EnemyUIController] Refreshed UI for {target.name}");
        }
#endif
    }
}
