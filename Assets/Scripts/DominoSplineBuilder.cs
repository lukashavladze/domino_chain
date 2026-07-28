using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class DominoSplineBuilder : MonoBehaviour
{
    public SplineContainer spline;

    public Domino dominoPrefab;
    public CoverTile coverTilePrefab;

    public Vector3 rotationOffset = new Vector3(0, 90, 0);

    public float spacing = 0.25f;
    public float groundOffset = 0.5f;
    public float coverHeight = 0.01f;

    public void Build()
    {
        // Remove previously generated objects
        while (transform.childCount > 1)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(1).gameObject);
#else
            Destroy(transform.GetChild(1).gameObject);
#endif
        }

        float length = spline.CalculateLength();
        int count = Mathf.Max(1, Mathf.FloorToInt(length / spacing));

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;

            spline.Evaluate(
                t,
                out float3 position,
                out float3 tangent,
                out float3 up);

#if UNITY_EDITOR
            Domino domino =
                (Domino)UnityEditor.PrefabUtility.InstantiatePrefab(dominoPrefab);

            CoverTile tile =
                (CoverTile)UnityEditor.PrefabUtility.InstantiatePrefab(coverTilePrefab);
#else
            Domino domino = Instantiate(dominoPrefab);
            CoverTile tile = Instantiate(coverTilePrefab);
#endif

            domino.transform.SetParent(transform);
            tile.transform.SetParent(transform);

            domino.transform.position =
                new Vector3(position.x,
                            position.y + groundOffset,
                            position.z);

            tile.transform.position =
                new Vector3(position.x,
                            coverHeight,
                            position.z);

            tile.transform.rotation = Quaternion.identity;

            Quaternion rot =
                Quaternion.LookRotation(tangent, up);

            rot *= Quaternion.Euler(rotationOffset);

            domino.transform.rotation = rot;

            domino.coverTile = tile;
        }

        DominoLine line = GetComponent<DominoLine>();

        if (line != null)
        {
            line.AutoConnect();
        }
    }
}