using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class BonfirePresentationBackgroundDissolve : MonoBehaviour
    {
        public enum BackgroundTransitionMode { Fade, Dissolve }

        private static readonly int RevealId = Shader.PropertyToID("_Reveal");

        [SerializeField, InspectorName("Background Transition Mode")] private BackgroundTransitionMode transitionMode = BackgroundTransitionMode.Dissolve;
        [SerializeField] private Image targetImage;
        [SerializeField] private CanvasGroup targetCanvasGroup;
        [SerializeField] private Material dissolveMaterial;
        [SerializeField, Min(0f), InspectorName("Background Fade In Duration")] private float fadeInDuration = .45f;
        [SerializeField, Min(0f), InspectorName("Background Fade Out Duration")] private float fadeOutDuration = .45f;
        [SerializeField, Range(0f, 1f)] private float initialReveal = 0f;

        public float Reveal { get; private set; } = 1f;
        public float FadeInDuration => fadeInDuration;
        public float FadeOutDuration => fadeOutDuration;
        public BackgroundTransitionMode TransitionMode => transitionMode;
        public event Action FadeOutCompleted;

        private Material runtimeMaterial;
        private Material originalMaterial;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            EnsureMaterialInstance();
            SetReveal(initialReveal);
        }

        private void OnEnable()
        {
            EnsureMaterialInstance();
            SetReveal(initialReveal);
        }

        public void SetReveal(float value)
        {
            CancelFade();
            ApplyReveal(value);
        }

        private void ApplyReveal(float value)
        {
            Reveal = Mathf.Clamp01(value);
            EnsureMaterialInstance();
            if (targetCanvasGroup == null) targetCanvasGroup = GetComponent<CanvasGroup>();
            if (transitionMode == BackgroundTransitionMode.Fade)
            {
                if (targetCanvasGroup != null) targetCanvasGroup.alpha = Reveal;
                if (runtimeMaterial != null && runtimeMaterial.HasProperty(RevealId)) runtimeMaterial.SetFloat(RevealId, 1f);
            }
            else
            {
                if (targetCanvasGroup != null) targetCanvasGroup.alpha = 1f;
                if (runtimeMaterial != null && runtimeMaterial.HasProperty(RevealId)) runtimeMaterial.SetFloat(RevealId, Reveal);
            }
        }

        public void PlayFadeIn() => Play(1f, fadeInDuration, null);
        public void PlayFadeOut() => Play(0f, fadeOutDuration, () => FadeOutCompleted?.Invoke());

        private void Play(float target, float duration, Action completed)
        {
            CancelFade();
            if (!Application.isPlaying)
            {
                ApplyReveal(target);
                completed?.Invoke();
                return;
            }
            if (!isActiveAndEnabled) return;
            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                ApplyReveal(target);
                completed?.Invoke();
                return;
            }
            fadeRoutine = StartCoroutine(FadeTo(target, duration, completed));
        }

        private IEnumerator FadeTo(float target, float duration, Action completed)
        {
            float start = Reveal;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ApplyReveal(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            ApplyReveal(target);
            fadeRoutine = null;
            completed?.Invoke();
        }

        private void CancelFade()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        private void EnsureMaterialInstance()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (runtimeMaterial != null || targetImage == null) return;
            Material source = dissolveMaterial != null ? dissolveMaterial : targetImage.material;
            if (source == null) return;
            originalMaterial = targetImage.material;
            runtimeMaterial = new Material(source) { name = source.name + " (Runtime)" };
            targetImage.material = runtimeMaterial;
        }

        private void OnDisable()
        {
            CancelFade();
            ApplyReveal(initialReveal);
        }

        private void OnDestroy()
        {
            if (targetImage != null && targetImage.material == runtimeMaterial)
                targetImage.material = originalMaterial;
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeMaterial);
                else DestroyImmediate(runtimeMaterial);
            }
            runtimeMaterial = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
            initialReveal = Mathf.Clamp01(initialReveal);
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetCanvasGroup == null) targetCanvasGroup = GetComponent<CanvasGroup>();
        }
#endif
    }
}
