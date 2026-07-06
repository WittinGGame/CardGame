using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleStatusIconPanelUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private BattleUnit target;

        [Header("Database")]
        [SerializeField] private StatusIconDatabase iconDatabase;

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private StatusIconSlotUI slotPrefab;

        [Header("Options")]
        [SerializeField] private bool hideWhenEmpty = true;
        [SerializeField] private bool refreshOnStart = true;
        [SerializeField] private bool verboseLogs;

        private StatusController subscribedStatusController;
        private readonly List<StatusDisplayData> displayBuffer = new();
        private readonly List<StatusIconSlotUI> spawnedSlots = new();

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
            ClearSlots();
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
                ClearSlots();
                SetRootVisible(false);
                return;
            }

            int count = target.StatusController.BuildStatusDisplayData(displayBuffer);
            if (count <= 0)
            {
                ClearSlots();
                SetRootVisible(false);
                return;
            }

            if (slotPrefab == null || slotContainer == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[BattleStatusIconPanelUI] Missing slotPrefab or slotContainer.", this);

                ClearSlots();
                SetRootVisible(false);
                return;
            }

            SetRootVisible(true);
            RebuildSlots();
        }

        private void RebuildSlots()
        {
            ClearSlots();

            Sprite buffArrow = iconDatabase != null ? iconDatabase.BuffArrowIcon : null;
            Sprite debuffArrow = iconDatabase != null ? iconDatabase.DebuffArrowIcon : null;

            for (int i = 0; i < displayBuffer.Count; i++)
            {
                StatusDisplayData data = displayBuffer[i];
                StatusIconDatabase.StatusIconEntry entry = null;

                if (iconDatabase != null)
                    iconDatabase.TryGet(data.Type, out entry);

                StatusIconSlotUI slot = Instantiate(slotPrefab, slotContainer);
                slot.Bind(data, entry, buffArrow, debuffArrow);
                spawnedSlots.Add(slot);
            }

            if (verboseLogs)
                Debug.Log($"[BattleStatusIconPanelUI] Built {spawnedSlots.Count} status slots for {target.name}.", this);
        }

        private void ClearSlots()
        {
            for (int i = spawnedSlots.Count - 1; i >= 0; i--)
            {
                StatusIconSlotUI slot = spawnedSlots[i];
                if (slot == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(slot.gameObject);
                else
                    DestroyImmediate(slot.gameObject);
            }

            spawnedSlots.Clear();
        }

        private void SetRootVisible(bool visible)
        {
            if (root == null)
                return;

            if (!visible && hideWhenEmpty)
                root.SetActive(false);
            else if (visible)
                root.SetActive(true);
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
    }
}
