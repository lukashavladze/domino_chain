
using UnityEngine;

public class DominoVisual : MonoBehaviour
{
    [SerializeField] private Renderer visualRenderer;

    public void ApplyMaterials(Material[] materials)
    {
        if (visualRenderer == null)
        {
            Debug.LogError(
                $"Visual Renderer missing on {name}");
            return;
        }

        if (materials == null || materials.Length == 0)
            return;

        int meshSlots = 0;

        if (visualRenderer is MeshRenderer meshRenderer)
        {
            MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();

            if (filter != null && filter.sharedMesh != null)
                meshSlots = filter.sharedMesh.subMeshCount;
        }

        Debug.Log(
            $"{name}: Skin materials={materials.Length}, " +
            $"Renderer materials={visualRenderer.sharedMaterials.Length}, " +
            $"Mesh submeshes={meshSlots}");

        if (meshSlots > 0 && materials.Length != meshSlots)
        {
            Debug.LogError(
                $"{name}: Skin has {materials.Length} materials, " +
                $"but mesh has {meshSlots} submeshes.");
            return;
        }

        visualRenderer.sharedMaterials = materials;
    }
}
