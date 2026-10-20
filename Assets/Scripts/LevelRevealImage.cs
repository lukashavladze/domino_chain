
using UnityEngine;

public class LevelRevealImage : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Renderer revealRenderer;

    private static readonly int MainTextureID =
        Shader.PropertyToID("_MainTexture");

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
            !material.HasProperty(MainTextureID))
        {
            Debug.LogError(
                "LevelRevealImage: _MainTexture not found. " +
                "Check RevealImage material and renderer."
            );
            return;
        }

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        revealRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetTexture(
            MainTextureID,
            texture
        );

        revealRenderer.SetPropertyBlock(propertyBlock);
        MaterialPropertyBlock checkBlock =
    new MaterialPropertyBlock();

        revealRenderer.GetPropertyBlock(checkBlock);

        Texture assignedTexture =
            checkBlock.GetTexture(MainTextureID);

        Debug.Log(
            $"Renderer: {revealRenderer.name}, " +
            $"Shader: {revealRenderer.sharedMaterial.shader.name}, " +
            $"Assigned texture: {assignedTexture?.name ?? "NULL"}"
        );

        Debug.Log(
            $"Reveal image assigned: {texture.name}"
        );
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
