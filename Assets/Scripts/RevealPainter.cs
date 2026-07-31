using UnityEngine;

public class RevealPainter : MonoBehaviour
{
    public static RevealPainter Instance { get; private set; }

    [Header("Ground")]
    [SerializeField] private Renderer groundRenderer;
    [SerializeField] private Collider groundCollider;

    [Header("GPU Brush")]
    [SerializeField] private Material brushMaterial;

    [Range(0.005f, 0.2f)]
    [SerializeField] private float brushSize = 0.035f;

    [Range(0f, 1f)]
    [SerializeField] private float brushSoftness = 0.2f;

    [Header("Performance")]
    [SerializeField] private int maskResolution = 512;

    private RenderTexture revealMask;
    private RenderTexture temporaryMask;

    private static readonly int RevealMaskId =
        Shader.PropertyToID("_RevealMask");

    private static readonly int BrushPositionId =
        Shader.PropertyToID("_BrushPosition");

    private static readonly int BrushSizeId =
        Shader.PropertyToID("_BrushSize");

    private static readonly int BrushSoftnessId =
        Shader.PropertyToID("_BrushSoftness");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CreateRenderTextures();
        ClearMask();

        groundRenderer.material.SetTexture(
            RevealMaskId,
            revealMask
        );
    }

    private void CreateRenderTextures()
    {
        revealMask = CreateMaskTexture("Reveal Mask");
        temporaryMask = CreateMaskTexture("Temporary Reveal Mask");
    }

    private RenderTexture CreateMaskTexture(string textureName)
    {
        RenderTexture texture = new RenderTexture(
            maskResolution,
            maskResolution,
            0,
            RenderTextureFormat.R8
        );

        texture.name = textureName;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.useMipMap = false;
        texture.autoGenerateMips = false;

        texture.Create();

        return texture;
    }

    private void ClearMask()
    {
        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active = revealMask;

        GL.Clear(
            true,
            true,
            Color.black
        );

        RenderTexture.active = previous;
    }

    public void Paint(Vector3 worldPosition)
    {
        Vector3 rayOrigin =
            worldPosition + Vector3.up * 2f;

        Ray ray = new Ray(
            rayOrigin,
            Vector3.down
        );

        if (!groundCollider.Raycast(
                ray,
                out RaycastHit hit,
                5f))
        {
            return;
        }

        PaintUV(hit.textureCoord);
    }

    private void PaintUV(Vector2 uv)
    {
        brushMaterial.SetVector(
            BrushPositionId,
            new Vector4(uv.x, uv.y, 0f, 0f)
        );

        brushMaterial.SetFloat(
            BrushSizeId,
            brushSize
        );

        brushMaterial.SetFloat(
            BrushSoftnessId,
            brushSoftness
        );

        Graphics.Blit(
            revealMask,
            temporaryMask,
            brushMaterial
        );

        Graphics.Blit(
            temporaryMask,
            revealMask
        );
    }

    public void ResetMask()
    {
        ClearMask();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        ReleaseTexture(revealMask);
        ReleaseTexture(temporaryMask);
    }

    private void ReleaseTexture(RenderTexture texture)
    {
        if (texture == null)
        {
            return;
        }

        texture.Release();
        Destroy(texture);
    }
}