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
    // Editor-only authoring/validation. Never changes a live run or saves a scene.
    public static class CardUpgradeValidation
    {
        private const string Folder = "Assets/ScriptsData/CardUpgrades";
        private const string CatalogPath = Folder + "/CardUpgradeCatalog.asset";

        [MenuItem("Card Battle/B2.2/Create Strike Upgrade Example")]
        public static void CreateExample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var strike = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptsData/Cards/Card_Strike.asset");
            if (strike == null) throw new InvalidOperationException("Base Strike asset is missing.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/ScriptsData", "CardUpgrades");
            // Never overwrite designer edits when this menu is invoked again.
            var damage = LoadOrCreate<DealDamageEffectData>(Folder + "/StrikeUpgradeDamage8.asset", asset => Set(asset, "damage", 8));
            var definition = LoadOrCreate<CardUpgradeDefinition>(Folder + "/StrikeLevel1.asset", asset =>
            {
                Set(asset, "baseCard", strike);
                Set(asset, "guaranteedEffects", new CardEffectData[] { damage });
            });
            LoadOrCreate<CardUpgradeCatalog>(CatalogPath, asset => Set(asset, "upgrades", new List<CardUpgradeDefinition> { definition }));
            Debug.Log("[B2.2] Example assets ready. Assign CardUpgradeCatalog to BattleRunBridge; no scene was saved.");
        }

        private static T LoadOrCreate<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Unexpected asset at " + path);
            var asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        [MenuItem("Card Battle/B2.2/Validate Guaranteed Upgrade")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int assertions = 0;
            Action<bool, string> check = (ok, message) => { assertions++; if (!ok) throw new Exception(message); };
            var temporaryAssets = new List<UnityEngine.Object>();
            var randomState = UnityEngine.Random.state;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var data = AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptsData/Cards/Card_Strike.asset");
                var catalog = AssetDatabase.LoadAssetAtPath<CardUpgradeCatalog>(CatalogPath);
                check(data != null && catalog != null, "Create example assets first.");
                var baseRecord = new RunCardRecord(data.CardId);
                var upgradedRecord = new RunCardRecord(data.CardId, 1);
                string before = JsonUtility.ToJson(upgradedRecord);
                check(RunCardResolver.TryResolve(baseRecord, data, null, out var baseCard), "Base resolution");
                check(RunCardResolver.TryResolve(upgradedRecord, data, catalog, out var upgraded), "Upgrade resolution");
                check(baseCard.Data == upgraded.Data && upgraded.Data == data, "Keep base asset");
                check(baseCard.RunCardInstanceId != upgraded.RunCardInstanceId, "Independent ownership");
                check(upgraded.RunCardInstanceId == upgradedRecord.runCardInstanceId, "Persistent identity retained");
                check(baseCard.InstanceId != upgraded.InstanceId, "Independent battle identity");
                check(CardDescriptionBuilder.BuildForInstance(baseCard).Contains("5 damage"), "Base description");
                check(CardDescriptionBuilder.BuildForInstance(upgraded).Contains("8 damage"), "Upgrade description");
                check(data.ApCost == upgraded.Data.ApCost && data.TargetMode == upgraded.Data.TargetMode && data.CardType == upgraded.Data.CardType, "Base properties");
                check(data.Retain == upgraded.Data.Retain && data.Temporary == upgraded.Data.Temporary && data.ExhaustAfterPlay == upgraded.Data.ExhaustAfterPlay, "Base keywords");
                var player = Add<PlayerBattleUnit>(scene);
                var enemy = Add<EnemyBattleUnit>(scene);
                var runner = Add<CardEffectSequenceRunner>(scene);
                var sync = Add<CardResolver>(scene);
                var deck = Add<DeckController>(scene);
                player.InitializeVitals(100, 100);
                enemy.InitializeVitals(100, 100);
                Drain(runner.ExecuteEffectsSequentially(new CardPlayContext(player, baseCard, new[] { enemy }, enemy)));
                check(enemy.CurrentHp == 95, "Base production damage = 5");
                enemy.InitializeVitals(100, 100);
                Drain(runner.ExecuteEffectsSequentially(new CardPlayContext(player, upgraded, new[] { enemy }, enemy)));
                check(enemy.CurrentHp == 92, "Upgrade production damage = 8");
                enemy.InitializeVitals(100, 100);
                sync.Resolve(new CardPlayContext(player, upgraded, new[] { enemy }, enemy));
                check(enemy.CurrentHp == 92, "Sync uses same sequence");
                var status = ScriptableObject.CreateInstance<ApplyStatusEffectData>(); temporaryAssets.Add(status);
                var def = ScriptableObject.CreateInstance<CardUpgradeDefinition>(); temporaryAssets.Add(def);
                var multiCatalog = ScriptableObject.CreateInstance<CardUpgradeCatalog>(); temporaryAssets.Add(multiCatalog);
                Set(def, "baseCard", data);
                Set(def, "guaranteedEffects", new CardEffectData[] { upgraded.EffectiveEffects[0], status, upgraded.EffectiveEffects[0] });
                Set(multiCatalog, "upgrades", new List<CardUpgradeDefinition> { def });
                check(RunCardResolver.TryResolve(upgradedRecord, data, multiCatalog, out var multi), "Multi-effect resolution");
                enemy.InitializeVitals(100, 100);
                Drain(runner.ExecuteEffectsSequentially(new CardPlayContext(player, multi, new[] { enemy }, enemy)));
                check(enemy.CurrentHp == 80, "Authored order: 8 then Vulnerable then 12");
                check(CardDescriptionBuilder.BuildForInstance(multi).Split('\n').Length == 3, "Multi-effect description");
                deck.BuildFromCardInstances(new[] { baseCard, upgraded });
                check(deck.Deck.Count == 2, "Resolved deck count");
                deck.DrawCards(2);
                check(deck.PlayCardFromHand(upgraded), "Play resolved card");
                check(deck.Graveyard.Contains(upgraded), "Same runtime copy in Graveyard");
                deck.DrawCards(1);
                check(deck.Hand.Contains(upgraded) && upgraded.EffectiveEffects[0] != data.Effects[0], "Reshuffle preserves upgrade");
                check(JsonUtility.ToJson(upgradedRecord) == before, "Execution does not mutate persistent data");
                check(CardDescriptionBuilder.Build(data).Contains("5 damage"), "Base asset unchanged");
                check(!RunCardResolver.TryResolve(upgradedRecord, data, null, out _), "Missing definition rejected (expected error)");
                Set(multiCatalog, "upgrades", new List<CardUpgradeDefinition> { def, def });
                check(!multiCatalog.TryGetUpgrade(data, out _), "Duplicate rejected (expected error)");
                var bridge = Add<BattleRunBridge>(scene);
                Set(bridge, "deckController", deck);
                Set(bridge, "cardCatalog", AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptsData/CardCatalog.asset"));
                Set(bridge, "cardUpgradeCatalog", catalog);
                var run = new RunState { currentDeck = new List<RunCardRecord> { baseRecord, upgradedRecord } };
                string runBefore = JsonUtility.ToJson(run);
                var resolve = typeof(BattleRunBridge).GetMethod("TryResolveDeckFromRun", BindingFlags.Instance | BindingFlags.NonPublic);
                object[] args = { run, null };
                check((bool)resolve.Invoke(bridge, args), "Bridge resolves per-record deck");
                var resolved = (List<CardInstance>)args[1];
                check(resolved.Count == 2 && resolved[0].RunCardInstanceId == baseRecord.runCardInstanceId &&
                    resolved[1].RunCardInstanceId == upgradedRecord.runCardInstanceId, "Bridge preserves ownership");
                check(CardDescriptionBuilder.BuildForInstance(resolved[0]).Contains("5 damage") &&
                    CardDescriptionBuilder.BuildForInstance(resolved[1]).Contains("8 damage"), "Bridge mixed Base/Upgrade");
                deck.BuildFromCardInstances(resolved);
                Set(bridge, "cardUpgradeCatalog", null);
                args = new object[] { run, null };
                check(!(bool)resolve.Invoke(bridge, args), "Bridge rejects missing Upgrade (expected error)");
                check(deck.Deck.Count == 2 && deck.Deck.Contains(resolved[0]) && deck.Deck.Contains(resolved[1]), "Failure leaves deck unchanged");
                check(JsonUtility.ToJson(run) == runBefore, "Bridge is read-only to run");
                var keywordData = ScriptableObject.CreateInstance<CardData>(); temporaryAssets.Add(keywordData);
                var keywordCard = new CardInstance(keywordData, effectiveEffects: upgraded.EffectiveEffects);
                Set(keywordData, "retain", true);
                check(DeckController.ResolveEndTurnDestination(keywordCard) == DeckController.EndTurnCardDestination.Hand, "Retain routing");
                Set(keywordData, "exhaustAfterPlay", true);
                check(DeckController.ResolvePlayedCardDestination(keywordCard) == PlayedCardDestination.Exhaust, "Exhaust routing");
                Set(keywordData, "temporary", true);
                check(DeckController.ResolvePlayedCardDestination(keywordCard) == PlayedCardDestination.Removed &&
                    DeckController.ResolveDiscardDestination(keywordCard) == DeckController.DiscardDestination.Removed, "Temporary routing");
                upgradedRecord.upgradeLevel = 2;
                check(!RunCardResolver.TryResolve(upgradedRecord, data, catalog, out _), "Unsupported level rejected (expected error)");
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "cardgame-b22-unity-validation.txt"), "PASS " + assertions + " actual Unity assertions");
                Debug.Log("[B2.2] PASS " + assertions + " actual Unity assertions; expected negative-case errors above.");
            }
            finally
            {
                UnityEngine.Random.state = randomState;
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var asset in temporaryAssets) UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static T Add<T>(Scene scene) where T : Component
        {
            var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(go, scene);
            var component = go.AddComponent<T>();
            // Edit-mode preview objects do not receive normal Play-mode Awake.
            if (component is BattleUnit unit)
                typeof(BattleUnit).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unit, null);
            return component;
        }
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Drain(IEnumerator routine)
        {
            while (routine.MoveNext()) if (routine.Current is IEnumerator nested) Drain(nested);
            (routine as IDisposable)?.Dispose();
        }
        private static bool Contains(this IReadOnlyList<CardInstance> cards, CardInstance card)
        {
            for (int i = 0; i < cards.Count; i++) if (cards[i] == card) return true;
            return false;
        }
    }
}
#endif
