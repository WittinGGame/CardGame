#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardBattle.Core.Editor
{
    public static class BonusUpgradeValidation
    {
        private const string Folder = "Assets/ScriptsData/CardUpgrades/Bonuses";
        private const string UpgradePath = "Assets/ScriptsData/CardUpgrades/StrikeLevel1.asset";
        private const string CatalogPath = "Assets/ScriptsData/CardUpgrades/CardUpgradeCatalog.asset";
        [MenuItem("Card Battle/B2.3/Create Strike Bonus Examples")]
        public static void CreateExamples()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var upgrade = AssetDatabase.LoadAssetAtPath<CardUpgradeDefinition>(UpgradePath);
            var catalog = AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>(CatalogPath);
            if (upgrade == null || catalog == null) throw new Exception("B2.2 example assets are required.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/ScriptsData/CardUpgrades", "Bonuses");
            var draw = Create<DrawCardsEffectData>("Draw1", e => Set(e, "amount", 1));
            var ap = Create<GainApEffectData>("GainAP1", e => Set(e, "amount", 1));
            var block = Create<AddBlockEffectData>("Block4", e => Set(e, "blockAmount", 4));
            var weak = Create<ApplyStatusEffectData>("Weak1", e => Set(e, "statusType", StatusEffectType.Weak));
            var vulnerable = Create<ApplyStatusEffectData>("Vulnerable1", e => Set(e, "statusType", StatusEffectType.Vulnerable));
            string[] ids = { "draw_insight", "renewed_action", "guarded_strike", "weaken", "expose_weakness" };
            string[] names = { "Draw Insight", "Renewed Action", "Guarded Strike", "Weaken", "Expose Weakness" };
            CardEffectData[] effects = { draw, ap, block, weak, vulnerable };
            var additions = new List<BonusUpgradeDefinition>();
            for (int i = 0; i < ids.Length; i++)
            {
                int index = i;
                additions.Add(Create<BonusUpgradeDefinition>(ids[i], e =>
                {
                    Set(e, "bonusId", ids[index]); Set(e, "displayName", names[index]);
                    Set(e, "effects", new[] { effects[index] });
                }));
            }
            // Append missing references; repeated use never overwrites authored content.
            Append(upgrade, "bonusPool", additions);
            Append(catalog, "bonuses", additions);
            Debug.Log("[B2.3] Five Strike Bonuses authored and registered. No scene/run/save changed.");
        }
        private static T Create<T>(string name, Action<T> setup) where T : ScriptableObject
        {
            string path = Folder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (File.Exists(path)) throw new Exception("Unexpected asset at " + path);
            asset = ScriptableObject.CreateInstance<T>(); setup(asset);
            AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssetIfDirty(asset); return asset;
        }
        private static void Append(UnityEngine.Object target, string field, List<BonusUpgradeDefinition> additions)
        {
            var serialized = new SerializedObject(target); var array = serialized.FindProperty(field);
            foreach (var item in additions)
            {
                bool found = false;
                for (int i = 0; i < array.arraySize; i++) if (array.GetArrayElementAtIndex(i).objectReferenceValue == item) found = true;
                if (found) continue;
                int index = array.arraySize++; array.GetArrayElementAtIndex(index).objectReferenceValue = item;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(target);
        }
        [MenuItem("Card Battle/B2.3/Validate Bonus Foundation")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int count = 0;
            Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
            var scene = EditorSceneManager.NewPreviewScene(); var temporary = new List<UnityEngine.Object>();
            string savePath = Path.Combine(Path.GetTempPath(), "cardgame-b23-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>(CatalogPath);
                var cardCatalog = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset");
                var upgrade = AssetDatabase.LoadAssetAtPath<CardUpgradeDefinition>(UpgradePath);
                var card = upgrade.BaseCard;
                var rng = new System.Random(123);
                check(BonusUpgradeOfferGenerator.TryGenerate(card, catalog, rng, out var offers) && offers.Count == 3 && new HashSet<string>(offers).Count == 3, "3 unique curated offers");
                var copy = ScriptableObject.CreateInstance<CardUpgradeDefinition>(); temporary.Add(copy);
                Set(copy, "baseCard", card); Set(copy, "guaranteedEffects", new List<CardEffectData>(upgrade.GuaranteedEffects).ToArray());
                var local = ScriptableObject.CreateInstance<CardUpgradeCatalog>(); temporary.Add(local);
                Set(local, "upgrades", new List<CardUpgradeDefinition> { copy });
                var pool = new List<BonusUpgradeDefinition>(upgrade.BonusPool);
                Set(local, "bonuses", pool);
                for (int size = 0; size <= 2; size++)
                {
                    Set(copy, "bonusPool", pool.GetRange(0, size).ToArray());
                    bool ok = BonusUpgradeOfferGenerator.TryGenerate(card, local, rng, out var small);
                    check(ok == (size > 0) && small.Count == size, "Pool size " + size);
                }
                Set(copy, "bonusPool", new[] { pool[0], pool[0], null, pool[1] });
                check(BonusUpgradeOfferGenerator.TryGenerate(card, local, rng, out var unique) && unique.Count == 2, "Duplicate/null pool filtered");
                var manager = Add<RunManager>(scene);
                manager.StartNewRun("bonus-test", 42, "knight", 100, new[] { new RunCardRecord(card.CardId), new RunCardRecord(card.CardId) });
                string id = manager.CurrentRun.currentDeck[0].runCardInstanceId;
                check(manager.TryCommitUpgradeOffers("rest-test", id, cardCatalog, catalog, rng), "Commit offers");
                var snapshot = manager.GetSnapshot(); string pending = JsonUtility.ToJson(snapshot.pendingCardUpgrade);
                check(manager.CurrentRun.currentDeck[0].upgradeLevel == 0 && string.IsNullOrEmpty(manager.CurrentRun.currentDeck[0].selectedBonusUpgradeId), "Commit does not apply Upgrade");
                check(manager.TryCommitUpgradeOffers("rest-test", id, cardCatalog, catalog, null), "Reentry does not need RNG");
                check(JsonUtility.ToJson(manager.GetPendingCardUpgradeSnapshot()) == pending, "Pending read stable");
                check(!manager.TryCommitUpgradeOffers("other", id, cardCatalog, catalog, rng), "Cannot overwrite commitment");
                var read = manager.GetPendingCardUpgradeSnapshot(); read.offeredBonusUpgradeIds.Clear();
                check(JsonUtility.ToJson(manager.GetPendingCardUpgradeSnapshot()) == pending, "Read returns deep copy");
                var service = Add<ActiveRunSaveService>(scene); Set(service, "saveFileName", savePath);
                check(service.TrySave(ActiveRunSaveData.Create(snapshot, null, "act1", "rest-test")), "Save pending");
                check(service.TryLoad(out var loaded) && manager.RestoreRun(loaded.runState), "Load/restore pending");
                check(JsonUtility.ToJson(manager.GetPendingCardUpgradeSnapshot()) == pending, "Exact offers survive Unity JSON");
                Set(copy, "bonusPool", new BonusUpgradeDefinition[0]);
                check(BonusUpgradeOfferGenerator.TryValidatePending(manager.GetPendingCardUpgradeSnapshot(), manager.CurrentRun.currentDeck[0], card, local), "Saved offers survive pool removal");
                check(manager.TryCommitUpgradeOffers("rest-test", id, cardCatalog, local, null) &&
                    JsonUtility.ToJson(manager.GetPendingCardUpgradeSnapshot()) == pending, "Reentry after pool removal does not reroll");
                var selfCard = ScriptableObject.CreateInstance<CardData>(); temporary.Add(selfCard);
                Set(selfCard, "targetMode", CardTargetMode.Self);
                check(!pool[4].IsCompatibleWith(selfCard) && pool[2].IsCompatibleWith(selfCard), "Debuff requires enemy context; Block is player-directed");
                foreach (var entry in pool)
                    check(entry.IsCompatibleWith(card), "Strike Bonus compatible: " + entry.BonusId);
                var record = new RunCardRecord(card.CardId, 1) { selectedBonusUpgradeId = "guarded_strike" };
                string before = JsonUtility.ToJson(record);
                check(RunCardResolver.TryResolve(record, card, local, out var resolved), "Selected Bonus survives pool removal");
                check(resolved.EffectiveEffects.Count == upgrade.GuaranteedEffects.Count + 1, "Append Bonus after Guaranteed");
                string text = CardDescriptionBuilder.BuildForInstance(resolved);
                check(text.IndexOf("8 damage", StringComparison.Ordinal) < text.IndexOf("4 Block", StringComparison.Ordinal), "Description order");
                var player = Add<PlayerBattleUnit>(scene); var enemy = Add<EnemyBattleUnit>(scene); var runner = Add<CardEffectSequenceRunner>(scene);
                player.InitializeVitals(100, 100); enemy.InitializeVitals(100, 100);
                Drain(runner.ExecuteEffectsSequentially(new CardPlayContext(player, resolved, new[] { enemy }, enemy)));
                check(enemy.CurrentHp == 92 && player.CurrentBlock == 4, "Guaranteed damage then Block Bonus");
                var secondRecord = record.CopyAsNewOwnedCard();
                secondRecord.selectedBonusUpgradeId = "expose_weakness";
                check(RunCardResolver.TryResolve(secondRecord, card, catalog, out var debuff), "Second owned copy Bonus");
                check(debuff.RunCardInstanceId != resolved.RunCardInstanceId && record.selectedBonusUpgradeId == "guarded_strike", "Two independently owned cards keep different Bonuses");
                enemy.InitializeVitals(100, 100);
                Drain(runner.ExecuteEffectsSequentially(new CardPlayContext(player, debuff, new[] { enemy }, enemy)));
                check(enemy.CurrentHp == 92, "Vulnerable follows damage rather than preceding it");
                check(resolved.EffectiveEffects[resolved.EffectiveEffects.Count-1] != debuff.EffectiveEffects[debuff.EffectiveEffects.Count-1], "Independent selected effects");
                check(JsonUtility.ToJson(record) == before, "Resolution leaves persistent data unchanged");
                record.selectedBonusUpgradeId = "missing";
                check(!RunCardResolver.TryResolve(record, card, catalog, out _), "Unknown saved Bonus rejected (expected error)");
                record.selectedBonusUpgradeId = "";
                check(RunCardResolver.TryResolve(record, card, catalog, out var legacy) && legacy.EffectiveEffects.Count == upgrade.GuaranteedEffects.Count, "Legacy Guaranteed-only preserved");
                record.upgradeLevel = 0;
                check(RunCardResolver.TryResolve(record, card, catalog, out var baseCard) && CardDescriptionBuilder.BuildForInstance(baseCard).Contains("5 damage"), "Base unchanged");
                var bad = manager.GetSnapshot(); bad.pendingCardUpgrade.offeredBonusUpgradeIds.Add(bad.pendingCardUpgrade.offeredBonusUpgradeIds[0]);
                check(!BonusUpgradeOfferGenerator.TryValidatePending(bad.pendingCardUpgrade, bad.currentDeck[0], card, catalog), "Duplicate saved offers rejected");
                bad.pendingCardUpgrade.runCardInstanceId = "missing";
                check(!BonusUpgradeOfferGenerator.TryValidatePending(bad.pendingCardUpgrade, bad.currentDeck[0], card, catalog), "Dangling pending rejected");
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "cardgame-b23-unity-validation.txt"), "PASS " + count);
                Debug.Log("[B2.3] PASS " + count + " actual Unity assertions; expected invalid-data diagnostics above.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var asset in temporary) UnityEngine.Object.DestroyImmediate(asset);
                if (File.Exists(savePath)) File.Delete(savePath);
            }
        }
        private static T Add<T>(Scene scene) where T : Component
        {
            var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(go, scene); var result = go.AddComponent<T>();
            if (result is BattleUnit unit) typeof(BattleUnit).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(unit, null);
            return result;
        }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static void Drain(IEnumerator routine) { while (routine.MoveNext()) if (routine.Current is IEnumerator child) Drain(child); (routine as IDisposable)?.Dispose(); }
    }
}
#endif
