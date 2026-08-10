using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RevealWave : MonoBehaviour
{
    [Header("Wave")]
    public float startRadius = 0.03f;
    public float endRadius = 0.35f;
    public float duration = 0.35f;

    [Header("Visual")]
    public Color waveColor =
        new Color(0.15f, 0.75f, 1f, 0.45f);

    [Range(16, 64)]
    public int segments = 40;

    [Header("Reveal")]
    public float revealInterval = 0.025f;

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    private float timer;
    private float revealTimer;

    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        propertyBlock =
            new MaterialPropertyBlock();

        CreateCircleMesh();

        UpdateRadius(startRadius);

        SetColor(waveColor);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        revealTimer += Time.deltaTime;

        float t = Mathf.Clamp01(
            timer / duration
        );

        float radius = Mathf.Lerp(
            startRadius,
            endRadius,
            t
        );

        // Expand filled visual wave.
        UpdateRadius(radius);

        // Reveal image underneath.
        if (revealTimer >= revealInterval)
        {
            revealTimer = 0f;

            PaintReveal(radius);
        }

        // Fade filled blue wave.
        Color currentColor = waveColor;

        currentColor.a =
            Mathf.Lerp(
                waveColor.a,
                0f,
                t
            );

        SetColor(currentColor);

        if (t >= 1f)
        {
            PaintReveal(endRadius);

            Destroy(gameObject);
        }
    }

    private void PaintReveal(float radius)
    {
        if (RevealPainter.Instance == null)
            return;

        RevealPainter.Instance.PaintCircle(
            transform.position,
            radius
        );
    }

    private void CreateCircleMesh()
    {
        mesh = new Mesh();

        Vector3[] vertices =
            new Vector3[segments + 1];

        int[] triangles =
            new int[segments * 3];

        // Center
        vertices[0] = Vector3.zero;

        // Outer circle
        for (int i = 0; i < segments; i++)
        {
            float angle =
                (i / (float)segments) *
                Mathf.PI *
                2f;

            vertices[i + 1] =
                new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)
                );
        }

        // Triangles
        for (int i = 0; i < segments; i++)
        {
            int next =
                (i + 1) % segments;

            int triangleIndex =
                i * 3;

            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] =
                i + 1;
            triangles[triangleIndex + 2] =
                next + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh =
            mesh;
    }

    private void UpdateRadius(float radius)
    {
        transform.localScale =
            new Vector3(
                radius,
                1f,
                radius
            );
    }

    private void SetColor(Color color)
    {
        meshRenderer.GetPropertyBlock(
            propertyBlock
        );

        propertyBlock.SetColor(
            BaseColorId,
            color
        );

        meshRenderer.SetPropertyBlock(
            propertyBlock
        );
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }
    }
}