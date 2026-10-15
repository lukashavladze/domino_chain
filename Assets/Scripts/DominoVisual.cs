
using UnityEngine;

public class DominoVisual : MonoBehaviour
{
    [SerializeField] private Renderer visualRenderer;

    public void ApplyMaterials(Material[] materials)
    {
        if (visualRenderer == null ||
            materials == null ||
            materials.Length == 0)
            return;

        // The array must match the mesh's material slots.
        if (materials.Length !=
            visualRenderer.sharedMaterials.Length)
        {
            Debug.LogWarning(
                $"Material slot mismatch on {name}");
            return;
        }

        visualRenderer.sharedMaterials = materials;
    }
}
