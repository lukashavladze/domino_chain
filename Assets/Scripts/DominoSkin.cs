
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDominoSkin",
    menuName = "Domino Game/Skins/Domino Skin")]
public class DominoSkin : ScriptableObject
{
    public string skinId = "domino_default";
    public string displayName = "Default";
    public Sprite shopIcon;

    [Header("Materials")]
    public Material[] normalMaterials;
    public Material[] firstDominoMaterials;
}
