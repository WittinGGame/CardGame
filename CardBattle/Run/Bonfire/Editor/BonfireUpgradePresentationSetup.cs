#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CardBattle.Core.Editor
{
    public static class BonfireUpgradePresentationSetup
    {
        [MenuItem("Card Battle/B2.R5A/Setup Upgrade Presentation")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var host = PrefabUtility.LoadPrefabContents(BonfireUpgradeUISetup.PrefabPath);
            try
            {
                var ui = host.GetComponent<BonfireUpgradePanelUI>();
                if (Get<BonfireUpgradePresentation>(ui,"presentation") != null)
                { Debug.Log("[B2.R5A] Already wired; preserving designer settings."); return; }
                var preview = Get<BonfireUpgradePreviewUI>(ui,"previewScreen");
                if (preview == null) throw new InvalidOperationException("R4 preview must be configured first.");
                var card = Get<UpgradeCardChoiceView>(preview,"guaranteedCard").GetComponent<RectTransform>();
                var previewRoot = preview.GetComponent<RectTransform>();
                var children = previewRoot.Cast<Transform>().ToArray();
                var fade = Rect(previewRoot,"PreviewFadeGroup");
                fade.anchorMin=Vector2.zero;fade.anchorMax=Vector2.one;fade.offsetMin=fade.offsetMax=Vector2.zero;
                var fadeGroup=fade.gameObject.AddComponent<CanvasGroup>();
                foreach(var child in children) if(child!=card && child.name!="VFX") child.SetParent(fade,true);
                var panelRoot=Get<GameObject>(ui,"panelRoot").transform;
                var heading=panelRoot.Find("Heading");
                var titleGroup=heading != null ? heading.GetComponent<CanvasGroup>() ?? heading.gameObject.AddComponent<CanvasGroup>() : null;
                // Centered pivot wrapper prevents the card's authored top pivot affecting the flip/move.
                var motion=Rect(previewRoot,"GuaranteedMotion");motion.anchorMin=card.anchorMin;motion.anchorMax=card.anchorMax;motion.sizeDelta=card.rect.size;
                motion.position=card.TransformPoint(card.rect.center);
                card.SetParent(motion,true);
                var center=Rect(previewRoot,"UpgradeCenterAnchor");center.sizeDelta=Vector2.zero;
                var presentation=host.AddComponent<BonfireUpgradePresentation>();
                Set(presentation,"fadeGroups",titleGroup != null ? new[]{fadeGroup,titleGroup} : new[]{fadeGroup});Set(presentation,"motionRoot",motion);Set(presentation,"centerTarget",center);
                Set(ui,"presentation",presentation);
                PrefabUtility.SaveAsPrefabAsset(host,BonfireUpgradeUISetup.PrefabPath);
                Debug.Log("[B2.R5A] Presentation wired. Grid dimensions, artwork and open scene unchanged.");
            }
            finally { PrefabUtility.UnloadPrefabContents(host); }
        }
        private static RectTransform Rect(Transform parent,string name)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            return rect;
        }
        private static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        private static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    }
}
#endif
