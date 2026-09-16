#if UNITY_EDITOR
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core.Editor
{
    public static class BonfireBonusUISetup
    {
        [MenuItem("Card Battle/B2.6/Add Bonus Controls to Upgrade Prefab")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var path = BonfireUpgradeUISetup.PrefabPath;
            var host = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var ui = host.GetComponent<BonfireUpgradePanelUI>();
                var field = typeof(BonfireUpgradePanelUI).GetField("bonusRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field.GetValue(ui) != null) { Debug.Log("[B2.6] Bonus controls already assigned; preserving prefab."); return; }
                var root = host.transform.Find("UpgradePanel");
                var bonusRoot = Rect(root, "BonusChoices", 0, -355, 920, 260);
                Text(bonusRoot, "Heading", "Choose Bonus", 0, 0, 900, 36, 24);
                var views = new BonusUpgradeChoiceView[3];
                for (int i = 0; i < 3; i++)
                {
                    var card = Rect(bonusRoot, "Bonus" + (i + 1), (i - 1) * 300, -48, 280, 195);
                    card.gameObject.AddComponent<Image>().color = new Color(.19f, .17f, .20f);
                    var button = card.gameObject.AddComponent<Button>();
                    var selected = Rect(card, "Selected", 0, 0, 280, 195);
                    selected.gameObject.AddComponent<Image>().color = new Color(.86f, .65f, .22f, .35f);
                    var label = Text(card, "Description", "", 0, -14, 250, 170, 18);
                    views[i] = card.gameObject.AddComponent<BonusUpgradeChoiceView>();
                    Set(views[i], "button", button); Set(views[i], "label", label); Set(views[i], "selectedRoot", selected.gameObject);
                    selected.gameObject.SetActive(false);
                }
                var confirm = Rect(root, "ConfirmBonus", 270, -682, 220, 46);
                confirm.gameObject.AddComponent<Image>().color = new Color(.35f, .28f, .18f);
                var apply = confirm.gameObject.AddComponent<Button>();
                Text(confirm, "Label", "Confirm Bonus", 0, -5, 210, 35, 20);
                Set(ui, "bonusRoot", bonusRoot.gameObject); Set(ui, "bonusChoices", views); Set(ui, "applyBonusButton", apply);
                bonusRoot.gameObject.SetActive(false); confirm.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(host, path);
                Debug.Log("[B2.6] Added Bonus controls to existing prefab, preserving its GUID. No scene saved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(host); }
        }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        private static TextMeshProUGUI Text(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Top; text.color = new Color(.93f, .89f, .8f); text.raycastTarget = false; return text;
        }
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    }
}
#endif
