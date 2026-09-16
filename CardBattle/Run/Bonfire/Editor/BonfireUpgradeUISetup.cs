#if UNITY_EDITOR
using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardBattle.Core.Editor
{
    public static class BonfireUpgradeUISetup
    {
        public const string PrefabPath = "Assets/Prefabs/BonfireUpgradePanel.prefab";
        [MenuItem("Card Battle/B2.5/Create Upgrade Panel Prefab")]
        public static void CreatePrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            { Debug.Log("[B2.5] Prefab already exists; preserving designer edits."); return; }
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var host = new GameObject("BonfireUpgradeHost", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(host, scene);
                Stretch(host.GetComponent<RectTransform>());
                var ui = host.AddComponent<BonfireUpgradePanelUI>();
                var root = Rect(host.transform,"UpgradePanel",0,0,1000,800);
                root.anchorMin = root.anchorMax = new Vector2(.5f,.5f); root.pivot = new Vector2(.5f,.5f);
                root.gameObject.AddComponent<Image>().color = new Color(.075f,.07f,.09f,.99f);
                Set(ui,"panelRoot",root.gameObject);
                Text(root,"Heading","Upgrade Card",0,-16,920,42,30,TextAlignmentOptions.Center);
                var deck = Rect(root,"DeckScroll",0,-74,900,350);
                deck.gameObject.AddComponent<Image>().color = new Color(.12f,.11f,.14f);
                deck.gameObject.AddComponent<RectMask2D>();
                var scroll = deck.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
                var content = Rect(deck,"Content",0,0,0,0);
                content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
                var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(160,220);grid.spacing=new Vector2(12,12);grid.padding=new RectOffset(20,20,12,12);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=5;
                var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport=deck;scroll.content=content;
                Set(ui,"deckRoot",deck.gameObject);Set(ui,"content",content);
                var template=Card(root,"CardTemplate");template.gameObject.SetActive(false);Set(ui,"cardTemplate",template);
                var locked=Card(root,"LockedCard");locked.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-100);locked.gameObject.SetActive(false);Set(ui,"lockedCardView",locked);
                var preview=Rect(root,"Preview",0,-440,900,225);Set(ui,"previewRoot",preview.gameObject);
                Set(ui,"selectedNameText",Text(preview,"SelectedName","Select a card",0,0,900,32,22,TextAlignmentOptions.Center));
                Set(ui,"currentText",Text(preview,"Current","",-220,-45,420,175,19));
                Set(ui,"upgradeText",Text(preview,"Guaranteed","",220,-45,420,175,19));
                Set(ui,"backButton",Button(root,"Back","Back",-270,-682));
                Set(ui,"confirmButton",Button(root,"Confirm","Confirm Card",270,-682));
                Set(ui,"retryButton",Button(root,"RetrySave","Retry Save",0,-682));
                Set(ui,"statusText",Text(root,"Status","",0,-740,900,52,17,TextAlignmentOptions.Center));
                root.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(host,PrefabPath);
                Debug.Log("[B2.5] Created "+PrefabPath+". Instantiate under Canvas; assign it to BonfirePanelUI.upgradePanelUI. No scene saved.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static UpgradeCardChoiceView Card(Transform parent,string name)
        {
            var root=Rect(parent,name,0,0,160,220);root.gameObject.AddComponent<Image>().color=new Color(.19f,.17f,.20f);
            var view=root.gameObject.AddComponent<UpgradeCardChoiceView>();Set(view,"canvasGroup",root.gameObject.AddComponent<CanvasGroup>());
            Set(view,"button",root.gameObject.AddComponent<Button>());
            var selected=Rect(root,"Selected",0,0,160,220);selected.gameObject.AddComponent<Image>().color=new Color(.86f,.65f,.22f,.35f);Set(view,"selectedRoot",selected.gameObject);selected.gameObject.SetActive(false);
            Set(view,"titleText",Text(root,"Name","",-18,-10,110,45,17));
            Set(view,"costText",Text(root,"AP","",57,-10,42,25,14,TextAlignmentOptions.Right));
            Set(view,"descriptionText",Text(root,"Description","",0,-65,140,105,16));
            Set(view,"availabilityText",Text(root,"Availability","",0,-180,140,34,13,TextAlignmentOptions.Center));
            return view;
        }
        private static Button Button(Transform parent,string name,string label,float x,float y)
        {
            var rect=Rect(parent,name,x,y,220,46);rect.gameObject.AddComponent<Image>().color=new Color(.35f,.28f,.18f);
            var button=rect.gameObject.AddComponent<Button>();Text(rect,"Label",label,0,-5,210,35,20,TextAlignmentOptions.Center);return button;
        }
        private static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,int size,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
        {
            var rect=Rect(parent,name,x,y,w,h);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;text.alignment=align;text.color=new Color(.93f,.89f,.8f);text.raycastTarget=false;return text;
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    }
}
#endif
