using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    /// <summary>
    /// View-only UI for hand card selection sessions (manual discard, etc.).
    /// </summary>
    public class HandCardSelectionUI : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject dimOverlay;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI selectionCountText;
        [SerializeField] private TextMeshProUGUI selectedActionLabelText;

        [Header("Confirm")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonLabel;

        [Header("Defaults")]
        [SerializeField] private string defaultInstruction = "Select cards to discard";
        [SerializeField] private string confirmLabel = "Discard";

        public Button ConfirmButton => confirmButton;
        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(string instruction, int selectedCount, int requiredCount)
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (dimOverlay != null)
                dimOverlay.SetActive(true);

            if (instructionText != null)
                instructionText.text = string.IsNullOrEmpty(instruction) ? defaultInstruction : instruction;

            if (selectedActionLabelText != null)
                selectedActionLabelText.text = confirmLabel;

            if (confirmButtonLabel != null)
                confirmButtonLabel.text = confirmLabel;

            RefreshCount(selectedCount, requiredCount);
            SetConfirmInteractable(selectedCount == requiredCount && requiredCount > 0);
        }

        public void RefreshCount(int selectedCount, int requiredCount)
        {
            if (selectionCountText != null)
                selectionCountText.text = $"{selectedCount} / {requiredCount}";

            SetConfirmInteractable(selectedCount == requiredCount && requiredCount > 0);
        }

        public void SetConfirmInteractable(bool value)
        {
            if (confirmButton != null)
                confirmButton.interactable = value;
        }

        public void HideImmediate()
        {
            if (confirmButton != null)
                confirmButton.interactable = false;

            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (dimOverlay != null)
                dimOverlay.SetActive(false);
        }
    }
}
