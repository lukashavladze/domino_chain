
using UnityEngine;

public class DominoSkinTester : MonoBehaviour
{
    [ContextMenu("Equip Default Skin")]
    public void EquipDefault()
    {
        DominoSkinManager.Instance?.EquipSkin(
            "domino_default");
    }

    [ContextMenu("Equip Emerald Skin")]
    public void EquipEmerald()
    {
        DominoSkinManager.Instance?.EquipSkin(
            "domino_emerald_1");
    }
}
