#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardBattle.Core.Editor
{
    public static class CurseMutationValidation
    {
        [MenuItem("Card Battle/B2.R5B1/Validate Mutation Choice Transaction")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int count = 0;
            Action<bool, string> check = (ok, why) => { count++; if (!ok) throw new Exception(why); };
            var scene = EditorSceneManager.NewPreviewScene();
            var assets = new List<UnityEngine.Object>();
            string path = Path.Combine(Path.GetTempPath(), "r5b1-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var cards = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset");
                var sourceUpgrades = AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>("Assets/ScriptsData/CardUpgrades/CardUpgradeCatalog.asset");
                var upgrades = UnityEngine.Object.Instantiate(sourceUpgrades); assets.Add(upgrades);
                var run = Add<RunManager>(scene);
                run.StartNewRun("r5b1", 1, "knight", 100, new[]
                {
                    new RunCardRecord("AllStrike"), new RunCardRecord("AllStrike"),
                    new RunCardRecord("AllStrike"), new RunCardRecord("strike")
                });
                run.SetCurrentHp(38);
                var rest = Node("rest", MapNodeType.Rest, "next");
                var act = ScriptableObject.CreateInstance<MapActData>(); assets.Add(act);
                Set(act, "actId", "r5b1"); Set(act, "startNodeId", "start");
                Set(act, "nodes", new List<MapNodeData> { Node("start", MapNodeType.Start, "rest"), rest, Node("next", MapNodeType.Rest) });
                var map = Add<MapRuntimeController>(scene); Set(map, "actData", act); map.InitializeMap(); map.TrySelectNode("rest");
                var save = Add<ActiveRunSaveService>(scene); Set(save, "saveFileName", path);
                var auto = Add<ActiveRunAutoSaveController>(scene); Set(auto, "runManager", run); Set(auto, "mapRuntimeController", map); Set(auto, "saveService", save);
                var bonfire = Add<BonfireController>(scene); Set(bonfire, "runManager", run); Set(bonfire, "mapRuntimeController", map);
                Set(bonfire, "activeRunAutoSaveController", auto); Set(bonfire, "cardCatalog", cards); Set(bonfire, "cardUpgradeCatalog", upgrades);
                var random = new CountingRandom(); Set(bonfire, "upgradeRandom", random);
                check(bonfire.TryOpenSession(rest), "Open Rest session");

                cards.TryGetCard("AllStrike", out var allStrike);
                upgrades.TryGetUpgrade(allStrike, out var sourceDefinition);
                var definition = UnityEngine.Object.Instantiate(sourceDefinition); assets.Add(definition);
                var definitions = new List<CardUpgradeDefinition>(Get<List<CardUpgradeDefinition>>(upgrades, "upgrades"));
                definitions.Remove(sourceDefinition); definitions.Add(definition); Set(upgrades, "upgrades", definitions);
                var authoredPool = Get<BonusUpgradeDefinition[]>(definition, "bonusPool");
                string first = run.CurrentRun.currentDeck[0].runCardInstanceId;

                Set(definition, "bonusPool", new BonusUpgradeDefinition[0]); Set(bonfire, "baseMutationChance", 1f);
                bonfire.TryBeginUpgrade(); bonfire.TrySelectUpgradeCard(first);
                for (int i = 0; i < 5; i++) _ = bonfire.MutationChance;
                check(random.Rolls == 0 && bonfire.MutationChance == 0f, "Preview performs zero rolls");
                bonfire.TryBackFromUpgrade(); check(random.Rolls == 0, "Back performs zero rolls");
                bonfire.TryBeginUpgrade(); bonfire.TrySelectUpgradeCard(first);
                check(bonfire.TryConfirmUpgradeCard(), "Zero-pool Confirm completes Guaranteed-only");
                check(random.Rolls == 1 && run.CurrentRun.currentDeck[0].upgradeLevel == 1 &&
                    string.IsNullOrEmpty(run.CurrentRun.currentDeck[0].selectedBonusUpgradeId), "Confirm rolls once and stores empty Mutation ID");
                check(run.CurrentRun.pendingCardUpgrade == null && map.GetNodeState("rest") == MapNodeState.Completed, "Guaranteed-only path completes normally");

                Set(definition, "bonusPool", authoredPool); map.InitializeMap(); map.TrySelectNode("rest");
                check(bonfire.TryOpenSession(rest), "Open second Rest session");
                string second = run.CurrentRun.currentDeck[1].runCardInstanceId;
                bonfire.TryBeginUpgrade(); bonfire.TrySelectUpgradeCard(second);
                float previewChance = bonfire.MutationChance;
                check(previewChance == 1f && random.Rolls == 1, "100 percent preview remains read-only");
                check(bonfire.TryConfirmUpgradeCard(), "Triggered Confirm commits Mutation offers");
                var pending = run.GetPendingCardUpgradeSnapshot();
                check(random.Rolls == 2 && pending != null && pending.mutationTriggered &&
                    pending.runCardInstanceId == second && pending.mutationChance == 1f, "One roll freezes trigger metadata");
                check(pending.offeredBonusUpgradeIds.Count == 1 && pending.offeredBonusUpgradeIds.Distinct().Count() == 1,
                    "Singleton curated pool freezes one unique offer");
                check(run.CurrentRun.currentDeck[1].upgradeLevel == 0 && map.GetNodeState("rest") == MapNodeState.Current &&
                    bonfire.IsWaitingForMutationChoice, "Triggered card and Rest remain pending");
                check(bonfire.GetOfferedMutationIds().SequenceEqual(pending.offeredBonusUpgradeIds), "Read-only Mutation offers match pending state");
                string frozen = JsonUtility.ToJson(pending);
                check(save.TryLoad(out var pendingSave) && JsonUtility.ToJson(pendingSave.runState.pendingCardUpgrade) == frozen,
                    "Pending save stores exact frozen offers");

                bonfire.ResetSession(); run.RestoreRun(pendingSave.runState); map.RestoreMapState(pendingSave.mapState);
                check(bonfire.TryOpenSession(rest) && random.Rolls == 2 &&
                    JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == frozen, "Re-entry preserves offers without reroll");
                string offered = pending.offeredBonusUpgradeIds[0];
                check(!bonfire.TrySelectMutation("not-offered") && run.CurrentRun.currentDeck[1].upgradeLevel == 0,
                    "ID outside frozen offers is rejected");
                int completed = 0; map.OnNodeCompleted += _ => completed++;
                int resolved = 0; UpgradeResolution observed = null;
                bonfire.OnUpgradeResolved += value => { resolved++; observed = value; };
                Set(save, "saveFileName", "");
                check(bonfire.TrySelectMutation(offered), "Offered Mutation commits immediately");
                var mutated = run.CurrentRun.currentDeck[1];
                check(mutated.upgradeLevel == 1 && mutated.selectedBonusUpgradeId == offered &&
                    run.CurrentRun.pendingCardUpgrade == null, "Selection applies exact final card once");
                check(completed == 1 && resolved == 1 && observed.MutationTriggered && observed.MutationId == offered,
                    "Selection resolves and completes Rest once");
                check(!bonfire.TrySelectMutation(offered) && completed == 1 && resolved == 1,
                    "Double selection cannot finalize twice");
                check(bonfire.State == BonfireController.SessionState.CompletedButUnsaved && bonfire.CanRetrySave,
                    "Failed final save retains CompletedButUnsaved transaction");
                check(bonfire.GetAppliedUpgradeApCost() == 1 && allStrike.ApCost == 2,
                    "Effective AP override remains per owned card");
                string finalState = JsonUtility.ToJson(run.GetSnapshot());
                check(!bonfire.TryRetrySave() && random.Rolls == 2 && JsonUtility.ToJson(run.GetSnapshot()) == finalState,
                    "Failed Retry does not reroll or reapply");
                Set(save, "saveFileName", path);
                check(bonfire.TryRetrySave() && random.Rolls == 2 && completed == 1, "Successful Retry only persists final state");
                check(save.TryLoad(out var finalSave) && finalSave.runState.currentDeck[1].selectedBonusUpgradeId == offered,
                    "Selected Mutation persists");

                map.InitializeMap(); map.TrySelectNode("rest"); check(bonfire.TryOpenSession(rest), "Open third Rest session");
                string third = run.CurrentRun.currentDeck[2].runCardInstanceId;
                Set(bonfire, "baseMutationChance", 0f); bonfire.TryBeginUpgrade(); bonfire.TrySelectUpgradeCard(third);
                check(bonfire.TryConfirmUpgradeCard() && random.Rolls == 3 &&
                    run.CurrentRun.currentDeck[2].upgradeLevel == 1 &&
                    string.IsNullOrEmpty(run.CurrentRun.currentDeck[2].selectedBonusUpgradeId) &&
                    run.CurrentRun.pendingCardUpgrade == null, "Zero percent remains Guaranteed-only");

                map.InitializeMap(); map.TrySelectNode("rest"); check(bonfire.TryOpenSession(rest), "Open multi-offer Rest session");
                string fourth = run.CurrentRun.currentDeck[3].runCardInstanceId;
                Set(bonfire, "baseMutationChance", 1f); bonfire.TryBeginUpgrade(); bonfire.TrySelectUpgradeCard(fourth);
                check(bonfire.TryConfirmUpgradeCard() && random.Rolls == 4, "Multi-pool Confirm still rolls once");
                var multi = run.GetPendingCardUpgradeSnapshot();
                check(multi.mutationTriggered && multi.offeredBonusUpgradeIds.Count >= 1 &&
                    multi.offeredBonusUpgradeIds.Count <= 3 && multi.offeredBonusUpgradeIds.Distinct().Count() == multi.offeredBonusUpgradeIds.Count,
                    "Multi-pool freezes one to three unique offers");
                check(!bonfire.TryConfirmUpgradeCard() && random.Rolls == 4 &&
                    JsonUtility.ToJson(run.GetPendingCardUpgradeSnapshot()) == JsonUtility.ToJson(multi),
                    "Repeated Confirm cannot reroll or regenerate offers");

                Debug.Log("[B2.R5B1] PASS " + count + " focused Mutation transaction assertions.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private sealed class CountingRandom : System.Random
        {
            public int Rolls;
            public override double NextDouble() { Rolls++; return .25; }
            public override int Next(int minValue, int maxValue) => minValue;
        }

        private static MapNodeData Node(string id, MapNodeType type, params string[] edges)
        { var node = new MapNodeData(); Set(node, "nodeId", id); Set(node, "nodeType", type); Set(node, "connectedNodeIds", new List<string>(edges)); return node; }
        private static T Add<T>(Scene scene) where T : Component
        { var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); return go.AddComponent<T>(); }
        private static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
    }
}
#endif
