#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CardBattle.Core.Editor
{
    // Idempotent prefab migration from the R5C prototype controller to one presentation-owned toggle.
    public static class BonfireCurseVFXSetup
    {
        [MenuItem("Card Battle/B2.R5C1/Simplify Curse Mutation VFX")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var host = PrefabUtility.LoadPrefabContents(BonfireUpgradeUISetup.PrefabPath);
            try
            {
                var presentation = host.GetComponent<BonfireUpgradePresentation>();
                if (presentation == null) throw new InvalidOperationException("R5B2 presentation must be configured first.");
                var preview = host.transform.Find("UpgradePanel/PreviewSelection");
                var front = preview != null ? preview.Find("CurseVFXFront") : null;
                if (front == null) throw new InvalidOperationException("PreviewSelection/CurseVFXFront was not found.");

                front.gameObject.SetActive(false);
                bool firstSimpleWiring = Get<GameObject>(presentation, "curseVFXFront") == null;
                Set(presentation, "curseVFXFront", front.gameObject);
                if (firstSimpleWiring) Set(presentation, "mutationVFXDuration", .4f);

                var oldBack = preview.Find("GuaranteedMotion/CurseVFXBack");
                if (oldBack != null) oldBack.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(host, BonfireUpgradeUISetup.PrefabPath);
                Debug.Log("[B2.R5C1] Simplified to presentation-owned CurseVFXFront toggle. Visual children preserved; open scenes unchanged.");
            }
            finally { PrefabUtility.UnloadPrefabContents(host); }
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }
}
#endif
