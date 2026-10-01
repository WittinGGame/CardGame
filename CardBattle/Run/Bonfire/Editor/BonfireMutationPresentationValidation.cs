#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardBattle.Core.Editor
{
    public static class BonfireMutationPresentationValidation
    {
        [MenuItem("Card Battle/B2.R5B2/Validate Mutation Choice Presentation")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int count = 0;
            Action<bool, string> check = (ok, why) => { count++; if (!ok) throw new Exception(why); };
            var scene = EditorSceneManager.NewPreviewScene();
            var assets = new List<UnityEngine.Object>();
            string path = Path.Combine(Path.GetTempPath(), "r5b2-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var cards = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset");
                var upgrades = AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>("Assets/ScriptsData/CardUpgrades/CardUpgradeCatalog.asset");
                var run = Add<RunManager>(scene);
                run.StartNewRun("r5b2", 1, "knight", 100, new[] { new RunCardRecord("strike") });
                var rest = Node("rest", MapNodeType.Rest, "next");
                var act = ScriptableObject.CreateInstance<MapActData>(); assets.Add(act);
                Set(act, "actId", "r5b2"); Set(act, "startNodeId", "start");
                Set(act, "nodes", new List<MapNodeData> { Node("start", MapNodeType.Start, "rest"), rest, Node("next", MapNodeType.Rest) });
                var map = Add<MapRuntimeController>(scene); Set(map, "actData", act); map.InitializeMap(); map.TrySelectNode("rest");
                var save = Add<ActiveRunSaveService>(scene); Set(save, "saveFileName", path);
                var auto = Add<ActiveRunAutoSaveController>(scene); Set(auto, "runManager", run); Set(auto, "mapRuntimeController", map); Set(auto, "saveService", save);
                var mapUI = Add<TreeMapUIController>(scene); Set(mapUI, "mapRuntimeController", map); mapUI.Hide();
                var bonfire = Add<BonfireController>(scene); Set(bonfire, "runManager", run); Set(bonfire, "mapRuntimeController", map);
                Set(bonfire, "activeRunAutoSaveController", auto); Set(bonfire, "treeMapUIController", mapUI);
                Set(bonfire, "cardCatalog", cards); Set(bonfire, "cardUpgradeCatalog", upgrades);
                Set(bonfire, "baseMutationChance", 1f); Set(bonfire, "upgradeRandom", new FixedRandom());
                check(bonfire.TryOpenSession(rest), "Open Rest session");

                var canvas = Add<Canvas>(scene); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonfireUpgradeUISetup.PrefabPath);
                check(prefab != null, "Upgrade prefab exists");
                var host = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); host.transform.SetParent(canvas.transform, false);
                var ui = host.GetComponent<BonfireUpgradePanelUI>();
                var presentation = host.GetComponent<BonfireUpgradePresentation>();
                check(presentation != null && presentation.IsConfigured, "R5B2 presentation references configured");
                var curseVFXFront = Get<GameObject>(presentation, "curseVFXFront");
                check(curseVFXFront != null && !curseVFXFront.activeSelf,
                    "CurseVFXFront is wired and inactive before presentation");
                ui.BindController(bonfire); Call(ui, "OnEnable");
                bonfire.TryBeginUpgrade(); ui.Refresh();
                var content = Get<Transform>(ui, "content");
                var row = content.GetComponentInChildren<UpgradeCardChoiceView>(true); Call(row, "Awake");
                Get<Button>(row, "button").onClick.Invoke(); ui.Refresh();
                check(ui.State == BonfireUpgradePanelUI.UpgradeUIState.PreviewSelection, "Card click opens preview");
                var confirm = Get<Button>(ui, "confirmButton");
                confirm.onClick.Invoke(); ui.Refresh();

                var pending = run.GetPendingCardUpgradeSnapshot();
                check(pending != null && pending.mutationTriggered && pending.offeredBonusUpgradeIds.Count == 3,
                    "Triggered transaction supplies three persisted offers");
                string frozenOffers = string.Join("|", pending.offeredBonusUpgradeIds);
                check(presentation.IsActive && presentation.Phase == BonfireUpgradePresentation.PresentationPhase.Intro,
                    "Mutation branch starts with R5A intro");
                check(!confirm.interactable, "No Confirm is available during Mutation presentation");

                Step(presentation, Get<float>(presentation, "fadeDuration") + Get<float>(presentation, "moveDuration") + .001f);
                check(presentation.IsHolding && presentation.Phase == BonfireUpgradePresentation.PresentationPhase.CenterHold,
                    "Guaranteed card reaches readable center hold");
                check(!curseVFXFront.activeSelf, "Mutation VFX stays off until the centered hold begins");
                var motion = Get<RectTransform>(presentation, "motionRoot");
                var upgradeCenter = Get<RectTransform>(presentation, "centerTarget");
                var choiceStart = Get<RectTransform>(presentation, "mutationChoiceStartAnchor");
                var left = Get<RectTransform>(presentation, "leftChoiceAnchor");
                var middle = Get<RectTransform>(presentation, "centerChoiceAnchor");
                var right = Get<RectTransform>(presentation, "rightChoiceAnchor");
                Vector3 guaranteedCenter = motion.position;
                Vector3[] destinations = { left.position, middle.position, right.position };
                choiceStart.position += new Vector3(37f, -19f, 0f);
                float hold = Get<float>(presentation, "holdDelay");
                Set(presentation, "mutationVFXDelay", .23f);
                Set(presentation, "mutationVFXDuration", .52f);
                float vfxDelay = Get<float>(presentation, "mutationVFXDelay");
                float vfxDuration = Get<float>(presentation, "mutationVFXDuration");
                Step(presentation, hold + .001f);
                check(presentation.Phase == BonfireUpgradePresentation.PresentationPhase.ChoiceExpand && presentation.ActiveChoiceCount == 3,
                    "Choice expansion starts from the existing Hold Delay without waiting for Mutation VFX timing");
                var prompt = Get<CanvasGroup>(presentation, "mutationChoicePrompt");
                check(prompt != null && prompt.gameObject.activeSelf && Mathf.Approximately(prompt.alpha, 0f) &&
                    !prompt.interactable && !prompt.blocksRaycasts,
                    "Mutation prompt starts hidden and cannot block card pointer input");
                var promptTitle = prompt.transform.Find("TitleText").GetComponent<TextMeshProUGUI>();
                var promptHint = prompt.transform.Find("HintText").GetComponent<TextMeshProUGUI>();
                check(promptTitle.text == "CHOOSE ONE MUTATION" && promptHint.text == "Select one card to continue",
                    "Mutation prompt keeps designer-authored default copy in the prefab");
                check(!curseVFXFront.activeSelf,
                    "Mutation VFX remains off during its independent designer-controlled delay");
                var views = Get<UpgradeCardChoiceView[]>(presentation, "mutationChoiceCards");
                var roots = Get<RectTransform[]>(presentation, "mutationChoiceRoots");
                var groups = Get<CanvasGroup[]>(presentation, "mutationChoiceGroups");
                foreach (var view in views) Call(view, "Awake");
                check(roots.Take(3).All(x => Vector3.Distance(x.position, choiceStart.position) < .01f),
                    "All choices begin at the dedicated designer start anchor");
                check(Vector3.Distance(motion.position, guaranteedCenter) < .01f &&
                    Vector3.Distance(motion.position, upgradeCenter.position) < .01f,
                    "Moving the choice start anchor does not move the centered Guaranteed card");
                check(Vector3.Distance(left.position, destinations[0]) < .01f &&
                    Vector3.Distance(middle.position, destinations[1]) < .01f &&
                    Vector3.Distance(right.position, destinations[2]) < .01f,
                    "Moving the choice start anchor does not change destination anchors");
                check(views.Take(3).Select(x => x.OwnedCardId).SequenceEqual(pending.offeredBonusUpgradeIds),
                    "Choice IDs preserve exact persisted order");
                check(views.Take(3).All(x => !string.IsNullOrWhiteSpace(Get<TextMeshProUGUI>(x, "availabilityText").text) &&
                    !string.IsNullOrWhiteSpace(Get<TextMeshProUGUI>(x, "descriptionText").text)),
                    "Choice cards show Mutation names and effective descriptions");
                Step(presentation, Mathf.Max(0f, vfxDelay - hold) + .001f);
                check(curseVFXFront.activeSelf && presentation.Phase == BonfireUpgradePresentation.PresentationPhase.ChoiceExpand,
                    "Mutation VFX can activate while choices are already expanding");
                check(prompt.alpha > 0f && prompt.alpha < 1f,
                    "Mutation prompt fades independently while choices continue expanding");
                float expandRemaining = Mathf.Max(0f,
                    Get<float>(presentation, "choiceExpandDuration") - Get<float>(presentation, "elapsed"));
                Step(presentation, expandRemaining + .001f);
                check(presentation.Phase == BonfireUpgradePresentation.PresentationPhase.ChoiceWait && presentation.IsHolding,
                    "Choice expansion duration is independent from Mutation VFX duration");
                check(curseVFXFront.activeSelf,
                    "Mutation VFX may remain visible after choices finish expanding");
                Step(presentation, vfxDuration + .001f);
                check(!curseVFXFront.activeSelf && presentation.Phase == BonfireUpgradePresentation.PresentationPhase.ChoiceWait,
                    "Mutation VFX turns off on its own timer without changing choice state");
                check(Vector3.Distance(roots[0].position, left.position) < .01f &&
                    Vector3.Distance(roots[1].position, middle.position) < .01f &&
                    Vector3.Distance(roots[2].position, right.position) < .01f, "Choices expand to designer anchors");
                check(groups.Take(3).All(x => x.alpha == 1f && x.blocksRaycasts), "Choices fade/scale in and accept input");
                check(Mathf.Approximately(prompt.alpha, 1f) && !prompt.blocksRaycasts,
                    "Mutation prompt reaches full visibility without blocking choices");
                check(string.Join("|", bonfire.GetOfferedMutationIds()) == frozenOffers, "Presentation never rerolls offers");

                int resolved = 0; bonfire.OnUpgradeResolved += _ => resolved++;
                string selected = views[1].OwnedCardId;
                Get<Button>(views[1], "button").onClick.Invoke();
                check(resolved == 1 && presentation.SelectedMutationId == selected &&
                    presentation.Phase == BonfireUpgradePresentation.PresentationPhase.ChoiceCollapse,
                    "One click calls B1 final selection immediately");
                check(views.Take(3).All(x => !Get<Button>(x, "button").interactable), "All choices disable immediately");
                check(prompt.gameObject.activeSelf && Mathf.Approximately(prompt.alpha, 1f),
                    "Selection starts prompt fade-out without delaying card collapse");
                Get<Button>(views[0], "button").onClick.Invoke();
                check(resolved == 1 && run.CurrentRun.currentDeck[0].selectedBonusUpgradeId == selected,
                    "Second click cannot change or reapply selection");
                check(map.GetNodeState("rest") == MapNodeState.Completed && save.TryLoad(out var finalSave) &&
                    finalSave.runState.currentDeck[0].selectedBonusUpgradeId == selected, "B1 selection owns apply, completion and save");

                Step(presentation, Get<float>(presentation, "unselectedFadeDuration") + .001f);
                check(presentation.Phase == BonfireUpgradePresentation.PresentationPhase.Exit &&
                    !roots[0].gameObject.activeSelf && !roots[2].gameObject.activeSelf && roots[1].gameObject.activeSelf,
                    "Unselected cards fade out and selected card enters common exit");
                check(!prompt.gameObject.activeSelf && Mathf.Approximately(prompt.alpha, 0f),
                    "Mutation prompt finishes fading independently during the existing collapse");
                var background = Get<CanvasGroup>(presentation, "presentationBackground");
                var backgroundDissolve = Get<BonfirePresentationBackgroundDissolve>(presentation, "backgroundDissolve");
                float delay = Get<float>(presentation, "cardExitDelay");
                Vector3 beforeExit = roots[1].position;
                Step(presentation, delay * .5f);
                check((backgroundDissolve != null ? backgroundDissolve.Reveal < 1f : background.alpha < 1f) &&
                    Vector3.Distance(roots[1].position, beforeExit) < .01f && groups[1].alpha == 1f,
                    "Background starts fading while selected card waits for Card Exit Delay");
                Step(presentation, delay * .5f + .05f);
                check(Vector3.Distance(roots[1].position, beforeExit) > .01f && groups[1].alpha < 1f && background.alpha > 0f,
                    "Selected card begins moving and fading after the delay while background fade overlaps");
                float exitTotal = Mathf.Max(Get<float>(presentation, "backgroundFadeDuration"),
                    delay + Mathf.Max(Get<float>(presentation, "exitMoveDuration"), Get<float>(presentation, "exitFadeDuration")));
                Step(presentation, exitTotal + .001f);
                check(!presentation.IsActive && presentation.LastExitReachedAnchor && presentation.LastExitFadedBackground,
                    "Selected card exits through UpgradeExitAnchor while background fades");
                check(mapUI.IsVisible && !Get<GameObject>(ui, "panelRoot").activeSelf,
                    "Existing completed flow reveals Map underneath and closes presentation");
                check(string.Join("|", pending.offeredBonusUpgradeIds) == frozenOffers,
                    "Presentation did not mutate its captured persisted offer snapshot");
                curseVFXFront.SetActive(true);
                presentation.ResetPresentation();
                check(!curseVFXFront.activeSelf && !Get<bool>(presentation, "mutationVFXTimerActive"),
                    "Reset cancels stale Mutation VFX timing and forces the object inactive");

                Call(ui, "OnDisable");
                Debug.Log("[B2.R5B2] PASS " + count + " focused Mutation presentation assertions.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private sealed class FixedRandom : System.Random
        {
            public override double NextDouble() => 0d;
            public override int Next(int minValue, int maxValue) => minValue;
        }
        private static void Step(BonfireUpgradePresentation target, float time) =>
            target.GetType().GetMethod("Advance", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, new object[] { time });
        private static MapNodeData Node(string id, MapNodeType type, params string[] edges)
        { var node = new MapNodeData(); Set(node, "nodeId", id); Set(node, "nodeType", type); Set(node, "connectedNodeIds", new List<string>(edges)); return node; }
        private static T Add<T>(Scene scene) where T : Component
        { var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); return go.AddComponent<T>(); }
        private static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        private static void Call(object obj, string method) => obj.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, null);
    }
}
#endif
