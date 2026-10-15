
using System.Collections.Generic;
using UnityEngine;

public class DominoSkinManager : MonoBehaviour
{
    public static DominoSkinManager Instance
    {
        get;
        private set;
    }

    [SerializeField] private DominoSkin defaultSkin;
    [SerializeField] private DominoSkin[] availableSkins;

    private const string EquippedKey =
        "EquippedDominoSkinId";

    public DominoSkin CurrentSkin { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        string savedId = PlayerPrefs.GetString(
            EquippedKey,
            defaultSkin != null ? defaultSkin.skinId : "");

        CurrentSkin = FindSkin(savedId);

        if (CurrentSkin == null)
            CurrentSkin = defaultSkin;
    }

    private DominoSkin FindSkin(string id)
    {
        if (defaultSkin != null &&
            defaultSkin.skinId == id)
            return defaultSkin;

        foreach (DominoSkin skin in availableSkins)
        {
            if (skin != null && skin.skinId == id)
                return skin;
        }

        return null;
    }

    public void ApplyToLine(DominoLine line)
    {
        if (line == null || CurrentSkin == null)
            return;

        // Supports both newly generated and saved levels.
        Domino[] dominoes =
            line.GetComponentsInChildren<Domino>(true);

        foreach (Domino domino in dominoes)
        {
            DominoVisual visual =
                domino.GetComponent<DominoVisual>();

            if (visual == null)
                continue;

            bool isFirst = domino == line.firstDomino;

            visual.ApplyMaterials(
                isFirst
                    ? CurrentSkin.firstDominoMaterials
                    : CurrentSkin.normalMaterials);
        }
    }

    public void ApplyToLevel(GameObject levelRoot)
    {
        if (levelRoot == null)
            return;

        DominoLine[] lines =
            levelRoot.GetComponentsInChildren<DominoLine>(true);

        foreach (DominoLine line in lines)
            ApplyToLine(line);
    }

    public bool EquipSkin(string skinId)
    {
        DominoSkin skin = FindSkin(skinId);

        if (skin == null)
            return false;

        // Later: verify ownership before equipping.
        CurrentSkin = skin;

        PlayerPrefs.SetString(EquippedKey, skinId);
        PlayerPrefs.Save();

        // Update the currently loaded level immediately.
        foreach (DominoLine line in
                 FindObjectsOfType<DominoLine>())
        {
            ApplyToLine(line);
        }

        return true;
    }
}
