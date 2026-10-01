#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core.Editor
{
    public static class BonfireMutationPresentationSetup
    {
        [MenuItem("Card Battle/B2.R5B2/Setup Mutation Choice Presentation")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var host = PrefabUtility.LoadPrefabContents(BonfireUpgradeUISetup.PrefabPath);
            try
            {
                var ui = host.GetComponent<BonfireUpgradePanelUI>();
                var presentation = host.GetComponent<BonfireUpgradePresentation>();
                if (ui == null || presentation == null) throw new InvalidOperationException("R5A Upgrade presentation must be configured first.");
                if (Get<RectTransform>(presentation, "upgradeExitAnchor") != null)
                {
                    var existingPreview = Get<GameObject>(ui, "previewRoot").GetComponent<RectTransform>();
                    EnsureMutationPrompt(existingPreview, presentation);
                    var existingCenter = Get<RectTransform>(presentation, "centerTarget");
                    var choiceStart = Get<RectTransform>(presentation, "mutationChoiceStartAnchor");
                    if (choiceStart == null)
                    {
                        choiceStart = Anchor(existingPreview, "MutationChoiceStartAnchor", Vector2.zero);
                        choiceStart.position = existingCenter.position;
                        Set(presentation, "mutationChoiceStartAnchor", choiceStart);
                        Set(presentation, "backgroundFadeDuration", .45f);
                        Set(presentation, "cardExitDelay", .15f);
                    }
                    var existingBackground = Get<CanvasGroup>(presentation, "presentationBackground");
                    var existingImage = existingBackground != null ? existingBackground.GetComponent<Image>() : null;
                    if (existingImage != null) existingImage.raycastTarget = true;
                    PrefabUtility.SaveAsPrefabAsset(host, BonfireUpgradeUISetup.PrefabPath);
                    Debug.Log("[B2.R5B2] Already wired; preserving designer anchors and timings.");
                    return;
                }

                var panel = Get<GameObject>(ui, "panelRoot").GetComponent<RectTransform>();
                var preview = Get<GameObject>(ui, "previewRoot").GetComponent<RectTransform>();
                var motion = Get<RectTransform>(presentation, "motionRoot");
                var center = Get<RectTransform>(presentation, "centerTarget");
                var guaranteed = motion.GetComponentInChildren<UpgradeCardChoiceView>(true);
                if (guaranteed == null) throw new InvalidOperationException("Guaranteed preview card is missing.");

                var background = Rect(panel, "PresentationBackground");
                Stretch(background); background.SetAsFirstSibling();
                var sourceImage = panel.GetComponent<Image>();
                var backgroundImage = background.gameObject.AddComponent<Image>();
                if (sourceImage != null)
                {
                    backgroundImage.sprite = sourceImage.sprite;
                    backgroundImage.color = sourceImage.color;
                    backgroundImage.material = sourceImage.material;
                    backgroundImage.type = sourceImage.type;
                    backgroundImage.preserveAspect = sourceImage.preserveAspect;
                    sourceImage.enabled = false;
                }
                // Preserve modal input blocking until the exit transition is complete.
                backgroundImage.raycastTarget = true;
                var backgroundGroup = background.gameObject.AddComponent<CanvasGroup>();

                var choiceStartAnchor = Anchor(preview, "MutationChoiceStartAnchor", Vector2.zero);
                choiceStartAnchor.position = center.position;
                var left = Anchor(preview, "LeftChoiceAnchor", new Vector2(-360f, 0f));
                var middle = Anchor(preview, "CenterChoiceAnchor", Vector2.zero);
                var right = Anchor(preview, "RightChoiceAnchor", new Vector2(360f, 0f));
                var exit = Anchor(preview, "UpgradeExitAnchor", new Vector2(0f, 760f));

                var roots = new List<RectTransform>();
                var groups = new List<CanvasGroup>();
                var cards = new List<UpgradeCardChoiceView>();
                for (int i = 0; i < 3; i++)
                {
                    var root = Rect(preview, "MutationChoice" + (i + 1));
                    root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
                    root.anchoredPosition = Vector2.zero;
                    root.sizeDelta = guaranteed.GetComponent<RectTransform>().rect.size;
                    var group = root.gameObject.AddComponent<CanvasGroup>();
                    var clone = UnityEngine.Object.Instantiate(guaranteed.gameObject, root, false);
                    clone.name = "Card";
                    var cloneRect = clone.GetComponent<RectTransform>();
                    cloneRect.anchorMin = cloneRect.anchorMax = cloneRect.pivot = new Vector2(.5f, .5f);
                    cloneRect.anchoredPosition = Vector2.zero;
                    cloneRect.localScale = Vector3.one;
                    root.gameObject.SetActive(false);
                    roots.Add(root); groups.Add(group); cards.Add(clone.GetComponent<UpgradeCardChoiceView>());
                }

                Set(presentation, "presentationBackground", backgroundGroup);
                Set(presentation, "motionCanvasGroup", guaranteed.GetComponent<CanvasGroup>());
                Set(presentation, "mutationChoiceStartAnchor", choiceStartAnchor);
                Set(presentation, "leftChoiceAnchor", left);
                Set(presentation, "centerChoiceAnchor", middle);
                Set(presentation, "rightChoiceAnchor", right);
                Set(presentation, "upgradeExitAnchor", exit);
                Set(presentation, "mutationChoiceRoots", roots.ToArray());
                Set(presentation, "mutationChoiceGroups", groups.ToArray());
                Set(presentation, "mutationChoiceCards", cards.ToArray());
                EnsureMutationPrompt(preview, presentation);
                PrefabUtility.SaveAsPrefabAsset(host, BonfireUpgradeUISetup.PrefabPath);
                Debug.Log("[B2.R5B2] Mutation choice anchors, cards and exit presentation wired. Open scenes unchanged.");
            }
            finally { PrefabUtility.UnloadPrefabContents(host); }
        }

        private static RectTransform Anchor(Transform parent, string name, Vector2 position)
        {
            var rect = Rect(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static void EnsureMutationPrompt(RectTransform preview, BonfireUpgradePresentation presentation)
        {
            var prompt = preview.Find("MutationChoicePrompt") as RectTransform;
            bool created = prompt == null;
            if (created)
            {
                prompt = Rect(preview, "MutationChoicePrompt");
                prompt.anchorMin = prompt.anchorMax = prompt.pivot = new Vector2(.5f, .5f);
                prompt.anchoredPosition = new Vector2(0f, 285f);
                prompt.sizeDelta = new Vector2(760f, 110f);
                var title = Text(prompt, "TitleText", "CHOOSE ONE MUTATION", new Vector2(0f, 20f), new Vector2(740f, 46f), 30f);
                var hint = Text(prompt, "HintText", "Select one card to continue", new Vector2(0f, -25f), new Vector2(740f, 34f), 19f);
                title.fontStyle = FontStyles.Bold;
                title.raycastTarget = false;
                hint.raycastTarget = false;
                var graphic = Rect(prompt, "PromptGraphic");
                graphic.anchorMin = new Vector2(.5f, 0f);
                graphic.anchorMax = new Vector2(.5f, 0f);
                graphic.pivot = new Vector2(.5f, 0f);
                graphic.anchoredPosition = Vector2.zero;
                graphic.sizeDelta = new Vector2(320f, 4f);
            }
            var group = prompt.GetComponent<CanvasGroup>();
            if (group == null) group = prompt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            prompt.gameObject.SetActive(false);
            Set(presentation, "mutationChoicePrompt", group);
            if (created)
            {
                Set(presentation, "mutationPromptFadeInDelay", .05f);
                Set(presentation, "mutationPromptFadeInDuration", .2f);
                Set(presentation, "mutationPromptFadeOutDuration", .15f);
            }
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var rect = Rect(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.93f, .89f, .8f);
            return text;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
#endif
