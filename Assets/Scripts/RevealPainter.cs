using System.Collections;
using UnityEngine;

public class RevealPainter : MonoBehaviour
{
    public static RevealPainter Instance { get; private set; }

    [Header("Ground")]
    [SerializeField] private Renderer groundRenderer;
    [SerializeField] private Collider groundCollider;

    [Header("Reveal Cover")]
    [SerializeField] private Renderer coverRenderer;

    [Header("GPU Brush")]
    [SerializeField] private Material brushMaterial;

    [Header("Brush Shape")]
    [Range(0f, 0.5f)]
    [SerializeField] private float brushSoftness = 0.05f;

    [Header("Performance")]
    [SerializeField] private int maskResolution = 512;


    [Header("Final Reveal")]
    [SerializeField] private float finalRevealDuration = 1.0f;

    [Range(0.001f, 0.2f)]
    [SerializeField] private float finalRevealSoftness = 0.025f;

    [Range(0.001f, 0.2f)]
    [SerializeField] private float waveWidth = 0.025f;

    [SerializeField]
    private Color waveColor =
        new Color(0f, 1f, 0.45f, 1f);

    [Range(0f, 5f)]
    [SerializeField] private float waveIntensity = 2.0f;


    private RenderTexture revealMask;
    private RenderTexture temporaryMask;

    private Material groundMaterial;

    private Material coverMaterial;


    private static readonly int RevealMaskId =
        Shader.PropertyToID("_RevealMask");

    private static readonly int BrushPositionId =
        Shader.PropertyToID("_BrushPosition");

    private static readonly int BrushSizeId =
        Shader.PropertyToID("_BrushSize");

    private static readonly int BrushSoftnessId =
        Shader.PropertyToID("_BrushSoftness");

    private static readonly int BrushRotationId =
        Shader.PropertyToID("_BrushRotation");


    // Final reveal shader properties
    private static readonly int FinalRevealActiveId =
        Shader.PropertyToID("_FinalRevealActive");

    private static readonly int FinalRevealRadiusId =
        Shader.PropertyToID("_FinalRevealRadius");

    private static readonly int FinalRevealSoftnessId =
        Shader.PropertyToID("_FinalRevealSoftness");

    private static readonly int WaveWidthId =
        Shader.PropertyToID("_WaveWidth");

    private static readonly int WaveColorId =
        Shader.PropertyToID("_WaveColor");

    private static readonly int WaveIntensityId =
        Shader.PropertyToID("_WaveIntensity");


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

        // Cache material once.
        groundMaterial =
            groundRenderer.material;

        if (coverRenderer != null)
        {
            coverMaterial = coverRenderer.material;
        }

        groundMaterial.SetTexture(
            RevealMaskId,
            revealMask
        );

        if (coverMaterial != null)
        {
            coverMaterial.SetTexture(
                RevealMaskId,
                revealMask
            );
        }

        ResetFinalRevealShader();
    }


    private void CreateRenderTextures()
    {
        revealMask =
            CreateMaskTexture(
                "Reveal Mask"
            );

        temporaryMask =
            CreateMaskTexture(
                "Temporary Reveal Mask"
            );
    }


    private RenderTexture CreateMaskTexture(
        string textureName)
    {
        RenderTexture texture =
            new RenderTexture(
                maskResolution,
                maskResolution,
                0,
                RenderTextureFormat.R8
            );

        texture.name = textureName;

        texture.filterMode =
            FilterMode.Bilinear;

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.useMipMap = false;
        texture.autoGenerateMips = false;

        texture.Create();

        return texture;
    }


    private void ClearMask()
    {
        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active =
            revealMask;

        GL.Clear(
            true,
            true,
            Color.black
        );

        RenderTexture.active =
            previous;
    }


    // =====================================================
    // NORMAL DOMINO REVEAL
    // =====================================================

    public void Paint(
        Vector3 worldPosition,
        Vector3 worldForward,
        Vector2 worldBrushSize)
    {
        Vector3 rayOrigin =
            worldPosition +
            Vector3.up * 2f;

        Ray ray =
            new Ray(
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


        Bounds groundBounds =
            groundRenderer.bounds;


        float uvWidth =
            worldBrushSize.x /
            groundBounds.size.x;

        float uvHeight =
            worldBrushSize.y /
            groundBounds.size.z;


        PaintUV(
            hit.textureCoord,
            rotation,
            new Vector2(
                uvWidth,
                uvHeight
            )
        );
    }


    private void PaintUV(
        Vector2 uv,
        float rotation,
        Vector2 uvBrushSize)
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
                uvBrushSize.x,
                uvBrushSize.y,
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


        groundMaterial.SetTexture(
            RevealMaskId,
            revealMask
        );
    }


    // =====================================================
    // FINAL RADIAL REVEAL
    // =====================================================

    public IEnumerator RevealAllRadial()
    {
        if (groundMaterial == null)
        {
            Debug.LogWarning(
                "RevealPainter: Ground material is missing."
            );

            yield break;
        }


        // ==========================================
        // START FINAL REVEAL
        // ==========================================

        groundMaterial.SetFloat(
            FinalRevealActiveId,
            1f
        );

        groundMaterial.SetFloat(
            FinalRevealRadiusId,
            0f
        );

        groundMaterial.SetFloat(
            FinalRevealSoftnessId,
            finalRevealSoftness
        );


        // ==========================================
        // GREEN WAVE - IMAGE MATERIAL
        // ==========================================

        groundMaterial.SetFloat(
            WaveWidthId,
            waveWidth
        );

        groundMaterial.SetColor(
            WaveColorId,
            waveColor
        );

        groundMaterial.SetFloat(
            WaveIntensityId,
            waveIntensity
        );


        // ==========================================
        // COVER MATERIAL
        // ==========================================

        if (coverMaterial != null)
        {
            coverMaterial.SetFloat(
                FinalRevealActiveId,
                1f
            );

            coverMaterial.SetFloat(
                FinalRevealRadiusId,
                0f
            );

            coverMaterial.SetFloat(
                FinalRevealSoftnessId,
                finalRevealSoftness
            );


            // Green wave must ALSO be sent to cover.
            coverMaterial.SetFloat(
                WaveWidthId,
                waveWidth
            );

            coverMaterial.SetColor(
                WaveColorId,
                waveColor
            );

            coverMaterial.SetFloat(
                WaveIntensityId,
                waveIntensity
            );
        }


        // ==========================================
        // MAXIMUM RADIUS
        //
        // UV center = 0.5, 0.5
        // UV corner distance ~= 0.707
        //
        // 0.75 guarantees that the reveal passes
        // completely beyond every corner.
        // ==========================================

        const float maxRadius = 0.75f;


        float elapsed = 0f;


        // ==========================================
        // ANIMATE EXPANDING REVEAL
        // ==========================================

        while (elapsed < finalRevealDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    finalRevealDuration
                );


            // Smooth acceleration/deceleration.
            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            float radius =
                Mathf.Lerp(
                    0f,
                    maxRadius,
                    eased
                );


            // Hidden image reveal.
            groundMaterial.SetFloat(
                FinalRevealRadiusId,
                radius
            );


            // Ground/cover removal + visible green wave.
            if (coverMaterial != null)
            {
                coverMaterial.SetFloat(
                    FinalRevealRadiusId,
                    radius
                );
            }


            yield return null;
        }


        // ==========================================
        // GUARANTEE 100% REVEAL
        // ==========================================

        groundMaterial.SetFloat(
            FinalRevealRadiusId,
            1f
        );


        if (coverMaterial != null)
        {
            coverMaterial.SetFloat(
                FinalRevealRadiusId,
                1f
            );
        }


        // ==========================================
        // REMOVE GREEN WAVE
        // ==========================================

        groundMaterial.SetFloat(
            WaveIntensityId,
            0f
        );


        if (coverMaterial != null)
        {
            coverMaterial.SetFloat(
                WaveIntensityId,
                0f
            );
        }
    }


    // =====================================================
    // RESET
    // =====================================================

    public void ResetMask()
    {
        ClearMask();

        ResetFinalRevealShader();

        if (groundMaterial != null)
        {
            groundMaterial.SetTexture(
                RevealMaskId,
                revealMask
            );
        }

        if (coverMaterial != null)
        {
            coverMaterial.SetTexture(
                RevealMaskId,
                revealMask
            );
        }
    }


    private void ResetFinalRevealShader()
    {
        if (groundMaterial == null)
            return;


        groundMaterial.SetFloat(
            FinalRevealActiveId,
            0f
        );

        groundMaterial.SetFloat(
            FinalRevealRadiusId,
            0f
        );

        groundMaterial.SetFloat(
            WaveIntensityId,
            0f
        );

        if (coverMaterial != null)
        {
            coverMaterial.SetFloat(
                FinalRevealActiveId,
                0f
            );

            coverMaterial.SetFloat(
                FinalRevealRadiusId,
                0f
            );

            coverMaterial.SetFloat(
        WaveIntensityId,
        0f
    );
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }


        ReleaseTexture(
            revealMask
        );

        ReleaseTexture(
            temporaryMask
        );
    }


    private void ReleaseTexture(
        RenderTexture texture)
    {
        if (texture == null)
            return;


        texture.Release();

        Destroy(texture);
    }
}