
using UnityEngine;

public class MeshNormalVisualizer : MonoBehaviour
{
    [ContextMenu("Analyze Mesh Normals")]
    public void AnalyzeNormals()
    {
        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter == null || filter.sharedMesh == null)
        {
            Debug.LogError("No mesh found.");
            return;
        }

        Mesh mesh = filter.sharedMesh;
        Vector3[] normals = mesh.normals;

        if (normals.Length == 0)
        {
            Debug.LogError("Mesh has no normals!");
            return;
        }

        Vector3 average = Vector3.zero;
        int invalid = 0;

        foreach (Vector3 normal in normals)
        {
            average += normal;

            if (normal.sqrMagnitude < 0.5f)
                invalid++;
        }

        average /= normals.Length;

        Debug.Log(
            $"Vertices: {mesh.vertexCount}\n" +
            $"Normals: {normals.Length}\n" +
            $"Average Normal: {average}\n" +
            $"Invalid/Short Normals: {invalid}\n" +
            $"Submeshes: {mesh.subMeshCount}"
        );
    }
}
