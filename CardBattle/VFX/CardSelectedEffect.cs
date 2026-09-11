using UnityEngine;

public class CardSelectedEffect : MonoBehaviour
{
    [SerializeField] private RectTransform border;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.04f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = border.localScale;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        border.localScale = baseScale * pulse;
    }
}