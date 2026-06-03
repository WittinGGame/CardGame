using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only camera shake utility. Knows only how to shake a transform.
    /// </summary>
    public class CameraShakeController : MonoBehaviour
    {
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private float defaultDuration = 0.12f;
        [SerializeField] private float defaultStrength = 0.06f;
        [SerializeField] private float defaultFrequency = 38f;

        private Vector3 originalLocalPosition;
        private Coroutine shakeRoutine;

        private void Awake()
        {
            if (shakeTarget == null && Camera.main != null)
                shakeTarget = Camera.main.transform;

            CacheOriginalLocalPosition();
        }

        private void OnEnable()
        {
            CacheOriginalLocalPosition();
        }

        private void OnDisable()
        {
            StopShakeRoutine();
            RestoreOriginalLocalPosition();
        }

        public void Shake()
        {
            Shake(defaultDuration, defaultStrength, defaultFrequency);
        }

        public void Shake(float duration, float strength, float frequency)
        {
            if (shakeTarget == null)
                return;

            if (shakeRoutine != null)
            {
                StopShakeRoutine();
                RestoreOriginalLocalPosition();
            }

            CacheOriginalLocalPosition();
            shakeRoutine = StartCoroutine(CoShake(duration, strength, frequency));
        }

        [ContextMenu("Test Shake")]
        private void TestShake()
        {
            Shake();
        }

        private IEnumerator CoShake(float duration, float strength, float frequency)
        {
            float clampedDuration = Mathf.Max(0.01f, duration);
            float clampedStrength = Mathf.Max(0f, strength);
            float clampedFrequency = Mathf.Max(0f, frequency);
            float elapsed = 0f;

            float seedX = Random.value * 1000f;
            float seedY = Random.value * 1000f + 100f;

            while (elapsed < clampedDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / clampedDuration);
                float fade = 1f - t;

                float sampleTime = Time.unscaledTime * clampedFrequency;
                float offsetX = (Mathf.PerlinNoise(seedX, sampleTime) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(seedY, sampleTime) - 0.5f) * 2f;
                Vector3 offset = new Vector3(offsetX, offsetY, 0f) * (clampedStrength * fade);

                shakeTarget.localPosition = originalLocalPosition + offset;
                yield return null;
            }

            RestoreOriginalLocalPosition();
            shakeRoutine = null;
        }

        private void StopShakeRoutine()
        {
            if (shakeRoutine == null)
                return;

            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        private void CacheOriginalLocalPosition()
        {
            if (shakeTarget != null)
                originalLocalPosition = shakeTarget.localPosition;
        }

        private void RestoreOriginalLocalPosition()
        {
            if (shakeTarget != null)
                shakeTarget.localPosition = originalLocalPosition;
        }
    }
}
