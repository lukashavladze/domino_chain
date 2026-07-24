using UnityEngine;

public class DominoLineBuilder : MonoBehaviour
{
    [Header("Prefab")]
    public Domino dominoPrefab;

    [Header("Line")]
    public int count = 10;
    public float spacing = 0.25f;
    public Vector3 direction = Vector3.right;

    [Header("Placement")]
    public float groundOffset = 0.5f;

    [Header("Rotation")]
    public Vector3 rotationOffset;

    public void Build()
    {
        while (transform.childCount > 0)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(0).gameObject);
#else
            Destroy(transform.GetChild(0).gameObject);
#endif
        }

        Vector3 dir = direction.normalized;

        for (int i = 0; i < count; i++)
        {
#if UNITY_EDITOR
            Domino domino =
                (Domino)UnityEditor.PrefabUtility.InstantiatePrefab(dominoPrefab);
#else
            Domino domino = Instantiate(dominoPrefab);
#endif

            domino.transform.SetParent(transform);

            Vector3 pos = dir * spacing * i;
            pos.y = groundOffset;

            domino.transform.localPosition = pos;

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            rot *= Quaternion.Euler(rotationOffset);

            domino.transform.localRotation = rot;
        }
    }

    public void Align()
    {
        Vector3 dir = direction.normalized;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            Vector3 pos = dir * spacing * i;
            pos.y = groundOffset;

            child.localPosition = pos;

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            rot *= Quaternion.Euler(rotationOffset);

            child.localRotation = rot;
        }
    }
}