using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }


    [Header("References")]
    [SerializeField]
    private ProceduralLevelGenerator generator;


    private GameObject currentLevelObject;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    //private void Start()
    //{
    //    LoadLevel(1);
    //}


    // =========================================================
    // LOAD LEVEL
    // =========================================================

    public bool LoadLevel(int levelNumber)
    {
        // -----------------------------------------
        // Find prefab
        // -----------------------------------------

        string resourcePath =
            $"Levels/Level_{levelNumber:0000}";


        GameObject levelPrefab =
            Resources.Load<GameObject>(
                resourcePath
            );


        if (levelPrefab == null)
        {
            Debug.LogError(
                $"LEVEL PREFAB NOT FOUND: " +
                $"{resourcePath}"
            );

            return false;
        }


        // -----------------------------------------
        // Remove previous level
        // -----------------------------------------

        UnloadCurrentLevel();


        // -----------------------------------------
        // Read saved level information
        // -----------------------------------------

        SavedLevelData levelData =
            levelPrefab.GetComponent<SavedLevelData>();


        if (levelData == null)
        {
            Debug.LogError(
                $"Level {levelNumber} has no " +
                $"SavedLevelData component."
            );

            return false;
        }


        // -----------------------------------------
        // Resize board
        // -----------------------------------------

        if (generator != null)
        {
            generator.SetGroundSize(
                levelData.groundSize
            );
        }


        // -----------------------------------------
        // Instantiate saved level
        // -----------------------------------------

        currentLevelObject =
            Instantiate(
                levelPrefab,
                generator.transform
            );

        // =====================================
        // LOAD IMAGE FOR THIS LEVEL
        // =====================================

        LevelRevealImage revealImage =
            FindFirstObjectByType<LevelRevealImage>();

        if (revealImage != null)
        {
            revealImage.LoadLevelImage(levelNumber);
        }
        else
        {
            Debug.LogWarning(
                "LevelRevealImage component not found."
            );
        }


        currentLevelObject.name =
            $"Level_{levelNumber:0000}";


        // -----------------------------------------
        // Make sure DominoLine connections
        // are initialized correctly.
        // -----------------------------------------

        DominoLine[] lines =
            currentLevelObject.GetComponentsInChildren
                <DominoLine>(true);


        foreach (DominoLine line in lines)
        {
            if (line != null)
            {
                line.AutoConnect();
                //line.RefreshAvailability();
            }
        }


        Debug.Log(
            $"LOADED LEVEL {levelNumber} | " +
            $"Lines: {lines.Length}"
        );


        return true;
    }


    // =========================================================
    // UNLOAD
    // =========================================================

    public void UnloadCurrentLevel()
    {
        if (currentLevelObject == null)
            return;


        Destroy(currentLevelObject);

        currentLevelObject = null;
    }
}