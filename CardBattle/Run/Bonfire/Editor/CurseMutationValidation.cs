#if UNITY_EDITOR
using System;
using System.Linq;
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
    public static class CurseMutationValidation
    {
        [MenuItem("Card Battle/B2.R2/Validate Automatic Mutation Upgrade")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
            var scene=EditorSceneManager.NewPreviewScene();var assets=new List<UnityEngine.Object>();
            string path=Path.Combine(Path.GetTempPath(),"r2-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var cards=AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset");
                var upgrades=AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>("Assets/ScriptsData/CardUpgrades/CardUpgradeCatalog.asset");
                upgrades=UnityEngine.Object.Instantiate(upgrades);assets.Add(upgrades);
                var run=Add<RunManager>(scene);var records=new List<RunCardRecord>();for(int i=0;i<2;i++)records.Add(new RunCardRecord("AllStrike"));
                run.StartNewRun("r2",1,"knight",100,records);run.SetCurrentHp(38);
                var rest=Node("rest",MapNodeType.Rest,"next");var act=ScriptableObject.CreateInstance<MapActData>();assets.Add(act);Set(act,"actId","r2");Set(act,"startNodeId","start");Set(act,"nodes",new List<MapNodeData>{Node("start",MapNodeType.Start,"rest"),rest,Node("next",MapNodeType.Rest)});
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
                var random=new CountingRandom(); Set(bonfire,"upgradeRandom",random); Set(bonfire,"baseMutationChance",1f);
                cards.TryGetCard("AllStrike",out var baseData); upgrades.TryGetUpgrade(baseData,out var definition);
                check(baseData.ApCost==2 && definition!=null,"AP2 authored proof available");
                var localDefinition=UnityEngine.Object.Instantiate(definition);assets.Add(localDefinition);
                var definitions=new List<CardUpgradeDefinition>(Get<List<CardUpgradeDefinition>>(upgrades,"upgrades"));
                definitions.Remove(definition);definitions.Add(localDefinition);Set(upgrades,"upgrades",definitions);
                var pool=Get<BonusUpgradeDefinition[]>(localDefinition,"bonusPool");
                Set(localDefinition,"bonusPool",new BonusUpgradeDefinition[0]);
                check(bonfire.GetEligibleUpgradeCards().Count==2,"No pool still eligible");
                string first=run.CurrentRun.currentDeck[0].runCardInstanceId,second=run.CurrentRun.currentDeck[1].runCardInstanceId;
                bonfire.TryBeginUpgrade();bonfire.TrySelectUpgradeCard(first);
                for(int i=0;i<5;i++){float preview=bonfire.MutationChance;ui.Refresh();}
                check(bonfire.MutationChance==0f && random.Rolls==0 && random.Picks==0,"Empty pool preview has zero chance and no RNG");
                bonfire.TryBackFromUpgrade();check(random.Rolls==0,"Back does not roll");
                bonfire.TryBeginUpgrade();bonfire.TrySelectUpgradeCard(first);
                int completed=0;map.OnNodeCompleted+=_=>completed++;
                int resolvedEvents=0,completedEvents=0; UpgradeResolution observed=null;
                bonfire.OnUpgradeResolved+=x=>{resolvedEvents++;observed=x;};bonfire.OnUpgradeCompleted+=_=>completedEvents++;
                check(bonfire.TryConfirmUpgradeCard(),"Empty pool confirms Guaranteed Upgrade");
                var guaranteed=run.CurrentRun.currentDeck[0].Clone();
                check(random.Rolls==1 && random.Picks==0 && guaranteed.upgradeLevel==1 && string.IsNullOrEmpty(guaranteed.selectedBonusUpgradeId),"One roll, valid Guaranteed-only result");
                check(!bonfire.IsActive && completed==1 && resolvedEvents==1 && completedEvents==1,"Automatic completion/save/map without choice screen");
                check(!bonfire.TryConfirmUpgradeCard() && random.Rolls==1,"Double Confirm does not roll");
                check(RunCardResolver.TryResolve(guaranteed,baseData,upgrades,out var guaranteedRuntime) && guaranteedRuntime.EffectiveApCost==2,"Guaranteed-only Battle resolution");
                // Same run, another copy, next test Rest session; outcome differs independently.
                map.InitializeMap();map.TrySelectNode("rest");bonfire.TryOpenSession(rest);Set(localDefinition,"bonusPool",pool);
                bonfire.TryBeginUpgrade();bonfire.TrySelectUpgradeCard(second);
                Set(bonfire,"baseMutationChance",4f);check(bonfire.MutationChance==1f,"Chance clamps high");Set(bonfire,"baseMutationChance",-2f);check(bonfire.MutationChance==0f,"Chance clamps low");Set(bonfire,"baseMutationChance",1f);
                Set(save,"saveFileName","");
                var selectedRow=Get<Transform>(ui,"content").GetComponentsInChildren<UpgradeCardChoiceView>(true).First(x=>x.OwnedCardId==second);
                Call(selectedRow,"Awake");Get<Button>(selectedRow,"button").onClick.Invoke();
                var confirm=Get<Button>(ui,"confirmButton"); confirm.onClick.Invoke();ui.Refresh();
                var mutated=run.CurrentRun.currentDeck[1];
                check(mutated.upgradeLevel==1 && mutated.selectedBonusUpgradeId==pool[0].BonusId && observed.MutationTriggered,"One curated Mutation selected and frozen");
                check(random.Rolls==2 && random.Picks==0,"Singleton pool requires no extra selection draw");
                check(guaranteed.selectedBonusUpgradeId!=mutated.selectedBonusUpgradeId && run.CurrentRun.currentDeck[0].selectedBonusUpgradeId==guaranteed.selectedBonusUpgradeId,"Duplicate copies keep independent outcomes");
                check(bonfire.State==BonfireController.SessionState.CompletedButUnsaved && !Get<GameObject>(ui,"bonusRoot").activeSelf,"Default never opens Bonus choices");
                check(bonfire.ResolvedUpgrade==observed && !bonfire.TryConfirmUpgradeCard() && random.Rolls==2,"Unsaved result locked against second Confirm");
                check(RunCardResolver.TryResolve(mutated,baseData,upgrades,out var mutatedRuntime) && mutatedRuntime.EffectiveApCost==1 && guaranteedRuntime.EffectiveApCost==2 && baseData.ApCost==2,"AP override is per owned runtime, source unchanged");
                var label=Get<TextMeshProUGUI>(Get<UpgradeCardChoiceView>(ui,"lockedCardView"),"costText");check(label.text=="1","Bonfire result uses effective AP");
                var cardView=Add<CardViewUI>(scene);var cost=new GameObject("Cost",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();SceneManager.MoveGameObjectToScene(cost.gameObject,scene);Set(cardView,"costText",cost);cardView.Bind(mutatedRuntime);check(cost.text=="1","Battle card UI uses effective AP");
                var deck=Add<DeckController>(scene);deck.BuildFromCardInstances(new[]{mutatedRuntime});deck.DrawCards(1);check(deck.DiscardCardFromHand(mutatedRuntime),"Discard mutated runtime");deck.ReshuffleGraveyardIntoDeckImmediate();deck.DrawCards(1);
                check(deck.Hand.Contains(mutatedRuntime) && mutatedRuntime.EffectiveApCost==1,"Reshuffle preserves runtime override");check(deck.ExhaustCardFromHand(mutatedRuntime) && deck.ExhaustPile.Contains(mutatedRuntime),"Exhaust preserves instance");
                deck.BuildFromCardInstances(new[]{mutatedRuntime,guaranteedRuntime});deck.DrawCards(2);
                var player=Add<PlayerBattleUnit>(scene);player.InitializeVitals(100,100);player.BeginRoundState();player.SpendApFromRunner(player.CurrentAp-1);
                var runner=Add<BattleActionRunner>(scene);Set(runner,"player",player);Set(runner,"deckController",deck);Set(runner,"enemyActionSystem",Add<EnemyActionSystem>(scene));Set(runner,"cardEffectSequenceRunner",Add<CardEffectSequenceRunner>(scene));
                var validate=typeof(BattleActionRunner).GetMethod("ValidateCardPlay",BindingFlags.Instance|BindingFlags.NonPublic);
                check((bool)validate.Invoke(runner,new object[]{mutatedRuntime}) && !(bool)validate.Invoke(runner,new object[]{guaranteedRuntime}),"Production affordability reads effective AP");
                var routine=(System.Collections.IEnumerator)typeof(BattleActionRunner).GetMethod("PlayCardSequence",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(runner,new object[]{mutatedRuntime,null,new BattleActionExecution()});
                routine.MoveNext();check(player.CurrentAp==0,"Production action spends effective AP1");(routine as IDisposable)?.Dispose();
                Render(canvas,camera,ui,"/private/tmp/cardgame-r2-unsaved.png");
                string frozen=JsonUtility.ToJson(run.GetSnapshot());bonfire.TryRetrySave();check(random.Rolls==2 && JsonUtility.ToJson(run.GetSnapshot())==frozen,"Failed Retry keeps exact outcome");
                Set(save,"saveFileName",path);check(bonfire.TryRetrySave() && random.Rolls==2 && completed==2 && completedEvents==2,"Successful Retry never rerolls or recompletes");
                check(save.TryLoad(out var loaded),"Final mutation save exists");run.RestoreRun(loaded.runState);map.RestoreMapState(loaded.mapState);
                check(run.CurrentRun.currentDeck[1].selectedBonusUpgradeId==mutated.selectedBonusUpgradeId && !bonfire.TryOpenSession(rest),"Continue preserves Mutation and completed node");
                // Nonempty pool with failed chance is also a normal Guaranteed-only result.
                map.InitializeMap();map.TrySelectNode("rest");run.AddCard("AllStrike");bonfire.TryOpenSession(rest);Set(bonfire,"baseMutationChance",0f);bonfire.TryBeginUpgrade();bonfire.TrySelectUpgradeCard(run.CurrentRun.currentDeck[2].runCardInstanceId);
                check(bonfire.TryConfirmUpgradeCard() && run.CurrentRun.currentDeck[2].upgradeLevel==1 && string.IsNullOrEmpty(run.CurrentRun.currentDeck[2].selectedBonusUpgradeId) && random.Rolls==3,"Failed Mutation chance still upgrades with empty ID");
                // Existing append content remains intact and ordered.
                cards.TryGetCard("strike",out var strike);upgrades.TryGetUpgrade(strike,out var strikeDefinition);
                var append=BonusUpgradeOfferGenerator.GetEligibleBonusIds(strike,upgrades);var record=new RunCardRecord("strike",1){selectedBonusUpgradeId=append[0]};
                check(RunCardResolver.TryResolve(record,strike,upgrades,out var appended) && appended.EffectiveEffects.Count>strikeDefinition.GuaranteedEffects.Count && appended.EffectiveEffects[0]==strikeDefinition.GuaranteedEffects[0],"Existing append Mutations preserve Guaranteed-first order");
                check(BonusUpgradeOfferGenerator.TryGenerate(strike,upgrades,new System.Random(1),out var offers) && offers.Count==3,"Reserved offer infrastructure remains available");
                map.InitializeMap();map.TrySelectNode("rest");run.AddCard("strike");bonfire.TryOpenSession(rest);Set(bonfire,"baseMutationChance",1f);bonfire.TryBeginUpgrade();
                bonfire.TrySelectUpgradeCard(run.CurrentRun.currentDeck[3].runCardInstanceId);ui.Refresh();
                var gridRows=Get<Transform>(ui,"content").GetComponentsInChildren<UpgradeCardChoiceView>(true);
                check(Get<TextMeshProUGUI>(gridRows[1],"costText").text=="1","Owned deck grid displays mutated effective AP");
                check(bonfire.TryConfirmUpgradeCard() && random.Rolls==4 && random.Picks==1,"Multi-entry pool uses one chance roll and one selection draw");
                check(run.CurrentRun.currentDeck[3].selectedBonusUpgradeId==append[0],"Selected random result belongs to that card curated pool");
                check(!bonfire.TryConfirmUpgradeCard() && random.Picks==1,"Repeated Confirm cannot choose another Mutation");
                check(baseData.ApCost==2 && baseData.TargetMode==CardTargetMode.AllEnemies,"Authored properties unchanged");
                Call(ui,"OnDisable");Debug.Log("[B2.R2] PASS "+n+" actual Unity assertions.");
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);foreach(var asset in assets)UnityEngine.Object.DestroyImmediate(asset);if(File.Exists(path))File.Delete(path);}
        }
        private sealed class CountingRandom : System.Random
        {
            public int Rolls;public int Picks;
            public override double NextDouble(){Rolls++;return .25;}
            public override int Next(int maxValue){Picks++;return 0;}
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
