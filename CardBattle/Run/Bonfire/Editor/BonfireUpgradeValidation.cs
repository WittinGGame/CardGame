#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardBattle.Core.Editor
{
    public static class BonfireUpgradeValidation
    {
        [MenuItem("Card Battle/B2.4/Validate Bonfire Upgrade Commitment")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int count = 0;
            Action<bool, string> check = (ok, why) => { count++; if (!ok) throw new Exception(why); };
            var scene = EditorSceneManager.NewPreviewScene();
            var assets = new List<UnityEngine.Object>();
            string path = Path.Combine(Path.GetTempPath(), "bonfire-b24-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var cards = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset");
                var upgrades = AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>("Assets/ScriptsData/CardUpgrades/CardUpgradeCatalog.asset");
                check(cards != null && upgrades != null, "Example catalogs exist");
                var run = Add<RunManager>(scene);
                run.StartNewRun("b24-test", 1, "knight", 100, new[] { new RunCardRecord("strike"), new RunCardRecord("strike"), new RunCardRecord("strike", 1), new RunCardRecord("missing") });
                var rest = Node("rest", MapNodeType.Rest, "next");
                var next = Node("next", MapNodeType.Rest);
                var act = ScriptableObject.CreateInstance<MapActData>(); assets.Add(act);
                Set(act, "actId", "test-act"); Set(act, "startNodeId", "start");
                Set(act, "nodes", new List<MapNodeData> { Node("start", MapNodeType.Start, "rest"), rest, next });
                var map = Add<MapRuntimeController>(scene); Set(map, "actData", act); map.InitializeMap();
                check(map.TrySelectNode("rest"), "Select Rest");
                var save = Add<ActiveRunSaveService>(scene); Set(save, "saveFileName", path);
                var auto = Add<ActiveRunAutoSaveController>(scene); Set(auto, "saveService", save); Set(auto, "runManager", run); Set(auto, "mapRuntimeController", map);
                var bonfire = Add<BonfireController>(scene);
                Set(bonfire, "runManager", run); Set(bonfire, "mapRuntimeController", map); Set(bonfire, "activeRunAutoSaveController", auto);
                Set(bonfire, "cardCatalog", cards); Set(bonfire, "cardUpgradeCatalog", upgrades);
                check(!bonfire.TryBeginUpgrade(), "Inactive cannot Upgrade");
                check(!bonfire.TryOpenSession(Node("rest", MapNodeType.NormalBattle)), "Non-Rest rejected");
                check(bonfire.TryOpenSession(rest), "Open Rest");
                check(bonfire.State == BonfireController.SessionState.Choice, "Initial choice");
                check(bonfire.GetEligibleUpgradeCards().Count == 2, "Exclude upgraded/missing definitions");
                check(!bonfire.CanRest && bonfire.CanBeginUpgrade && !bonfire.CanLeave, "Full HP still exposes Upgrade");
                var eligible = bonfire.GetEligibleUpgradeCards(); string first = eligible[0].runCardInstanceId, second = eligible[1].runCardInstanceId;
                eligible[0].upgradeLevel = 1;
                check(run.CurrentRun.currentDeck[0].upgradeLevel == 0, "Eligibility snapshots isolated");
                string before = JsonUtility.ToJson(run.GetSnapshot()), fileBefore = File.ReadAllText(path);
                check(bonfire.TryBeginUpgrade(), "Begin Upgrade");
                check(bonfire.TrySelectUpgradeCard(first) && bonfire.TrySelectUpgradeCard(second), "Switch selection before Confirm");
                check(bonfire.SelectedRunCardInstanceId == second && JsonUtility.ToJson(run.GetSnapshot()) == before && File.ReadAllText(path) == fileBefore, "Selection does not mutate/save");
                check(bonfire.TryBackFromUpgrade() && bonfire.SelectedRunCardInstanceId == "" && bonfire.State == BonfireController.SessionState.Choice, "Back clears transient state");
                run.SetCurrentHp(38);
                check(bonfire.CanRest, "Back leaves Rest available");
                check(bonfire.TryBeginUpgrade() && bonfire.TrySelectUpgradeCard(first), "Select again");
                run.CurrentRun.currentDeck[0].upgradeLevel = 1;
                check(!bonfire.TryConfirmUpgradeCard() && run.CurrentRun.pendingCardUpgrade == null, "Level changed before Confirm rejected");
                run.CurrentRun.currentDeck[0].upgradeLevel = 0;
                var removed = run.CurrentRun.currentDeck[0]; run.CurrentRun.currentDeck.RemoveAt(0);
                check(!bonfire.TryConfirmUpgradeCard(), "Removed ownership rejected"); run.CurrentRun.currentDeck.Insert(0, removed);
                Set(bonfire, "cardUpgradeCatalog", null);
                check(!bonfire.TryConfirmUpgradeCard(), "Missing Upgrade definition rejected"); Set(bonfire, "cardUpgradeCatalog", upgrades);
                cards.TryGetCard("strike", out var strike); upgrades.TryGetUpgrade(strike, out var definition);
                var noPoolDefinition = ScriptableObject.CreateInstance<CardUpgradeDefinition>(); assets.Add(noPoolDefinition);
                Set(noPoolDefinition, "baseCard", strike); Set(noPoolDefinition, "guaranteedEffects", new List<CardEffectData>(definition.GuaranteedEffects).ToArray());
                var noPool = ScriptableObject.CreateInstance<CardUpgradeCatalog>(); assets.Add(noPool); Set(noPool, "upgrades", new List<CardUpgradeDefinition>{noPoolDefinition});
                Set(bonfire, "cardUpgradeCatalog", noPool);
                check(bonfire.GetEligibleUpgradeCards().Count == 0 && !bonfire.TryConfirmUpgradeCard(), "Empty pool before Confirm rejected"); Set(bonfire, "cardUpgradeCatalog", upgrades);
                bool reentrantAccepted = false;
                Action attempt = () => { if (bonfire.IsBusy) reentrantAccepted |= bonfire.TryConfirmUpgradeCard() || bonfire.TryRest() || bonfire.TryLeave() || bonfire.TrySelectUpgradeCard(second); };
                bonfire.OnStateChanged += attempt;
                Action<RunState> duringRunChange = _ => attempt(); run.OnRunChanged += duringRunChange;
                Set(save, "saveFileName", "");
                check(bonfire.TryConfirmUpgradeCard(), "Commit accepted despite save failure");
                check(!reentrantAccepted, "Reentrant calls rejected");
                string frozen = JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot());
                check(bonfire.State == BonfireController.SessionState.UpgradeCommitted && bonfire.HasCommittedChoice && bonfire.CanRetrySave, "Committed unsaved state");
                check(run.CurrentRun.currentDeck[0].upgradeLevel == 0 && string.IsNullOrEmpty(run.CurrentRun.currentDeck[0].selectedBonusUpgradeId), "Persistent card remains base");
                check(bonfire.GetCommittedUpgradeCardSnapshot().runCardInstanceId == first && bonfire.GetOfferedBonusIds().Count == 3 && new HashSet<string>(bonfire.GetOfferedBonusIds()).Count == 3, "Locked card and unique offers");
                check(!bonfire.TryConfirmUpgradeCard() && !bonfire.TrySelectUpgradeCard(second) && !bonfire.TryBackFromUpgrade() && !bonfire.TryRest() && !bonfire.TryLeave(), "All alternate actions blocked");
                check(!bonfire.TryRetrySave() && JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == frozen, "Failed retry preserves offers");
                check(bonfire.TryOpenSession(rest) && bonfire.CanRetrySave && JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == frozen, "Same-session reentry preserves unsaved offers");
                Set(save, "saveFileName", path);
                check(bonfire.TryRetrySave() && !bonfire.CanRetrySave && JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == frozen, "Retry saves same offers");
                check(map.HasSelectedNode && map.GetNodeState("rest") == MapNodeState.Current && map.GetNodeState("next") == MapNodeState.Locked, "Confirm did not complete/unlock");
                check(save.TryLoad(out var loaded), "Load committed save");
                run.OnRunChanged -= duringRunChange; bonfire.OnStateChanged -= attempt;
                bonfire.ResetSession();
                check(run.RestoreRun(loaded.runState) && map.RestoreMapState(loaded.mapState), "Restore run and map");
                check(bonfire.TryOpenSession(rest) && bonfire.State == BonfireController.SessionState.UpgradeCommitted && JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == frozen, "Continue resumes exact commitment");
                check(!bonfire.CanRest && !bonfire.CanLeave && !bonfire.TrySelectUpgradeCard(second), "Resumed commitment locked");
                run.CurrentRun.pendingCardUpgrade.offeredBonusUpgradeIds[0] = "missing-bonus";
                check(!bonfire.TryOpenSession(rest) && bonfire.State == BonfireController.SessionState.InvalidUpgrade && !bonfire.CanRetrySave, "Unknown pending Bonus rejected visibly");
                run.RestoreRun(loaded.runState); bonfire.ResetSession(); run.CurrentRun.pendingCardUpgrade.nodeId = "next";
                check(!bonfire.TryOpenSession(rest) && !bonfire.CanRest && !bonfire.CanLeave, "Wrong node commitment rejected");
                run.RestoreRun(loaded.runState); bonfire.ResetSession(); run.CurrentRun.pendingCardUpgrade.runCardInstanceId = "missing-owner";
                check(!bonfire.TryOpenSession(rest) && bonfire.GetCommittedUpgradeCardSnapshot() == null, "Missing pending ownership rejected");
                run.CurrentRun.pendingCardUpgrade = null; bonfire.ResetSession();
                check(bonfire.TryOpenSession(rest) && bonfire.CanRest && bonfire.TryRest() && run.CurrentRun.currentHp == 68, "Existing Rest heal/completion remains valid");
                Debug.Log("[B2.4] PASS " + count + " actual Unity assertions; expected validation/save-failure diagnostics above.");
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "cardgame-b24-unity-validation.txt"), "PASS " + count);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
                if (File.Exists(path)) File.Delete(path);
            }
        }
        private static MapNodeData Node(string id, MapNodeType type, params string[] edges)
        {
            var node = new MapNodeData(); Set(node,"nodeId",id); Set(node,"nodeType",type); Set(node,"connectedNodeIds",new List<string>(edges)); return node;
        }
        private static T Add<T>(Scene scene) where T:Component
        {
            var go = new GameObject(typeof(T).Name) {hideFlags=HideFlags.HideAndDontSave}; SceneManager.MoveGameObjectToScene(go,scene); return go.AddComponent<T>();
        }
        private static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    }
}
#endif
