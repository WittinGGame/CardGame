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
using UnityEngine.EventSystems;

namespace CardBattle.Core.Editor
{
    public static class BonfireUpgradeUIValidation
    {
        [MenuItem("Card Battle/B2.5/Validate Upgrade Selection UI")]
        [MenuItem("Card Battle/B2.R4/Validate Upgrade UX")]
        public static void Validate() { Run(false); }
        [MenuItem("Card Battle/B2.R5A/Validate Upgrade Presentation")]
        public static void ValidatePresentation() { Run(true); }
        private static void Run(bool withPresentation)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
            var scene=EditorSceneManager.NewPreviewScene();var assets=new List<UnityEngine.Object>();
            string path=Path.Combine(Path.GetTempPath(),"b25-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var cards=AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset");
                var upgrades=AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>("Assets/ScriptsData/CardUpgrades/CardUpgradeCatalog.asset");
                var run=Add<RunManager>(scene);var records=new List<RunCardRecord>();for(int i=0;i<12;i++)records.Add(new RunCardRecord("strike"));records.Add(new RunCardRecord("strike",1));records.Add(new RunCardRecord("missing"));
                run.StartNewRun("b25",1,"knight",100,records);run.SetCurrentHp(38);
                var rest=Node("rest",MapNodeType.Rest);var act=ScriptableObject.CreateInstance<MapActData>();assets.Add(act);Set(act,"actId","b25");Set(act,"startNodeId","start");Set(act,"nodes",new List<MapNodeData>{Node("start",MapNodeType.Start,"rest"),rest});
                var map=Add<MapRuntimeController>(scene);Set(map,"actData",act);map.InitializeMap();map.TrySelectNode("rest");
                var save=Add<ActiveRunSaveService>(scene);Set(save,"saveFileName",path);
                var auto=Add<ActiveRunAutoSaveController>(scene);Set(auto,"runManager",run);Set(auto,"mapRuntimeController",map);Set(auto,"saveService",save);
                var bonfire=Add<BonfireController>(scene);Set(bonfire,"runManager",run);Set(bonfire,"mapRuntimeController",map);Set(bonfire,"activeRunAutoSaveController",auto);Set(bonfire,"cardCatalog",cards);Set(bonfire,"cardUpgradeCatalog",upgrades);bonfire.TryOpenSession(rest);
                var random=new CountingRandom();Set(bonfire,"upgradeRandom",random);
                var canvas=Add<Canvas>(scene);canvas.renderMode=RenderMode.ScreenSpaceCamera;var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;canvas.gameObject.AddComponent<GraphicRaycaster>();
                var camera=Add<Camera>(scene);camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;canvas.planeDistance=1;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BonfireUpgradeUISetup.PrefabPath);check(prefab!=null,"UI prefab exists");
                var host=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);host.transform.SetParent(canvas.transform,false);
                var ui=host.GetComponent<BonfireUpgradePanelUI>();
                var presentation=host.GetComponent<BonfireUpgradePresentation>();
                if(withPresentation) check(presentation!=null,"Presentation prefab reference exists");
                if(presentation!=null) presentation.enabled=withPresentation; // R4 regression also verifies the optional-presentation fallback.
                ui.BindController(bonfire);Call(ui,"OnEnable");
                var confirm=Get<Button>(ui,"confirmButton");var back=Get<Button>(ui,"backButton");var retry=Get<Button>(ui,"retryButton");var previewBack=Get<Button>(ui,"previewBackButton");
                bonfire.TryBeginUpgrade();ui.Refresh();
                check(ui.DisplayedCardCount==14,"Full run deck represented");
                check(ui.State==BonfireUpgradePanelUI.UpgradeUIState.DeckSelection && !Get<GameObject>(ui,"previewRoot").activeSelf,"Starts with separate deck screen only");
                var content=Get<Transform>(ui,"content");var rows=content.GetComponentsInChildren<UpgradeCardChoiceView>(true);
                foreach(var row in rows)Call(row,"Awake");
                var grid=content.GetComponent<GridLayoutGroup>();
                check(grid.constraint==GridLayoutGroup.Constraint.FixedColumnCount&&grid.constraintCount==4,"Four fixed columns");
                var scroll=content.GetComponentInParent<ScrollRect>();check(scroll.vertical&&!scroll.horizontal&&scroll.content==content,"Vertical scrolling uses full deck content");
                // Exercise designer-authored dimensions on the test instance only, not the prefab asset.
                grid.cellSize=new Vector2(200,300);grid.spacing=new Vector2(17,29);
                var deckLayout=ui.GetComponentInChildren<BonfireUpgradeDeckUI>(true);
                Call(deckLayout,"OnEnable");deckLayout.RefreshLayout();
                check(grid.cellSize==new Vector2(200,300)&&grid.spacing==new Vector2(17,29),"Opening/refreshing preserves authored cell size and spacing");
                var viewportSize=scroll.viewport.sizeDelta;scroll.viewport.sizeDelta+=new Vector2(120,0);
                Call(deckLayout,"OnRectTransformDimensionsChange");deckLayout.RefreshLayout();
                check(grid.cellSize==new Vector2(200,300)&&grid.spacing==new Vector2(17,29)&&grid.constraintCount==4,"Viewport resize preserves dimensions and four columns");
                scroll.viewport.sizeDelta=viewportSize;
                var visual=Get<RectTransform>(rows[0],"visualRoot");var normal=visual.localScale;
                rows[0].OnPointerEnter(null);check(visual.localScale.x>normal.x,"Eligible hover scales visual");
                rows[0].OnPointerExit(null);check(visual.localScale==normal,"Pointer exit restores original scale");
                rows[12].OnPointerEnter(null);check(Get<CanvasGroup>(rows[12],"canvasGroup").alpha<1f&&!Get<Button>(rows[12],"button").interactable,"Ineligible remains dim and non-interactable");
                Get<Button>(rows[12],"button").onClick.Invoke();check(bonfire.SelectedRunCardInstanceId=="","Ineligible click cannot select");
                check(Get<Image>(rows[0],"artworkImage").sprite!=null&&Get<TextMeshProUGUI>(rows[0],"titleText").text!="","Real artwork/title assigned");
                Render(canvas,camera,ui,"/private/tmp/cardgame-r4-grid.png");
                scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
                Render(canvas,camera,ui,"/private/tmp/cardgame-r4-grid-bottom.png");
                var corners=new Vector3[4];rows[13].GetComponent<RectTransform>().GetWorldCorners(corners);
                float bottom=scroll.viewport.InverseTransformPoint(corners[0]).y,top=scroll.viewport.InverseTransformPoint(corners[1]).y;
                check(bottom>=scroll.viewport.rect.yMin-1 && top<=scroll.viewport.rect.yMax+1,$"Scroll reaches final owned cards: {bottom}..{top} in {scroll.viewport.rect}");
                scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();
                rows[0].OnPointerEnter(null);Call(rows[0],"OnDisable");check(visual.localScale==Get<RectTransform>(rows[1],"visualRoot").localScale,"Disabling hovered row restores scale");
                check(rows[0].OwnedCardId!=rows[1].OwnedCardId,"Duplicate base copies have separate IDs");
                check(rows[0].IsSelectable&&!rows[12].IsSelectable&&!rows[13].IsSelectable,"Eligibility dimming retains invalid/Level1 rows");
                check(!confirm.interactable,"Confirm disabled without selection");
                string before=JsonUtility.ToJson(run.GetSnapshot());string fileBefore=File.ReadAllText(path);
                Get<Button>(rows[0],"button").onClick.Invoke();ui.Refresh();
                check(bonfire.SelectedRunCardInstanceId==rows[0].OwnedCardId,"Click selects ownership ID");
                check(ui.State==BonfireUpgradePanelUI.UpgradeUIState.PreviewSelection&&!Get<GameObject>(ui,"deckRoot").activeSelf&&Get<GameObject>(ui,"previewRoot").activeSelf,"Click opens separate preview");
                var preview=Get<BonfireUpgradePreviewUI>(ui,"previewScreen");
                check(Get<TextMeshProUGUI>(Get<UpgradeCardChoiceView>(preview,"currentCard"),"descriptionText").text.Contains("5 damage")&&Get<TextMeshProUGUI>(Get<UpgradeCardChoiceView>(preview,"guaranteedCard"),"descriptionText").text.Contains("8 damage"),"Real preview cards use authored current and Guaranteed sequences");
                check(Get<TextMeshProUGUI>(preview,"mutationChanceText").text.StartsWith("Mutation Chance:"),"Mutation shown as plain text");
                Render(canvas,camera,ui,"/private/tmp/cardgame-r4-preview.png");
                Get<Button>(rows[1],"button").onClick.Invoke();check(bonfire.SelectedRunCardInstanceId==rows[0].OwnedCardId,"Hidden grid cannot switch preview selection");
                check(Get<TextMeshProUGUI>(ui,"currentText").text.Contains("5 damage")&&Get<TextMeshProUGUI>(ui,"upgradeText").text.Contains("8 damage"),"Authored preview 5 vs 8");
                check(Get<TextMeshProUGUI>(ui,"currentText").text.Contains("1 AP")&&Get<TextMeshProUGUI>(ui,"upgradeText").text.Contains("1 AP"),"Base AP in both columns");
                check(JsonUtility.ToJson(run.GetSnapshot())==before&&File.ReadAllText(path)==fileBefore&&run.CurrentRun.pendingCardUpgrade==null,"Preview has no persistent/save/RNG-offer mutation");
                previewBack.onClick.Invoke();
                check(ui.State==BonfireUpgradePanelUI.UpgradeUIState.DeckSelection&&bonfire.CanBackFromUpgrade,"Preview Back returns to uncommitted deck selection");
                var deckCanvasGroup=Get<CanvasGroup>(ui,"deckSelectionCanvasGroup");
                check(Mathf.Approximately(deckCanvasGroup.alpha,1f)&&deckCanvasGroup.interactable,"Preview Back does not start the top-level Deck exit fade");
                confirm.onClick.Invoke();check(random.Rolls==0&&JsonUtility.ToJson(run.GetSnapshot())==before&&File.ReadAllText(path)==fileBefore,"Preview Back and grid Confirm cannot mutate/save/roll");
                Get<Button>(rows[1],"button").onClick.Invoke();ui.Refresh();
                check(bonfire.SelectedRunCardInstanceId==rows[1].OwnedCardId&&!Get<GameObject>(rows[0],"selectedRoot").activeSelf&&Get<GameObject>(rows[1],"selectedRoot").activeSelf,"Selection highlight switches");
                var firstRow=rows[0];for(int i=0;i<5;i++)ui.Refresh();check(ui.DisplayedCardCount==14&&content.GetChild(0).GetComponent<UpgradeCardChoiceView>()==firstRow,"Refresh reuses items");
                previewBack.onClick.Invoke();
                back.onClick.Invoke();check(bonfire.State==BonfireController.SessionState.Choice&&bonfire.SelectedRunCardInstanceId==""&&!Get<GameObject>(ui,"panelRoot").activeSelf&&Mathf.Approximately(deckCanvasGroup.alpha,0f),"Deck Back fades content, then uses controller and hides Upgrade panel");
                check(bonfire.CanRest,"Rest remains available before commitment");
                bonfire.TryBeginUpgrade();ui.Refresh();rows=content.GetComponentsInChildren<UpgradeCardChoiceView>(true);foreach(var row in rows)Call(row,"Awake");
                check(Mathf.Approximately(deckCanvasGroup.alpha,1f)&&deckCanvasGroup.interactable&&deckCanvasGroup.blocksRaycasts,"Reopening Upgrade restores DeckSelection alpha and input");
                Get<Button>(rows[0],"button").onClick.Invoke();string chosen=bonfire.SelectedRunCardInstanceId;
                bool sawResolving=false;bonfire.OnUpgradeResolved+=_=>{sawResolving=ui.State==BonfireUpgradePanelUI.UpgradeUIState.ResolvingUpgrade;confirm.onClick.Invoke();previewBack.onClick.Invoke();};
                Set(save,"saveFileName","");confirm.onClick.Invoke();ui.Refresh();
                check(sawResolving&&ui.State==(withPresentation?BonfireUpgradePanelUI.UpgradeUIState.ResolvingUpgrade:BonfireUpgradePanelUI.UpgradeUIState.Completed)&&random.Rolls==1,"Resolving guard rejects reentrant Confirm/Back; completion consumes one roll");
                confirm.onClick.Invoke();check(random.Rolls==1,"Repeated Confirm does not roll twice");
                check(bonfire.State==BonfireController.SessionState.CompletedButUnsaved && bonfire.GetCommittedUpgradeCardSnapshot().runCardInstanceId==chosen,"Confirm automatically resolves selected card");
                string frozen=JsonUtility.ToJson(run.GetSnapshot());
                check(run.CurrentRun.currentDeck[0].upgradeLevel==1 && bonfire.GetOfferedBonusIds().Count==0,"Default Upgrade has no pending choice");
                if(!withPresentation) check(Get<UpgradeCardChoiceView>(ui,"lockedCardView").gameObject.activeSelf && !Get<GameObject>(ui,"bonusRoot").activeSelf,"Save failure shows locked result, no Bonus choice");
                if(!withPresentation) check(!back.gameObject.activeSelf&&!confirm.gameObject.activeSelf&&!bonfire.CanRest,"Completed controls unavailable");
                if(withPresentation)
                {
                    var curseVFXFront=Get<GameObject>(presentation,"curseVFXFront");
                    var mutationPrompt=Get<CanvasGroup>(presentation,"mutationChoicePrompt");
                    check(presentation.IsActive&&!presentation.IsHolding,"Confirm starts presentation");
                    check(curseVFXFront!=null&&!curseVFXFront.activeSelf,"No-Mutation starts with CurseVFXFront inactive");
                    check(mutationPrompt!=null&&!mutationPrompt.gameObject.activeSelf&&Mathf.Approximately(mutationPrompt.alpha,0f),"No-Mutation never shows the Mutation choice prompt");
                    check(!confirm.interactable&&!previewBack.interactable&&!Get<GameObject>(ui,"deckRoot").activeSelf,"Animation blocks Confirm, Back and grid");
                    check(!presentation.TryBegin(bonfire.ResolvedUpgrade),"Duplicate resolution cannot start animation again");
                    var groups=Get<CanvasGroup[]>(presentation,"fadeGroups");
                    var guaranteedView=Get<UpgradeCardChoiceView>(Get<BonfireUpgradePreviewUI>(ui,"previewScreen"),"guaranteedCard");
                    var motion=Get<RectTransform>(presentation,"motionRoot");var rotation=motion.localRotation;
                    float fade=Get<float>(presentation,"fadeDuration"),move=Get<float>(presentation,"moveDuration");
                    Step(presentation,fade*.5f);
                    check(Mathf.Abs(groups[0].alpha-.5f)<.001f,"Preview group fades halfway");
                    check(Get<CanvasGroup>(guaranteedView,"canvasGroup").alpha==1f&&!guaranteedView.transform.IsChildOf(groups[0].transform),"Guaranteed card is outside faded group and remains opaque");
                    Step(presentation,fade*.5f+move*.25f);
                    check(groups[0].alpha==0f&&Quaternion.Angle(motion.localRotation,rotation*Quaternion.Euler(0,90,0))<.1f,"Fade finishes, positive Y quarter-turn");
                    Step(presentation,move*.5f);
                    check(Quaternion.Angle(motion.localRotation,rotation*Quaternion.Euler(0,270,0))<.1f,"Flip passes three-quarter-turn rather than reversing");
                    Step(presentation,move*.25f+.001f);
                    check(presentation.IsHolding&&ui.State==BonfireUpgradePanelUI.UpgradeUIState.Completed,"Animation ends held");
                    check(Vector3.Distance(motion.position,Get<RectTransform>(presentation,"centerTarget").position)<.001f&&Quaternion.Angle(motion.localRotation,rotation)<.01f,"Card centers and ends at original readable orientation");
                    check(!curseVFXFront.activeSelf,"No-Mutation never activates CurseVFXFront at center");
                    Step(presentation,Get<float>(presentation,"holdDelay")+.001f);
                    check(presentation.Phase==BonfireUpgradePresentation.PresentationPhase.Exit&&!presentation.IsHolding,"No-Mutation continues into common exit without input");
                    check(!curseVFXFront.activeSelf,"No-Mutation keeps CurseVFXFront inactive through exit");
                    var background=Get<CanvasGroup>(presentation,"presentationBackground");
                    var backgroundDissolve=Get<BonfirePresentationBackgroundDissolve>(presentation,"backgroundDissolve");
                    float delay=Get<float>(presentation,"cardExitDelay");Vector3 exitStart=motion.position;
                    Step(presentation,delay*.5f);
                    check((backgroundDissolve!=null?backgroundDissolve.Reveal<1f:background.alpha<1f)&&Vector3.Distance(motion.position,exitStart)<.01f&&Get<CanvasGroup>(presentation,"motionCanvasGroup").alpha==1f,"No-Mutation background fades before delayed card exit");
                    Step(presentation,delay*.5f+.05f);
                    check(Vector3.Distance(motion.position,exitStart)>.01f&&Get<CanvasGroup>(presentation,"motionCanvasGroup").alpha<1f&&background.alpha>0f,"No-Mutation card exit overlaps the background fade after delay");
                    Step(presentation,ExitTotal(presentation)+.001f);
                    check(!presentation.IsActive&&presentation.LastExitReachedAnchor&&presentation.LastExitFadedBackground,"No-Mutation exits through anchor and fades background");
                    check(!curseVFXFront.activeSelf,"Closing presentation leaves CurseVFXFront inactive");
                    check(JsonUtility.ToJson(run.GetSnapshot())==frozen&&random.Rolls==1,"Animation never mutates result or RNG");
                }
                check(retry.gameObject.activeSelf,"Final save can be retried");
                Set(save,"saveFileName",path);retry.onClick.Invoke();ui.Refresh();
                check(!bonfire.IsActive && JsonUtility.ToJson(run.GetSnapshot())==frozen && random.Rolls==1,"Retry persists exact result without reroll");
                if(withPresentation) { check(!presentation.IsActive&&!Get<GameObject>(ui,"panelRoot").activeSelf,"Successful retry keeps finished presentation closed");Call(ui,"OnDisable");Call(ui,"OnEnable"); }
                check(save.TryLoad(out var loaded),"Load final UI result");bonfire.ResetSession();run.RestoreRun(loaded.runState);map.RestoreMapState(loaded.mapState);ui.Refresh();
                check(!bonfire.TryOpenSession(rest) && !Get<GameObject>(ui,"panelRoot").activeSelf,"Completed Rest does not reopen Upgrade");
                for(int i=0;i<3;i++){Call(ui,"OnDisable");Call(ui,"OnEnable");ui.BindController(bonfire);}
                check(ui.DisplayedCardCount==0,"Repeated lifecycle creates no stale items");
                bonfire.ResetSession();map.InitializeMap();map.TrySelectNode("rest");bonfire.TryOpenSession(rest);bonfire.TryBeginUpgrade();
                int transitions=0;Action changed=()=>transitions++;bonfire.OnStateChanged+=changed;
                back.onClick.Invoke();bonfire.OnStateChanged-=changed;
                check(transitions==1&&bonfire.State==BonfireController.SessionState.Choice,"One Back handler after repeated enable");
                bonfire.TryBeginUpgrade();ui.Refresh();rows=content.GetComponentsInChildren<UpgradeCardChoiceView>(true);foreach(var row in rows)Call(row,"Awake");
                Get<Button>(rows[1],"button").onClick.Invoke();
                check(ui.State==BonfireUpgradePanelUI.UpgradeUIState.PreviewSelection&&bonfire.SelectedRunCardInstanceId==rows[1].OwnedCardId,"Reopened UI selects the exact remaining duplicate");
                if(withPresentation)
                {
                    int rolls=random.Rolls;confirm.onClick.Invoke();
                    check(!bonfire.IsActive&&save.TryLoad(out var finalSave)&&finalSave.runState.currentDeck[1].upgradeLevel==1,"Successful Confirm saves gameplay before animation finishes");
                    check(presentation.IsActive&&!presentation.IsHolding&&random.Rolls==rolls+1,"Gameplay does not wait for animation");
                    CompleteNoMutationPresentation(presentation);
                    check(!presentation.IsActive&&!Get<GameObject>(ui,"panelRoot").activeSelf&&presentation.LastExitReachedAnchor,"Successful save exits and closes presentation");
                    bonfire.ResetSession();map.InitializeMap();map.TrySelectNode("rest");bonfire.TryOpenSession(rest);bonfire.TryBeginUpgrade();
                }
                Call(ui,"OnDisable");Call(ui,"OnEnable");
                check(ui.State==BonfireUpgradePanelUI.UpgradeUIState.DeckSelection&&ui.DisplayedCardCount==14,"Re-enable returns to one full grid without stale preview");
                Call(ui,"OnDisable");
                Debug.Log((withPresentation?"[B2.R5A] PASS ":"[B2.R4] PASS ")+n+" actual Unity UI assertions (includes B2.5 regression).");
                File.WriteAllText(Path.Combine(Path.GetTempPath(),"cardgame-r4-unity-validation.txt"),"PASS "+n);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);foreach(var asset in assets)UnityEngine.Object.DestroyImmediate(asset);if(File.Exists(path))File.Delete(path);}
        }
        private static void Render(Canvas canvas,Camera camera,BonfireUpgradePanelUI ui,string path)
        {
            var texture=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=texture;
                // Prime the preview-scene camera before measuring its Canvas; Editor window size is unrelated.
                camera.Render(); Canvas.ForceUpdateCanvases();
                var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor=1;Call(scaler,"Handle");Canvas.ForceUpdateCanvases();
                ui.GetComponentInChildren<BonfireUpgradeDeckUI>(true).RefreshLayout();
                LayoutRebuilder.ForceRebuildLayoutImmediate(Get<Transform>(ui,"content").GetComponent<RectTransform>());
                foreach(var card in ui.GetComponentsInChildren<UpgradeCardChoiceView>(true))Call(card,"OnRectTransformDimensionsChange");
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
                var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                try { image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            finally { camera.targetTexture=null;RenderTexture.active=previous;texture.Release();UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static void Step(BonfireUpgradePresentation presentation,float time)=>presentation.GetType().GetMethod("Advance",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(presentation,new object[]{time});
        private static void CompleteNoMutationPresentation(BonfireUpgradePresentation presentation)
        {
            Step(presentation,Get<float>(presentation,"fadeDuration")+Get<float>(presentation,"moveDuration")+.001f);
            Step(presentation,Get<float>(presentation,"holdDelay")+.001f);
            Step(presentation,ExitTotal(presentation)+.001f);
        }
        private static float ExitTotal(BonfireUpgradePresentation presentation)=>Mathf.Max(Get<float>(presentation,"backgroundFadeDuration"),Get<float>(presentation,"cardExitDelay")+Mathf.Max(Get<float>(presentation,"exitMoveDuration"),Get<float>(presentation,"exitFadeDuration")));
        private sealed class CountingRandom : System.Random
        {
            public int Rolls;
            public override double NextDouble() { Rolls++;return .99; }
        }
        private static MapNodeData Node(string id,MapNodeType type,params string[] edges){var node=new MapNodeData();Set(node,"nodeId",id);Set(node,"nodeType",type);Set(node,"connectedNodeIds",new List<string>(edges));return node;}
        private static T Add<T>(Scene scene)where T:Component{var go=new GameObject(typeof(T).Name){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(go,scene);return go.AddComponent<T>();}
        private static T Get<T>(object obj,string field)=>(T)obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
        private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        private static void Call(object obj,string method)=>obj.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,null);
    }
}
#endif
