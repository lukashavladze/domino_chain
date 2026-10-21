using UnityEngine;

public class LevelRevealImage : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Renderer revealRenderer;

    private static readonly int MainTextureID =
        Shader.PropertyToID("_MainTexture");

    private Material runtimeMaterial;

    private void Awake()
    {
        if (revealRenderer == null)
        {
            Debug.LogError(
                "LevelRevealImage: Reveal Renderer missing!"
            );

            return;
        }

        runtimeMaterial = revealRenderer.material;
    }

    public void SetImage(Texture texture)
    {
        if (revealRenderer == null || texture == null)
        {
            Debug.LogError(
                "LevelRevealImage: Renderer or texture missing."
            );

            return;
        }

        if (runtimeMaterial == null)
        {
            runtimeMaterial = revealRenderer.material;
        }

        if (!runtimeMaterial.HasProperty(MainTextureID))
        {
            Debug.LogError(
                "LevelRevealImage: _MainTexture not found!"
            );

            return;
        }

        runtimeMaterial.SetTexture(
            MainTextureID,
            texture
        );

        Debug.Log(
            $"REVEAL IMAGE ASSIGNED: {texture.name} | " +
            $"Material: {runtimeMaterial.name} | " +
            $"Shader: {runtimeMaterial.shader.name}"
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
            Debug.LogError(
                $"LEVEL IMAGE NOT FOUND: {path}"
            );

            return;
        }

        SetImage(image);
    }
}