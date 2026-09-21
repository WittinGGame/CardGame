using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class BonusUpgradeChoiceView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private GameObject selectedRoot;
        public string BonusId { get; private set; }
        public event Action<string> OnSelected;
        private bool selectable;
        private void Awake() { if (button != null) button.onClick.AddListener(HandleClick); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(HandleClick); }
        private void HandleClick() { if (selectable) OnSelected?.Invoke(BonusId); }
        public void Bind(BonusUpgradeDefinition bonus, bool selected, bool canSelect)
        {
            BonusId = bonus.BonusId;
            selectable = canSelect;
            if (button != null) button.interactable = canSelect;
            if (selectedRoot != null) selectedRoot.SetActive(selected);
            if (label != null)
            {
                var lines = new List<string> { bonus.DisplayName };
                if (!string.IsNullOrWhiteSpace(bonus.Description)) lines.Add(bonus.Description);
                if (bonus.OverrideApCost) lines.Add($"AP: {bonus.ApCostOverride}");
                if (bonus.Effects != null) foreach (var effect in bonus.Effects)
                    if (effect != null && !string.IsNullOrWhiteSpace(effect.GetDescriptionText())) lines.Add(effect.GetDescriptionText());
                label.text = string.Join("\n", lines);
            }
        }
    }
}
