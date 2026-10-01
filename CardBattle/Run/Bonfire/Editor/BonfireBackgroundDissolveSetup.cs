#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardBattle.Core.Editor
{
    public static class BonfireBackgroundDissolveSetup
    {
        private const string GraphPath = "Assets/Shader/SG_BonfirePresentationBackgroundDissolve.shadergraph";
        private const string MaterialPath = "Assets/Material/M_BonfirePresentationBackgroundDissolve.mat";
        private const string MaskPath = "Assets/Shader/Textures/T_BonfireDissolveMask_Placeholder.png";
        private const string NoisePath = "Assets/Shader/Textures/T_BonfireDissolveNoise_Placeholder.png";

        [MenuItem("Card Battle/B2.R5D/Setup Background Dissolve")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsurePlaceholderTextures();
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            if (shader == null) throw new InvalidOperationException("Bonfire background dissolve Shader Graph did not import.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_BonfirePresentationBackgroundDissolve" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else material.shader = shader;
            material.SetTexture("_MaskTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath));
            material.SetTexture("_NoiseTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath));
            material.SetColor("_TintColor", Color.white);
            material.SetFloat("_Reveal", 1f);
            material.SetFloat("_EdgeWidth", .08f);
            material.SetFloat("_EdgeSoftness", .03f);
            material.SetFloat("_NoiseStrength", .25f);
            material.SetFloat("_NoiseTiling", 3f);
            material.SetColor("_EdgeColor", new Color(.32f, .16f, .08f, .6f));
            material.SetFloat("_EdgeIntensity", .45f);
            material.SetFloat("_InvertDirection", 0f);
            EditorUtility.SetDirty(material);

            var host = PrefabUtility.LoadPrefabContents(BonfireUpgradeUISetup.PrefabPath);
            try
            {
                var background = host.transform.Find("UpgradePanel/PresentationBackground");
                if (background == null) throw new InvalidOperationException("UpgradePanel/PresentationBackground was not found.");
                var image = background.GetComponent<Image>();
                if (image == null) throw new InvalidOperationException("PresentationBackground requires an Image.");
                var backgroundCanvasGroup = background.GetComponent<CanvasGroup>();
                if (backgroundCanvasGroup == null) backgroundCanvasGroup = background.gameObject.AddComponent<CanvasGroup>();
                image.material = material;
                var dissolve = background.GetComponent<BonfirePresentationBackgroundDissolve>();
                if (dissolve == null) dissolve = background.gameObject.AddComponent<BonfirePresentationBackgroundDissolve>();
                var serialized = new SerializedObject(dissolve);
                serialized.FindProperty("transitionMode").enumValueIndex = (int)BonfirePresentationBackgroundDissolve.BackgroundTransitionMode.Dissolve;
                serialized.FindProperty("targetImage").objectReferenceValue = image;
                serialized.FindProperty("targetCanvasGroup").objectReferenceValue = backgroundCanvasGroup;
                serialized.FindProperty("dissolveMaterial").objectReferenceValue = material;
                serialized.FindProperty("fadeInDuration").floatValue = .45f;
                serialized.FindProperty("fadeOutDuration").floatValue = .45f;
                serialized.FindProperty("initialReveal").floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var presentation = host.GetComponent<BonfireUpgradePresentation>();
                if (presentation == null) throw new InvalidOperationException("BonfireUpgradePresentation was not found.");
                var presentationSerialized = new SerializedObject(presentation);
                presentationSerialized.FindProperty("backgroundDissolve").objectReferenceValue = dissolve;
                presentationSerialized.ApplyModifiedPropertiesWithoutUndo();
                var panelUI = host.GetComponent<BonfireUpgradePanelUI>();
                if (panelUI == null) throw new InvalidOperationException("BonfireUpgradePanelUI was not found.");
                var panelSerialized = new SerializedObject(panelUI);
                panelSerialized.FindProperty("backgroundDissolve").objectReferenceValue = dissolve;
                var deckSelection = host.transform.Find("UpgradePanel/DeckSelection");
                if (deckSelection == null) throw new InvalidOperationException("UpgradePanel/DeckSelection was not found.");
                var deckCanvasGroup = deckSelection.GetComponent<CanvasGroup>();
                if (deckCanvasGroup == null) deckCanvasGroup = deckSelection.gameObject.AddComponent<CanvasGroup>();
                panelSerialized.FindProperty("deckSelectionCanvasGroup").objectReferenceValue = deckCanvasGroup;
                panelSerialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(host, BonfireUpgradeUISetup.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(host); }
            AssetDatabase.SaveAssets();
            Debug.Log("[B2.R5D] PresentationBackground dissolve material and controller configured.");
        }

        [MenuItem("Card Battle/B2.R5D/Validate Background Dissolve")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int count = 0;
            Action<bool, string> check = (ok, why) => { count++; if (!ok) throw new Exception(why); };
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            check(shader != null && material != null && material.shader == shader, "Shader Graph material is available");
            foreach (string property in new[] { "_MainTex", "_TintColor", "_MaskTexture", "_NoiseTexture", "_Reveal",
                "_EdgeWidth", "_EdgeSoftness", "_NoiseStrength", "_NoiseTiling", "_EdgeColor", "_EdgeIntensity", "_InvertDirection" })
                check(material.HasProperty(property), "Missing dissolve property " + property);
            foreach (string property in new[] { "_Stencil", "_StencilComp", "_StencilOp", "_StencilWriteMask",
                "_StencilReadMask", "_ColorMask" })
                check(material.HasProperty(property), "Canvas Shader Graph is missing UI property " + property);
            check(material.GetTexture("_MaskTexture") != null && material.GetTexture("_NoiseTexture") != null &&
                material.GetTexture("_MaskTexture") != material.GetTexture("_NoiseTexture"), "Distinct placeholder mask and noise are assigned");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonfireUpgradeUISetup.PrefabPath);
            var source = prefab.transform.Find("UpgradePanel/PresentationBackground");
            var sourceImage = source != null ? source.GetComponent<Image>() : null;
            var sourceController = source != null ? source.GetComponent<BonfirePresentationBackgroundDissolve>() : null;
            check(sourceImage != null && sourceController != null && sourceImage.material == material, "Prefab background is wired to Image, material and controller");
            check(prefab.GetComponent<BonfireUpgradePresentation>() != null, "Upgrade presentation remains present");
            var panelUI = prefab.GetComponent<BonfireUpgradePanelUI>();
            var panelSerialized = new SerializedObject(panelUI);
            check(panelSerialized.FindProperty("backgroundDissolve").objectReferenceValue == sourceController,
                "Upgrade panel lifecycle is wired to the background dissolve controller");
            var sourceSerialized = new SerializedObject(sourceController);
            check(Mathf.Approximately(sourceSerialized.FindProperty("initialReveal").floatValue, 0f),
                "Each Upgrade session starts from hidden reveal");
            check(sourceSerialized.FindProperty("targetCanvasGroup").objectReferenceValue == source.GetComponent<CanvasGroup>() &&
                sourceSerialized.FindProperty("transitionMode").enumValueIndex == (int)BonfirePresentationBackgroundDissolve.BackgroundTransitionMode.Dissolve,
                "Background CanvasGroup is wired and Dissolve remains the default transition mode");
            var deck = prefab.transform.Find("UpgradePanel/DeckSelection");
            check(deck != null && deck.GetComponent<CanvasGroup>() != null &&
                panelSerialized.FindProperty("deckSelectionCanvasGroup").objectReferenceValue == deck.GetComponent<CanvasGroup>(),
                "DeckSelection content CanvasGroup is wired to the Upgrade session UI");
            check(panelSerialized.FindProperty("deckContentFadeInDelay").floatValue >= 0f &&
                panelSerialized.FindProperty("deckContentFadeInDuration").floatValue >= 0f &&
                panelSerialized.FindProperty("deckContentFadeOutDuration").floatValue >= 0f &&
                panelSerialized.FindProperty("backgroundStartDelay").floatValue >= 0f,
                "DeckSelection transition timings are serialized, valid and designer controlled");

            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.Find("UpgradePanel").gameObject.SetActive(true);
                var controller = instance.GetComponentInChildren<BonfirePresentationBackgroundDissolve>(true);
                var image = controller.GetComponent<Image>();
                Call(controller, "Awake");
                check(image.material != null && image.material != material, "Controller uses a private runtime material instance");
                controller.SetReveal(0f);
                check(Mathf.Approximately(controller.Reveal, 0f) && Mathf.Approximately(image.material.GetFloat("_Reveal"), 0f), "Reveal 0 is fully driven to material");
                controller.SetReveal(1f);
                check(Mathf.Approximately(controller.Reveal, 1f) && Mathf.Approximately(image.material.GetFloat("_Reveal"), 1f), "Reveal 1 is fully driven to material");
                var canvasGroup = controller.GetComponent<CanvasGroup>();
                var controllerSerialized = new SerializedObject(controller);
                controllerSerialized.FindProperty("transitionMode").enumValueIndex = (int)BonfirePresentationBackgroundDissolve.BackgroundTransitionMode.Fade;
                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
                controller.SetReveal(0f);
                check(Mathf.Approximately(canvasGroup.alpha, 0f) && Mathf.Approximately(image.material.GetFloat("_Reveal"), 1f),
                    "Fade mode uses only normal UI alpha and keeps dissolve fully revealed");
                controller.SetReveal(1f);
                check(Mathf.Approximately(canvasGroup.alpha, 1f), "Fade mode reaches fully visible UI alpha");
                controllerSerialized.FindProperty("transitionMode").enumValueIndex = (int)BonfirePresentationBackgroundDissolve.BackgroundTransitionMode.Dissolve;
                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
                controller.SetReveal(0f);
                check(Mathf.Approximately(canvasGroup.alpha, 1f) && Mathf.Approximately(image.material.GetFloat("_Reveal"), 0f),
                    "Dissolve mode keeps normal alpha visible and drives only Reveal");
                controller.PlayFadeIn();
                check(Mathf.Approximately(controller.Reveal, 1f), "Fade-in API reaches visible state in focused Editor validation");
                controller.PlayFadeOut();
                check(Mathf.Approximately(controller.Reveal, 0f), "Fade-out API reaches hidden state in focused Editor validation");
                int fadeOutCompletions = 0;
                controller.FadeOutCompleted += () => fadeOutCompletions++;
                controller.SetReveal(1f);
                controller.PlayFadeOut();
                check(fadeOutCompletions == 1 && Mathf.Approximately(controller.Reveal, 0f),
                    "Fade-out completion is raised once after reaching hidden state");
                Call(controller, "OnDisable");
                check(Mathf.Approximately(controller.Reveal, 0f), "Disable cancels fades and restores configured hidden reveal");
                UnityEngine.Object.DestroyImmediate(instance);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Debug.Log("[B2.R5D] PASS " + count + " focused background dissolve assertions.");
        }

        private static void EnsurePlaceholderTextures()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MaskPath));
            if (!File.Exists(MaskPath)) WriteTexture(MaskPath, false);
            if (!File.Exists(NoisePath)) WriteTexture(NoisePath, true);
            AssetDatabase.ImportAsset(MaskPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(NoisePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture(MaskPath, TextureWrapMode.Clamp);
            ConfigureTexture(NoisePath, TextureWrapMode.Repeat);
        }

        private static void WriteTexture(string path, bool noise)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[size * size];
            var random = new System.Random(1979);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    byte value = noise ? (byte)random.Next(0, 256) :
                        (byte)Mathf.RoundToInt(Mathf.Clamp01((x + y) / (float)(size * 2 - 2)) * 255f);
                    pixels[y * size + x] = new Color32(value, value, value, 255);
                }
            texture.SetPixels32(pixels); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ConfigureTexture(string path, TextureWrapMode wrap)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.wrapMode = wrap;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void Call(object target, string method) => target.GetType()
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    }
}
#endif
