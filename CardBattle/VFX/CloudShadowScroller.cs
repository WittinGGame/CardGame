using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CloudShadowScroller : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private Vector2 moveSpeed = new Vector2(0.25f, 0.07f);

    private UniversalAdditionalLightData additionalLightData;
    private Vector2 offset;

    void Awake()
    {
        additionalLightData = sunLight.GetUniversalAdditionalLightData();
        offset = additionalLightData.lightCookieOffset;
    }

    void Update()
    {
        offset += moveSpeed * Time.deltaTime;
        additionalLightData.lightCookieOffset = offset;
    }
}