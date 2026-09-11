using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.01f, 0.0f);

    private Material materialInstance;
    private Vector2 offset;

    void Start()
    {
        materialInstance = targetRenderer.material;
        offset = materialInstance.mainTextureOffset;
    }

    void Update()
    {
        offset += scrollSpeed * Time.deltaTime;
        materialInstance.mainTextureOffset = offset;
    }
}