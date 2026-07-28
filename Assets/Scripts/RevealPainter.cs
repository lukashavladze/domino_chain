using UnityEngine;

public class RevealPainter : MonoBehaviour
{
    [Header("Reveal")]
    public RenderTexture revealMask;
    public Material brushMaterial;

    [Range(0.001f, 0.2f)]
    public float brushSize = 0.05f;

    Camera cam;

    void Start()
    {
        cam = Camera.main;

        // Clear the mask to black (everything hidden)
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = revealMask;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = active;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            PaintAtMouse();
        }
    }

    void PaintAtMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Vector2 uv = hit.textureCoord;

        brushMaterial.SetVector("_Brush",
            new Vector4(uv.x, uv.y, brushSize, 0));

        RenderTexture temp = RenderTexture.GetTemporary(
            revealMask.width,
            revealMask.height,
            0,
            revealMask.format);

        Graphics.Blit(revealMask, temp);
        Graphics.Blit(temp, revealMask, brushMaterial);

        RenderTexture.ReleaseTemporary(temp);
    }
}