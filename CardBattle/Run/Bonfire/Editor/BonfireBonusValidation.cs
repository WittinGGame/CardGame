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
    public static class BonfireBonusValidation
    {
        [MenuItem("Card Battle/B2.6/Validate Final Bonus Apply")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
            var scene=EditorSceneManager.NewPreviewScene();var assets=new List<UnityEngine.Object>();
            string path=Path.Combine(Path.GetTempPath(),"b26-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var cards=AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset");
                var upgrades=AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>("Assets/ScriptsData/CardUpgrades/CardUpgradeCatalog.asset");
                upgrades=UnityEngine.Object.Instantiate(upgrades);assets.Add(upgrades);
                var run=Add<RunManager>(scene);var records=new List<RunCardRecord>();for(int i=0;i<2;i++)records.Add(new RunCardRecord("strike"));
                run.StartNewRun("b26",1,"knight",100,records);run.SetCurrentHp(38);
                var rest=Node("rest",MapNodeType.Rest,"next");var act=ScriptableObject.CreateInstance<MapActData>();assets.Add(act);Set(act,"actId","b26");Set(act,"startNodeId","start");Set(act,"nodes",new List<MapNodeData>{Node("start",MapNodeType.Start,"rest"),rest,Node("next",MapNodeType.Rest)});
                var map=Add<MapRuntimeController>(scene);Set(map,"actData",act);map.InitializeMap();map.TrySelectNode("rest");
                var save=Add<ActiveRunSaveService>(scene);Set(save,"saveFileName",path);
                var auto=Add<ActiveRunAutoSaveController>(scene);Set(auto,"runManager",run);Set(auto,"mapRuntimeController",map);Set(auto,"saveService",save);
                var bonfire=Add<BonfireController>(scene);Set(bonfire,"runManager",run);Set(bonfire,"mapRuntimeController",map);Set(bonfire,"activeRunAutoSaveController",auto);Set(bonfire,"cardCatalog",cards);Set(bonfire,"cardUpgradeCatalog",upgrades);bonfire.TryOpenSession(rest);
                var mapUI=Add<TreeMapUIController>(scene);Set(mapUI,"mapRuntimeController",map);Set(bonfire,"treeMapUIController",mapUI);mapUI.Hide();
                var canvas=Add<Canvas>(scene);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.gameObject.AddComponent<GraphicRaycaster>();
                var camera=Add<Camera>(scene);camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;canvas.planeDistance=1;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BonfireUpgradeUISetup.PrefabPath);check(prefab!=null,"UI prefab exists");
                var host=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);host.transform.SetParent(canvas.transform,false);
                var ui=host.GetComponent<BonfireUpgradePanelUI>();ui.BindController(bonfire);Call(ui,"OnEnable");
                bonfire.TryBeginUpgrade(); bonfire.TrySelectUpgradeCard(run.CurrentRun.currentDeck[0].runCardInstanceId); bonfire.TryConfirmUpgradeCard(); ui.Refresh();
                var pending = run.GetPendingCardUpgradeSnapshot(); string chosen = pending.runCardInstanceId;
                var originalPending = pending.Clone(); string frozen = JsonUtility.ToJson(pending);
                var views = Get<BonusUpgradeChoiceView[]>(ui, "bonusChoices"); foreach (var view in views) Call(view, "Awake");
                var apply = Get<Button>(ui, "applyBonusButton"); var retry = Get<Button>(ui, "retryButton");
                check(views.Length == 3 && !apply.interactable, "Three Bonus slots, Confirm requires selection");
                for (int count = 1; count <= 3; count++)
                {
                    run.CurrentRun.pendingCardUpgrade = originalPending.Clone();
                    run.CurrentRun.pendingCardUpgrade.offeredBonusUpgradeIds.RemoveRange(count, 3-count); ui.Refresh();
                    int active = 0; for (int i=0;i<views.Length;i++) if(views[i].gameObject.activeSelf) { active++; check(views[i].BonusId == originalPending.offeredBonusUpgradeIds[i], "Exact persisted ID/order"); }
                    check(active == count, "One to three offers represented");
                }
                run.CurrentRun.pendingCardUpgrade = originalPending.Clone(); ui.Refresh();
                check(!bonfire.TrySelectBonus("not-offered") && !bonfire.CanApplySelectedBonus, "Offered membership required");
                string before = JsonUtility.ToJson(run.GetSnapshot()); string fileBefore = File.ReadAllText(path);
                Get<Button>(views[0], "button").onClick.Invoke();
                check(bonfire.SelectedBonusId == views[0].BonusId && apply.interactable, "Bonus click selects transient ID");
                Get<Button>(views[1], "button").onClick.Invoke();
                check(bonfire.SelectedBonusId == views[1].BonusId && Get<GameObject>(views[1],"selectedRoot").activeSelf && !Get<GameObject>(views[0],"selectedRoot").activeSelf, "Selection switches highlight");
                check(JsonUtility.ToJson(run.GetSnapshot()) == before && File.ReadAllText(path) == fileBefore, "Selection does not mutate or save");
                for(int i=0;i<5;i++)ui.Refresh();
                check(JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == frozen, "Refresh never rerolls");
                // Restore the persisted commitment before Apply; only transient selection is lost.
                check(save.TryLoad(out var committedSave), "Committed save exists");
                bonfire.ResetSession(); run.RestoreRun(committedSave.runState); map.RestoreMapState(committedSave.mapState); bonfire.TryOpenSession(rest); ui.Refresh();
                check(JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == frozen && !apply.interactable && views[0].BonusId == originalPending.offeredBonusUpgradeIds[0], "Continue restores exact offers without selection");
                bonfire.TrySelectBonus(views[0].BonusId);
                var locked = run.CurrentRun.currentDeck[0];
                run.CurrentRun.pendingCardUpgrade.offeredBonusUpgradeIds[1] = views[0].BonusId;
                check(!bonfire.TryApplySelectedBonus() && locked.upgradeLevel == 0, "Duplicate offers rejected");
                run.CurrentRun.pendingCardUpgrade = originalPending.Clone();
                run.CurrentRun.pendingCardUpgrade.nodeId = "wrong";
                check(!bonfire.TryApplySelectedBonus() && locked.upgradeLevel == 0, "Wrong pending node rejected");
                run.CurrentRun.pendingCardUpgrade = originalPending.Clone();
                run.CurrentRun.currentDeck.RemoveAt(0);
                check(!bonfire.TryApplySelectedBonus(), "Missing owned card rejected"); run.CurrentRun.currentDeck.Insert(0,locked);
                locked.upgradeLevel=1; check(!bonfire.TryApplySelectedBonus(), "Already upgraded rejected"); locked.upgradeLevel=0;
                Set(bonfire,"cardUpgradeCatalog",null); check(!bonfire.TryApplySelectedBonus() && locked.upgradeLevel==0, "Missing upgrade catalog rejected"); Set(bonfire,"cardUpgradeCatalog",upgrades);
                var registry = Get<List<BonusUpgradeDefinition>>(upgrades,"bonuses");
                var registryCopy = new List<BonusUpgradeDefinition>(registry);
                var without = new List<BonusUpgradeDefinition>(registry); without.RemoveAll(x=>x!=null && x.BonusId==views[0].BonusId);
                try { Set(upgrades,"bonuses",without); check(!bonfire.TryApplySelectedBonus() && locked.upgradeLevel==0,"Unresolvable saved Bonus rejected"); }
                finally { Set(upgrades,"bonuses",registryCopy); }
                // Pick the authored Draw bonus so the resulting sequence has a precise expectation.
                string drawId = null; foreach(string id in originalPending.offeredBonusUpgradeIds) if(upgrades.TryGetBonus(id,out var bonus)) foreach(var effect in bonus.Effects) if(effect is DrawCardsEffectData) drawId=id;
                check(drawId != null && bonfire.TrySelectBonus(drawId), "Authored Draw offer available");
                Render(canvas,camera,ui,"/private/tmp/cardgame-b26-choices.png");
                int completes=0; Action<MapNodeData> completed=_=>completes++; map.OnNodeCompleted+=completed;
                bool reentered=false; Action reenter=()=>{if(bonfire.IsBusy)reentered |= bonfire.TryApplySelectedBonus() || bonfire.TrySelectBonus(views[1].BonusId) || bonfire.TryRest();}; bonfire.OnStateChanged+=reenter;
                Set(save,"saveFileName",""); apply.onClick.Invoke(); ui.Refresh(); bonfire.OnStateChanged-=reenter;
                check(!reentered,"Reentrant actions blocked");
                check(locked.upgradeLevel==1 && locked.selectedBonusUpgradeId==drawId && locked.runCardInstanceId==chosen,"Final Apply mutates exact ownership and Bonus");
                check(run.CurrentRun.currentDeck[1].upgradeLevel==0 && string.IsNullOrEmpty(run.CurrentRun.currentDeck[1].selectedBonusUpgradeId),"Other base copy unchanged");
                check(completes==1 && map.GetNodeState("rest")==MapNodeState.Completed && map.GetNodeState("next")==MapNodeState.Available,"Complete once and unlock next through Map API");
                check(run.CurrentRun.pendingCardUpgrade==null && !map.HasSelectedNode,"Pending cleared only with final card/node state");
                check(bonfire.State==BonfireController.SessionState.CompletedButUnsaved && bonfire.HasAppliedUpgrade && retry.gameObject.activeSelf,"Final save failure locks result with Retry");
                check(!apply.gameObject.activeSelf && !Get<GameObject>(ui,"bonusRoot").activeSelf && !bonfire.CanRest && !bonfire.TryApplySelectedBonus(),"No second Apply or alternate action");
                check(cards.TryGetCard("strike",out var data) && RunCardResolver.TryResolve(locked,data,upgrades,out _),"Future Battle resolver accepts completed card");
                RunCardResolver.TryResolve(locked,data,upgrades,out var resolved); RunCardResolver.TryResolve(run.CurrentRun.currentDeck[1],data,upgrades,out var baseCard);
                check(resolved.EffectiveEffects.Count==2 && resolved.EffectiveEffects[0] is DealDamageEffectData && resolved.EffectiveEffects[1] is DrawCardsEffectData,"Guaranteed damage before Bonus Draw");
                string description=CardDescriptionBuilder.BuildForInstance(resolved);
                check(description.Contains("8 damage") && description.IndexOf("8 damage",StringComparison.Ordinal)<description.IndexOf("Draw",StringComparison.Ordinal),"Final description follows effective order");
                check(CardDescriptionBuilder.BuildForInstance(baseCard).Contains("5 damage"),"Duplicate Base remains Damage 5");
                Render(canvas,camera,ui,"/private/tmp/cardgame-b26-unsaved.png");
                string finalRun=JsonUtility.ToJson(run.GetSnapshot());
                for(int i=0;i<3;i++){Call(ui,"OnDisable");Call(ui,"OnEnable");}
                check(bonfire.HasAppliedUpgrade && retry.gameObject.activeSelf && JsonUtility.ToJson(run.GetSnapshot())==finalRun,"UI reopen preserves applied result");
                Set(save,"saveFileName",path); retry.onClick.Invoke();
                check(!bonfire.IsActive && !Get<GameObject>(ui,"panelRoot").activeSelf && completes==1 && mapUI.IsVisible,"Retry closes session without re-completing");
                check(JsonUtility.ToJson(run.GetSnapshot())==finalRun,"Retry does not mutate final card");
                check(save.TryLoad(out var finalSave) && (finalSave.runState.pendingCardUpgrade == null || !finalSave.runState.pendingCardUpgrade.isCommitted),"Final saved snapshot has no pending commitment");
                run.RestoreRun(finalSave.runState); map.RestoreMapState(finalSave.mapState);
                check(!bonfire.TryOpenSession(rest) && map.GetNodeState("rest")==MapNodeState.Completed && !map.HasSelectedNode,"Continue final state does not reopen Bonfire");
                check(run.CurrentRun.currentDeck[0].selectedBonusUpgradeId==drawId && run.CurrentRun.currentDeck[0].upgradeLevel==1,"Chosen Bonus persists on Continue");
                map.OnNodeCompleted-=completed; Call(ui,"OnDisable");
                Debug.Log("[B2.6] PASS "+n+" actual Unity assertions.");
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
