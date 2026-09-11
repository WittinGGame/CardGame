using UnityEngine;
using UnityEngine.UI;

public class CardGlowPulse : MonoBehaviour
{
    [SerializeField] private Image glow;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 0.55f;

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;

        Color c = glow.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        glow.color = c;
    }
}