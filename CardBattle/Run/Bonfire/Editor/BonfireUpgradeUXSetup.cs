#if UNITY_EDITOR
using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core.Editor
{
    // Explicit, idempotent prefab migration. Does not touch the open scene or source card prefab.
    public static class BonfireUpgradeUXSetup
    {
        [MenuItem("Card Battle/B2.R4/Upgrade Bonfire UI Structure")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var host = PrefabUtility.LoadPrefabContents(BonfireUpgradeUISetup.PrefabPath);
            try
            {
                var ui = host.GetComponent<BonfireUpgradePanelUI>();
                if (Get<BonfireUpgradePreviewUI>(ui, "previewScreen") != null)
                { Debug.Log("[B2.R4] Already configured; preserving designer edits."); return; }
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardViewUI.prefab").GetComponent<CardViewUI>();
                var root = Get<GameObject>(ui, "panelRoot").GetComponent<RectTransform>();
                Stretch(root);
                root.GetComponent<Image>().color = new Color(.055f,.05f,.065f,.98f);
                var heading = root.Find("Heading").GetComponent<TextMeshProUGUI>();
                Place(heading.rectTransform, 0, -35, 1000, 70); heading.text = "UPGRADE"; heading.fontSize = 42;
                var deckScreen = Rect(root, "DeckSelection", 0, 0, 0, 0); Stretch(deckScreen);
                var deck = Get<GameObject>(ui,"deckRoot").GetComponent<RectTransform>(); deck.SetParent(deckScreen,false);
                deck.anchorMin = new Vector2(.13f,.12f); deck.anchorMax = new Vector2(.87f,.85f);
                deck.offsetMin = deck.offsetMax = Vector2.zero;
                deck.GetComponent<Image>().color = new Color(0,0,0,.12f);
                var scroll = deck.GetComponent<ScrollRect>(); scroll.scrollSensitivity = 70;
                var content = Get<Transform>(ui,"content"); var grid = content.GetComponent<GridLayoutGroup>();
                grid.constraintCount = 4; grid.cellSize = new Vector2(280,420); grid.spacing = new Vector2(44,56);
                grid.padding = new RectOffset(28,28,28,28);
                var deckUI = deckScreen.gameObject.AddComponent<BonfireUpgradeDeckUI>(); Set(deckUI,"grid",grid); Set(deckUI,"scrollRect",scroll);
                Set(ui,"deckRoot",deckScreen.gameObject);
                var template = Get<UpgradeCardChoiceView>(ui,"cardTemplate"); InstallCardVisual(template,source);
                var locked = Get<UpgradeCardChoiceView>(ui,"lockedCardView"); InstallCardVisual(locked,source);
                Place(locked.GetComponent<RectTransform>(),0,-100,160,240);
                var preview = Get<GameObject>(ui,"previewRoot").GetComponent<RectTransform>(); Stretch(preview); preview.name = "PreviewSelection";
                foreach (string field in new[]{"selectedNameText","currentText","upgradeText"})
                    Get<TextMeshProUGUI>(ui,field).gameObject.SetActive(false); // Preserve old serialized references for existing tools.
                var previewUI = preview.gameObject.AddComponent<BonfireUpgradePreviewUI>(); Set(ui,"previewScreen",previewUI);
                var current = UnityEngine.Object.Instantiate(template,preview); current.name = "CurrentCard"; current.gameObject.SetActive(true);
                var guaranteed = UnityEngine.Object.Instantiate(template,preview); guaranteed.name = "GuaranteedCard"; guaranteed.gameObject.SetActive(true);
                Place(current.GetComponent<RectTransform>(),-330,-250,320,480);
                Place(guaranteed.GetComponent<RectTransform>(),330,-250,320,480);
                Set(previewUI,"currentCard",current); Set(previewUI,"guaranteedCard",guaranteed);
                Label(preview,"CurrentLabel","CURRENT",-330,-200,360,40,25);
                Label(preview,"GuaranteedLabel","GUARANTEED UPGRADE",330,-200,400,40,25);
                Label(preview,"TransitionArrow","> > >",0,-440,200,85,55);
                Set(previewUI,"mutationChanceText",Label(preview,"MutationChance","Mutation Chance: 0%",0,-780,900,50,25));
                var back = Get<Button>(ui,"backButton"); back.transform.SetParent(deckScreen,false); Bottom(back.GetComponent<RectTransform>(),.08f,60);
                var previewBack = UnityEngine.Object.Instantiate(back,preview); previewBack.name="PreviewBack"; Set(ui,"previewBackButton",previewBack);
                var confirm = Get<Button>(ui,"confirmButton"); confirm.transform.SetParent(preview,false); Bottom(confirm.GetComponent<RectTransform>(),.5f,85);
                confirm.GetComponentInChildren<TextMeshProUGUI>().text="CONFIRM";
                Bottom(Get<Button>(ui,"retryButton").GetComponent<RectTransform>(),.5f,85);
                Bottom(Get<Button>(ui,"applyBonusButton").GetComponent<RectTransform>(),.7f,85);
                var status = Get<TextMeshProUGUI>(ui,"statusText"); Bottom(status.rectTransform,.5f,15); status.rectTransform.sizeDelta=new Vector2(1150,52);
                root.gameObject.SetActive(false); template.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(host,BonfireUpgradeUISetup.PrefabPath);
                Debug.Log("[B2.R4] Bonfire prefab updated. Existing card visuals reused; no scene or artwork asset changed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(host); }
        }
        private static void InstallCardVisual(UpgradeCardChoiceView view, CardViewUI source)
        {
            var original = Get<RectTransform>(source,"visualRoot");
            foreach(Transform child in view.transform) child.gameObject.SetActive(false);
            var visual = UnityEngine.Object.Instantiate(original,view.transform); visual.name="CardVisual";
            visual.anchorMin=visual.anchorMax=visual.pivot=new Vector2(.5f,.5f); visual.anchoredPosition=Vector2.zero;
            Set(view,"visualRoot",visual); Set(view,"fitVisualToCard",true); Set(view,"compactCost",true);
            Set(view,"titleText",CopyReference<TextMeshProUGUI>(source,"nameText",original,visual));
            Set(view,"costText",CopyReference<TextMeshProUGUI>(source,"costText",original,visual));
            Set(view,"descriptionText",CopyReference<TextMeshProUGUI>(source,"descriptionText",original,visual));
            Set(view,"artworkImage",CopyReference<Image>(source,"artworkImage",original,visual));
            Set(view,"typeBadgeImage",CopyReference<Image>(source,"typeBadgeImage",original,visual));
            Set(view,"typeBadgeSet",Get<CardTypeBadgeSet>(source,"typeBadgeSet"));
            // Only static uGUI/TMP lives under this source VisualRoot; no hand interaction or VFX is copied.
            foreach(var component in visual.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(component is Graphic)) throw new InvalidOperationException("Unexpected non-visual card component: "+component.GetType().Name);
            foreach(var graphic in visual.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget=false;
            var availability=Get<TextMeshProUGUI>(view,"availabilityText"); availability.gameObject.SetActive(true);
            CaptionInsideCard(availability.rectTransform);
            availability.fontSize=18; availability.transform.SetAsLastSibling();
            var image=view.GetComponent<Image>(); image.color=Color.clear; image.raycastTarget=true;
            Get<Button>(view,"button").transition=Selectable.Transition.None;
            view.GetComponent<RectTransform>().sizeDelta=new Vector2(280,420);
        }
        private static void CaptionInsideCard(RectTransform rect)
        { rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,8);rect.sizeDelta=new Vector2(250,28); }
        private static T CopyReference<T>(object source,string field,Transform original,Transform copy) where T:Component
        {
            var component=Get<T>(source,field);
            return copy.Find(AnimationUtility.CalculateTransformPath(component.transform,original)).GetComponent<T>();
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        { var go=new GameObject(name,typeof(RectTransform)); var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);Place(rect,x,y,w,h);return rect; }
        private static void Place(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one; }
        private static void Bottom(RectTransform rect,float x,float y)
        { rect.anchorMin=rect.anchorMax=new Vector2(x,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,y); }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private static TextMeshProUGUI Label(Transform parent,string name,string value,float x,float y,float w,float h,int size)
        { var text=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.color=new Color(.93f,.89f,.8f);return text; }
        private static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        private static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    }
}
#endif
