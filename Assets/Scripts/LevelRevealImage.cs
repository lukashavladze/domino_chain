
using UnityEngine;

public class LevelRevealImage : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Renderer revealRenderer;

    [Header("Shader")]
    [SerializeField]
    private string textureProperty = "_BaseMap";

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (revealRenderer == null)
        {
            Debug.LogError(
                "LevelRevealImage: Reveal Renderer is missing!"
            );
        }
    }

    public void SetImage(Texture texture)
    {
        if (revealRenderer == null || texture == null)
        {
            Debug.LogWarning(
                "LevelRevealImage: Cannot assign image."
            );
            return;
        }

        Material material = revealRenderer.sharedMaterial;

        if (material == null ||
            !material.HasProperty(textureProperty))
        {
            Debug.LogError(
                $"Shader property {textureProperty} not found."
            );
            return;
        }

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        revealRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetTexture(
            textureProperty,
            texture
        );

        revealRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Reveal image assigned: {texture.name}");
    }

    public void LoadLevelImage(int levelNumber)
    {
        string path =
            $"level_images/Level_{levelNumber:0000}";

        Texture2D image =
            Resources.Load<Texture2D>(path);

        if (image == null)
        {
            Debug.LogWarning(
                $"Level image missing: {path}"
            );
            return;
        }

        SetImage(image);
    }
}
