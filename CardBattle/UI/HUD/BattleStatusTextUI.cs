using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleStatusTextUI : MonoBehaviour
    {
        [SerializeField] private BattleUnit target;
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private bool hideWhenEmpty = true;
        [SerializeField] private bool refreshOnStart = true;
        [SerializeField] private bool verboseLogs = false;

        private StatusController subscribedStatusController;

        public BattleUnit Target => target;

        private void Start()
        {
            if (refreshOnStart)
                Refresh();
        }

        private void OnEnable()
        {
            SubscribeStatusController();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeStatusController();
        }

        private void OnDestroy()
        {
            UnsubscribeStatusController();
        }

        public void SetTarget(BattleUnit unit)
        {
            if (target == unit)
            {
                Refresh();
                return;
            }

            UnsubscribeStatusController();
            target = unit;
            SubscribeStatusController();
            Refresh();
        }

        public void ClearTarget()
        {
            SetTarget(null);
        }

        public void Refresh()
        {
            if (target == null ||
                !target.IsAlive ||
                target.StatusController == null)
            {
                ShowEmpty();
                return;
            }

            string displayText = ResolveDisplayText(target.StatusController);
            if (string.IsNullOrEmpty(displayText))
            {
                ShowEmpty();
                return;
            }

            if (statusText != null)
                statusText.text = displayText;

            if (root != null)
                root.SetActive(true);

            if (verboseLogs)
                Debug.Log($"[BattleStatusTextUI] {target.name}: {displayText}", this);
        }

        private void SubscribeStatusController()
        {
            if (target?.StatusController == null || subscribedStatusController == target.StatusController)
                return;

            UnsubscribeStatusController();
            subscribedStatusController = target.StatusController;
            subscribedStatusController.OnStatusesChanged += HandleStatusesChanged;
        }

        private void UnsubscribeStatusController()
        {
            if (subscribedStatusController == null)
                return;

            subscribedStatusController.OnStatusesChanged -= HandleStatusesChanged;
            subscribedStatusController = null;
        }

        private void HandleStatusesChanged()
        {
            Refresh();
        }

        private void ShowEmpty()
        {
            if (statusText != null)
                statusText.text = string.Empty;

            if (root != null && hideWhenEmpty)
                root.SetActive(false);
        }

        private static string ResolveDisplayText(StatusController controller)
        {
            if (controller == null)
                return string.Empty;

            string displayText = controller.BuildStatusDisplayText();
            if (!string.IsNullOrEmpty(displayText))
                return displayText;

            string debugText = controller.BuildDebugText();
            return debugText == "(none)" ? string.Empty : debugText;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Refresh Status UI")]
        private void DebugRefreshStatusUI()
        {
            Refresh();
        }
#endif
    }
}
