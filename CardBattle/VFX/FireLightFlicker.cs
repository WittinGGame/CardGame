using UnityEngine;

public class FireLightFlicker : MonoBehaviour
{
    [SerializeField] private Light targetLight;

    [Header("Intensity")]
    [SerializeField] private float baseIntensity = 3f;
    [SerializeField] private float intensityVariation = 0.6f;

    [Header("Range")]
    [SerializeField] private float baseRange = 8f;
    [SerializeField] private float rangeVariation = 0.5f;

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed = 6f;

    [Header("Color")]
    [SerializeField] private Color warmColor = new Color(1f, 0.45f, 0.15f);
    [SerializeField] private Color hotColor = new Color(1f, 0.7f, 0.3f);
    [SerializeField] private float colorVariation = 0.15f;

    private float seed;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        seed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        if (targetLight == null)
            return;

        float noise = Mathf.PerlinNoise(
            seed,
            Time.time * flickerSpeed
        );

        float centeredNoise = (noise - 0.5f) * 2f;

        targetLight.intensity =
            baseIntensity + centeredNoise * intensityVariation;

        targetLight.range =
            baseRange + centeredNoise * rangeVariation;

        float colorT =
            0.5f + centeredNoise * colorVariation;

        targetLight.color =
            Color.Lerp(warmColor, hotColor, colorT);
    }
}