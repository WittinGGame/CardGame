#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardBattle.Core.Editor
{
    public static class BonfireUpgradeUIValidation
    {
        [MenuItem("Card Battle/B2.5/Validate Upgrade Selection UI")]
        public static void Validate()
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
                var canvas=Add<Canvas>(scene);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.gameObject.AddComponent<GraphicRaycaster>();
                var camera=Add<Camera>(scene);camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;canvas.planeDistance=1;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BonfireUpgradeUISetup.PrefabPath);check(prefab!=null,"UI prefab exists");
                var host=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);host.transform.SetParent(canvas.transform,false);
                var ui=host.GetComponent<BonfireUpgradePanelUI>();ui.BindController(bonfire);Call(ui,"OnEnable");
                var confirm=Get<Button>(ui,"confirmButton");var back=Get<Button>(ui,"backButton");var retry=Get<Button>(ui,"retryButton");
                bonfire.TryBeginUpgrade();ui.Refresh();
                check(ui.DisplayedCardCount==14,"Full run deck represented");
                var content=Get<Transform>(ui,"content");var rows=content.GetComponentsInChildren<UpgradeCardChoiceView>(true);
                foreach(var row in rows)Call(row,"Awake");
                check(rows[0].OwnedCardId!=rows[1].OwnedCardId,"Duplicate base copies have separate IDs");
                check(rows[0].IsSelectable&&!rows[12].IsSelectable&&!rows[13].IsSelectable,"Eligibility dimming retains invalid/Level1 rows");
                check(!confirm.interactable,"Confirm disabled without selection");
                string before=JsonUtility.ToJson(run.GetSnapshot());string fileBefore=File.ReadAllText(path);
                Get<Button>(rows[0],"button").onClick.Invoke();ui.Refresh();
                check(bonfire.SelectedRunCardInstanceId==rows[0].OwnedCardId,"Click selects ownership ID");
                check(Get<TextMeshProUGUI>(ui,"currentText").text.Contains("5 damage")&&Get<TextMeshProUGUI>(ui,"upgradeText").text.Contains("8 damage"),"Authored preview 5 vs 8");
                check(Get<TextMeshProUGUI>(ui,"currentText").text.Contains("1 AP")&&Get<TextMeshProUGUI>(ui,"upgradeText").text.Contains("1 AP"),"Base AP in both columns");
                check(JsonUtility.ToJson(run.GetSnapshot())==before&&File.ReadAllText(path)==fileBefore&&run.CurrentRun.pendingCardUpgrade==null,"Preview has no persistent/save/RNG-offer mutation");
                Get<Button>(rows[1],"button").onClick.Invoke();ui.Refresh();
                check(bonfire.SelectedRunCardInstanceId==rows[1].OwnedCardId&&!Get<GameObject>(rows[0],"selectedRoot").activeSelf&&Get<GameObject>(rows[1],"selectedRoot").activeSelf,"Selection highlight switches");
                var firstRow=rows[0];for(int i=0;i<5;i++)ui.Refresh();check(ui.DisplayedCardCount==14&&content.GetChild(0).GetComponent<UpgradeCardChoiceView>()==firstRow,"Refresh reuses items");
                Render(canvas,camera,ui,"/private/tmp/cardgame-b25-selection.png");
                back.onClick.Invoke();check(bonfire.State==BonfireController.SessionState.Choice&&bonfire.SelectedRunCardInstanceId==""&&!Get<GameObject>(ui,"panelRoot").activeSelf,"Back uses controller and hides preview panel");
                check(bonfire.CanRest,"Rest remains available before commitment");
                bonfire.TryBeginUpgrade();ui.Refresh();rows=content.GetComponentsInChildren<UpgradeCardChoiceView>(true);foreach(var row in rows)Call(row,"Awake");
                Get<Button>(rows[0],"button").onClick.Invoke();string chosen=bonfire.SelectedRunCardInstanceId;
                Set(save,"saveFileName","");confirm.onClick.Invoke();ui.Refresh();
                check(bonfire.State==BonfireController.SessionState.UpgradeCommitted&&bonfire.GetCommittedUpgradeCardSnapshot().runCardInstanceId==chosen,"Confirm uses B2.4 commitment");
                string frozen=JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot());
                check(bonfire.GetOfferedBonusIds().Count==3&&run.CurrentRun.currentDeck[0].upgradeLevel==0,"Offers generated only after Confirm; no final apply");
                check(Get<UpgradeCardChoiceView>(ui,"lockedCardView").gameObject.activeSelf&&!Get<GameObject>(ui,"deckRoot").activeSelf,"Locked presentation replaces editable grid");
                check(!back.gameObject.activeSelf&&!confirm.gameObject.activeSelf&&!bonfire.CanRest,"Committed controls unavailable");
                check(retry.gameObject.activeSelf&&Get<TextMeshProUGUI>(ui,"statusText").text.Contains("Save retry required"),"Save failure differs from failed Confirm");
                Set(save,"saveFileName",path);retry.onClick.Invoke();ui.Refresh();
                check(!retry.gameObject.activeSelf&&JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot())==frozen,"Retry preserves offers");
                check(save.TryLoad(out var loaded),"Load UI commitment");bonfire.ResetSession();run.RestoreRun(loaded.runState);map.RestoreMapState(loaded.mapState);bonfire.TryOpenSession(rest);ui.Refresh();
                check(ui.DisplayedCardCount==0&&Get<UpgradeCardChoiceView>(ui,"lockedCardView").OwnedCardId==chosen&&!back.gameObject.activeSelf,"Restored commitment bypasses selection");
                Render(canvas,camera,ui,"/private/tmp/cardgame-b25-committed.png");
                for(int i=0;i<3;i++){Call(ui,"OnDisable");Call(ui,"OnEnable");ui.BindController(bonfire);}
                check(ui.DisplayedCardCount==0&&JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot())==frozen,"Repeated lifecycle does not reroll/create deck rows");
                // Isolate a fresh uncommitted presentation to count click handlers after repeated enable.
                bonfire.ResetSession();run.CurrentRun.pendingCardUpgrade=null;bonfire.TryOpenSession(rest);bonfire.TryBeginUpgrade();
                int transitions=0;Action changed=()=>transitions++;bonfire.OnStateChanged+=changed;
                back.onClick.Invoke();bonfire.OnStateChanged-=changed;
                check(transitions==1&&bonfire.State==BonfireController.SessionState.Choice,"One Back handler after repeated enable");
                Call(ui,"OnDisable");
                Debug.Log("[B2.5] PASS "+n+" actual Unity UI assertions.");
                File.WriteAllText(Path.Combine(Path.GetTempPath(),"cardgame-b25-unity-validation.txt"),"PASS "+n);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);foreach(var asset in assets)UnityEngine.Object.DestroyImmediate(asset);if(File.Exists(path))File.Delete(path);}
        }
        private static void Render(Canvas canvas,Camera camera,BonfireUpgradePanelUI ui,string path)
        {
            var texture=new RenderTexture(1100,850,24);var previous=RenderTexture.active;
            try{camera.targetTexture=texture;Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(Get<Transform>(ui,"content").GetComponent<RectTransform>());Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;var image=new Texture2D(1100,850,TextureFormat.RGB24,false);try{image.ReadPixels(new Rect(0,0,1100,850),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(image);}}
            finally{camera.targetTexture=null;RenderTexture.active=previous;texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
        }
        private static MapNodeData Node(string id,MapNodeType type,params string[] edges){var node=new MapNodeData();Set(node,"nodeId",id);Set(node,"nodeType",type);Set(node,"connectedNodeIds",new List<string>(edges));return node;}
        private static T Add<T>(Scene scene)where T:Component{var go=new GameObject(typeof(T).Name){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(go,scene);return go.AddComponent<T>();}
        private static T Get<T>(object obj,string field)=>(T)obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
        private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        private static void Call(object obj,string method)=>obj.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,null);
    }
}
#endif
