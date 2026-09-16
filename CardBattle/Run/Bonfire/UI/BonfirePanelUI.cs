using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class BonfirePanelUI : MonoBehaviour
    {
        [SerializeField] private BonfireController bonfireController;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private BonfireUpgradePanelUI upgradePanelUI;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI previewText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button restButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button retrySaveButton;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (upgradePanelUI != null) upgradePanelUI.BindController(bonfireController);
            if (bonfireController != null) bonfireController.OnStateChanged += Refresh;
            if (restButton != null) restButton.onClick.AddListener(HandleRest);
            if (upgradeButton != null) upgradeButton.onClick.AddListener(HandleUpgrade);
            if (leaveButton != null) leaveButton.onClick.AddListener(HandleLeave);
            if (retrySaveButton != null) retrySaveButton.onClick.AddListener(HandleRetry);
            Refresh();
        }

        private void OnDisable()
        {
            if (bonfireController != null) bonfireController.OnStateChanged -= Refresh;
            if (restButton != null) restButton.onClick.RemoveListener(HandleRest);
            if (upgradeButton != null) upgradeButton.onClick.RemoveListener(HandleUpgrade);
            if (leaveButton != null) leaveButton.onClick.RemoveListener(HandleLeave);
            if (retrySaveButton != null) retrySaveButton.onClick.RemoveListener(HandleRetry);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void HandleUpgrade()
        {
            if (bonfireController == null) return;
            if (bonfireController.State == BonfireController.SessionState.UpgradeCardSelection)
                bonfireController.TryBackFromUpgrade();
            else bonfireController.TryBeginUpgrade();
        }

        private void HandleRest() => bonfireController?.TryRest();
        private void HandleLeave() => bonfireController?.TryLeave();
        private void HandleRetry() => bonfireController?.TryRetrySave();

        private void Refresh()
        {
            bool active = bonfireController != null && bonfireController.IsActive;
            bool upgradeVisible = active && upgradePanelUI != null &&
                (bonfireController.HasAppliedUpgrade || bonfireController.State == BonfireController.SessionState.UpgradeCardSelection ||
                 bonfireController.State == BonfireController.SessionState.UpgradeCommitted ||
                 bonfireController.State == BonfireController.SessionState.InvalidUpgrade);
            if (panelRoot != null) panelRoot.SetActive(active && !upgradeVisible);
            if (!active) return;
            if (hpText != null) hpText.text = $"{bonfireController.CurrentHp} / {bonfireController.MaxHp} HP";
            if (previewText != null) previewText.text = bonfireController.HasCommittedChoice
                ? "Choice resolved"
                : $"Rest: +{bonfireController.RestHealAmount} HP\nAfter Rest: {bonfireController.PreviewResultingHp} / {bonfireController.MaxHp}";
            if (restButton != null) restButton.interactable = bonfireController.CanRest;
            if (upgradeButton != null)
            {
                bool selecting = bonfireController.State == BonfireController.SessionState.UpgradeCardSelection;
                upgradeButton.interactable = selecting ? bonfireController.CanBackFromUpgrade : bonfireController.CanBeginUpgrade;
                var label = upgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = selecting ? "Back" : "Upgrade";
            }
            if (leaveButton != null)
            {
                leaveButton.gameObject.SetActive(bonfireController.CanLeave);
                leaveButton.interactable = bonfireController.CanLeave;
            }
            if (retrySaveButton != null)
            {
                retrySaveButton.gameObject.SetActive(bonfireController.CanRetrySave);
                retrySaveButton.interactable = bonfireController.CanRetrySave;
            }
            if (statusText != null) statusText.text = !string.IsNullOrEmpty(bonfireController.LastError)
                ? bonfireController.LastError
                : bonfireController.IsBusy ? "Saving..."
                : bonfireController.State == BonfireController.SessionState.UpgradeCommitted ? "Upgrade committed — waiting for Bonus selection"
                : bonfireController.State == BonfireController.SessionState.UpgradeCardSelection ? "Choose a card to upgrade, or go Back."
                : bonfireController.CurrentHp == bonfireController.MaxHp ? "HP Full" : "Choose one action";
        }
    }
}
