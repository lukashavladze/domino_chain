using UnityEngine;

public class RevealPainter : MonoBehaviour
{
    public static RevealPainter Instance { get; private set; }

    [Header("Ground")]
    [SerializeField] private Renderer groundRenderer;
    [SerializeField] private Collider groundCollider;

    [Header("GPU Brush")]
    [SerializeField] private Material brushMaterial;

    [Header("Brush Shape")]
    [SerializeField]
    private Vector2 brushSize =
    new Vector2(0.05f, 0.02f);

    [Range(0f, 0.5f)]
    [SerializeField] private float brushSoftness = 0.05f;

    private static readonly int BrushRotationId =
    Shader.PropertyToID("_BrushRotation");

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

    public void Paint(
     Vector3 worldPosition,
     Vector3 worldForward)
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

        float rotation =
            Mathf.Atan2(
                worldForward.x,
                worldForward.z
            ) * Mathf.Rad2Deg;

        PaintUV(
            hit.textureCoord,
            rotation
        );
    }

    private void PaintUV(
     Vector2 uv,
     float rotation)
    {
        brushMaterial.SetVector(
            BrushPositionId,
            new Vector4(
                uv.x,
                uv.y,
                0f,
                0f
            )
        );

        brushMaterial.SetVector(
            BrushSizeId,
            new Vector4(
                brushSize.x,
                brushSize.y,
                0f,
                0f
            )
        );

        brushMaterial.SetFloat(
            BrushRotationId,
            rotation
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