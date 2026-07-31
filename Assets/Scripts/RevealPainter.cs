using UnityEngine;

public class RevealPainter : MonoBehaviour
{
    public static RevealPainter Instance;

    [Header("Ground")]
    public Renderer groundRenderer;

    [Header("Brush")]
    public Texture2D brushTexture;

    [Range(0.01f, 0.2f)]
    public float brushSize = 0.05f;

    Texture2D maskTexture;

    void Awake()
    {
        Instance = this;

        maskTexture = new Texture2D(1024, 1024, TextureFormat.R8, false, true);

        Color32[] pixels = new Color32[1024 * 1024];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.black;

        maskTexture.SetPixels32(pixels);
        maskTexture.Apply();

        groundRenderer.material.SetTexture("_RevealMask", maskTexture);
        Debug.Log(groundRenderer.material.GetTexture("_RevealMask"));
    }

    public void Paint(Vector3 worldPosition)
    {
        Ray ray = new Ray(worldPosition + Vector3.up * 5f, Vector3.down);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Renderer renderer = hit.collider.GetComponent<Renderer>();

        if (renderer == null)
            return;

        Vector2 uv = hit.textureCoord;

        StampBrush(uv);
    }

    void StampBrush(Vector2 uv)
    {
        int centerX = Mathf.RoundToInt(uv.x * maskTexture.width);
        int centerY = Mathf.RoundToInt(uv.y * maskTexture.height);

        int radius = Mathf.RoundToInt(maskTexture.width * brushSize);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int px = centerX + x;
                int py = centerY + y;

                if (px < 0 || py < 0 || px >= maskTexture.width || py >= maskTexture.height)
                    continue;

                float u = (x + radius) / (float)(radius * 2);
                float v = (y + radius) / (float)(radius * 2);

                Color brush = brushTexture.GetPixelBilinear(u, v);

                if (brush.r <= 0f)
                    continue;

                Color current = maskTexture.GetPixel(px, py);

                if (brush.r > current.r)
                    maskTexture.SetPixel(px, py, Color.white * brush.r);
            }
        }

        maskTexture.Apply(false);
        Debug.Log(maskTexture.GetPixel(centerX, centerY));
    }
}