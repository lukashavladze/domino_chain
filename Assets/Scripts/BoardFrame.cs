
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BoardFrame : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private Renderer groundRenderer;

    [Header("Generated Frame")]
    [SerializeField] private MeshFilter frameMeshFilter;
    [SerializeField] private MeshRenderer frameMeshRenderer;

    [Header("Materials")]
    [SerializeField] private Material innerGold;
    [SerializeField] private Material emeraldMarble;
    [SerializeField] private Material outerGold;

    [Header("Dimensions")]
    [Min(0.01f)]
    [SerializeField] private float frameWidth = 0.33f;

    [Min(0.001f)]
    [SerializeField] private float frameHeight = 0.12f;

    [Min(0f)]
    [SerializeField] private float gap = 0.02f;

    [Header("Border Proportions")]
    [Range(0.01f, 0.4f)]
    [SerializeField] private float innerGoldRatio = 0.10f;

    [Range(0.01f, 0.4f)]
    [SerializeField] private float outerGoldRatio = 0.10f;

    [Header("Height")]
    [SerializeField] private float topHeightOffset = 0f;

    private Mesh generatedMesh;

    public void Resize()
    {
        if (groundRenderer == null ||
            frameMeshFilter == null ||
            frameMeshRenderer == null)
        {
            Debug.LogWarning(
                "BoardFrame: Assign Ground and LuxuryFrame components.");
            return;
        }

        // Convert world-space ground bounds into
        // this BoardFrame object's local coordinates.
        Bounds worldBounds = groundRenderer.bounds;

        Vector3 localMin =
            transform.InverseTransformPoint(worldBounds.min);

        Vector3 localMax =
            transform.InverseTransformPoint(worldBounds.max);

        float minX = Mathf.Min(localMin.x, localMax.x);
        float maxX = Mathf.Max(localMin.x, localMax.x);
        float minZ = Mathf.Min(localMin.z, localMax.z);
        float maxZ = Mathf.Max(localMin.z, localMax.z);

        float groundTop =
            Mathf.Max(localMin.y, localMax.y);

        float innerOffset = gap;
        float innerWidth = frameWidth * innerGoldRatio;
        float outerWidth = frameWidth * outerGoldRatio;
        float marbleWidth =
            frameWidth - innerWidth - outerWidth;

        if (marbleWidth <= 0f)
        {
            Debug.LogWarning("Invalid border proportions.");
            return;
        }

        float yBottom = groundTop + topHeightOffset;
        float yTop = yBottom + frameHeight;

        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();

        var triangles = new List<int>[]
        {
            new List<int>(),
            new List<int>(),
            new List<int>()
        };

        float startX = minX - innerOffset;
        float endX = maxX + innerOffset;
        float startZ = minZ - innerOffset;
        float endZ = maxZ + innerOffset;

        // Inner gold ring.
        AddRing(
            vertices, uv, triangles[0],
            startX, endX, startZ, endZ,
            innerWidth, yBottom, yTop + 0.012f);

        // Emerald marble ring.
        float marbleInset = innerWidth;

        AddRing(
            vertices, uv, triangles[1],
            startX - marbleInset,
            endX + marbleInset,
            startZ - marbleInset,
            endZ + marbleInset,
            marbleWidth, yBottom, yTop);

        // Outer gold ring.
        float outerInset = innerWidth + marbleWidth;

        AddRing(
            vertices, uv, triangles[2],
            startX - outerInset,
            endX + outerInset,
            startZ - outerInset,
            endZ + outerInset,
            outerWidth, yBottom, yTop + 0.018f);

        Mesh mesh = new Mesh();
        mesh.name = "Luxury Board Frame";

        if (vertices.Count > 65535)
            mesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = 3;

        for (int i = 0; i < 3; i++)
            mesh.SetTriangles(triangles[i], i);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (generatedMesh != null)
        {
            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);
        }

        generatedMesh = mesh;
        frameMeshFilter.sharedMesh = generatedMesh;

        frameMeshRenderer.sharedMaterials = new Material[]
        {
            innerGold,
            emeraldMarble,
            outerGold
        };
    }

    private void AddRing(
        List<Vector3> vertices,
        List<Vector2> uv,
        List<int> triangles,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        float width,
        float bottomY,
        float topY)
    {
        float outerMinX = minX - width;
        float outerMaxX = maxX + width;
        float outerMinZ = minZ - width;
        float outerMaxZ = maxZ + width;

        Vector3[] inner =
        {
            new Vector3(minX, topY, minZ),
            new Vector3(maxX, topY, minZ),
            new Vector3(maxX, topY, maxZ),
            new Vector3(minX, topY, maxZ)
        };

        Vector3[] outer =
        {
            new Vector3(outerMinX, topY, outerMinZ),
            new Vector3(outerMaxX, topY, outerMinZ),
            new Vector3(outerMaxX, topY, outerMaxZ),
            new Vector3(outerMinX, topY, outerMaxZ)
        };

        // Top surface, four connected mitered strips.
        // Top surface - normals facing UP.
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;

            AddQuad(
                vertices, uv, triangles,
                outer[i], outer[next],
                inner[next], inner[i]);
        }

        // Outer vertical walls.
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;

            Vector3 a = outer[i];
            Vector3 b = outer[next];

            Vector3 c = new Vector3(
                b.x, bottomY, b.z);

            Vector3 d = new Vector3(
                a.x, bottomY, a.z);

            AddQuad(
                vertices, uv, triangles,
                a, b, c, d);
        }

        // Inner vertical walls.
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;

            Vector3 a = inner[next];
            Vector3 b = inner[i];

            Vector3 c = new Vector3(
                b.x, bottomY, b.z);

            Vector3 d = new Vector3(
                a.x, bottomY, a.z);

            AddQuad(
                vertices, uv, triangles,
                a, b, c, d);
        }
    }

    private void AddQuad(
        List<Vector3> vertices,
        List<Vector2> uv,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int start = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        // World-sized UV coordinates for consistent
        // marble texture scale.
        uv.Add(new Vector2(a.x, a.z));
        uv.Add(new Vector2(b.x, b.z));
        uv.Add(new Vector2(c.x, c.z));
        uv.Add(new Vector2(d.x, d.z));

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }

    [ContextMenu("Resize Board Frame")]
    private void ResizeFromInspector()
    {
        Resize();
    }
}
