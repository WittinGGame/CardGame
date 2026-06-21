using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Debug-only factory for creating a static Act 1 map without hand-authoring a ScriptableObject.
    /// </summary>
    public static class StaticAct1MapDebugFactory
    {
        public const string DefaultAssetPath = "Assets/GameData/Map/Act1_Map.asset";

        public static MapActData CreateAct1Map()
        {
            var act = ScriptableObject.CreateInstance<MapActData>();

            SetPrivateField(act, "actId", "act1");
            SetPrivateField(act, "displayName", "Act 1");
            SetPrivateField(act, "startNodeId", "start");

            var nodes = new List<MapNodeData>
            {
                CreateNode("start", "Start", MapNodeType.Start, string.Empty,
                    new[] { "act1_normal_a", "act1_normal_b" }, new Vector2(0f, 0f)),
                CreateNode("act1_normal_a", "Patrol A", MapNodeType.NormalBattle, "act1_normal_01",
                    new[] { "act1_normal_c", "act1_elite_a" }, new Vector2(-160f, 120f)),
                CreateNode("act1_normal_b", "Patrol B", MapNodeType.NormalBattle, "act1_normal_02",
                    new[] { "act1_normal_c" }, new Vector2(160f, 120f)),
                CreateNode("act1_normal_c", "Crossroad Patrol", MapNodeType.NormalBattle, "act1_normal_03",
                    new[] { "act1_boss" }, new Vector2(0f, 260f)),
                CreateNode("act1_elite_a", "Elite Guard", MapNodeType.EliteBattle, "act1_elite_01",
                    new[] { "act1_boss" }, new Vector2(-180f, 260f)),
                CreateNode("act1_boss", "Gatekeeper", MapNodeType.Boss, "act1_boss_01",
                    Array.Empty<string>(), new Vector2(0f, 420f))
            };

            SetPrivateField(act, "nodes", nodes);
            return act;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Card Battle/Map/Create Act 1 Debug Map Asset")]
        public static void CreateAct1MapAsset()
        {
            MapActData act = CreateAct1Map();
            act.name = "Act1_Map";

            string directory = System.IO.Path.GetDirectoryName(DefaultAssetPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            if (UnityEditor.AssetDatabase.LoadAssetAtPath<MapActData>(DefaultAssetPath) != null)
            {
                UnityEngine.Object.DestroyImmediate(act);
                Debug.LogWarning(
                    $"[StaticAct1MapDebugFactory] Asset already exists at '{DefaultAssetPath}'.");
                return;
            }

            UnityEditor.AssetDatabase.CreateAsset(act, DefaultAssetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log(
                $"[StaticAct1MapDebugFactory] Created Act 1 map asset at '{DefaultAssetPath}'.");
        }
#endif

        private static MapNodeData CreateNode(
            string nodeId,
            string displayName,
            MapNodeType nodeType,
            string encounterId,
            string[] connectedNodeIds,
            Vector2 uiPosition)
        {
            var node = new MapNodeData();
            SetPrivateField(node, "nodeId", nodeId);
            SetPrivateField(node, "displayName", displayName);
            SetPrivateField(node, "nodeType", nodeType);
            SetPrivateField(node, "encounterId", encounterId);
            SetPrivateField(node, "connectedNodeIds", new List<string>(connectedNodeIds));
            SetPrivateField(node, "uiPosition", uiPosition);
            return node;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            if (field == null)
            {
                Debug.LogError(
                    $"[StaticAct1MapDebugFactory] Could not find field '{fieldName}' on {target.GetType().Name}.");
                return;
            }

            field.SetValue(target, value);
        }
    }
}
