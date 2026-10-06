using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    // =========================================================
    // LIVES
    // =========================================================

    [Header("Lives")]
    [SerializeField]
    private int maximumLives = 3;


    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField]
    private GameUI gameUI;

    [SerializeField]
    private CameraShake cameraShake;


    // =========================================================
    // LEVEL
    // =========================================================

    [Header("Level")]
    [SerializeField]
    private int currentLevel = 1;

    private const string SavedLevelKey = "SavedLevel";


    // =========================================================
    // RUNTIME STATE
    // =========================================================

    private int currentLives;

    private bool gameOver;
    private bool continueUsed;

    private bool levelCompleted;

    private Coroutine completionCoroutine;


    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    public int CurrentLives => currentLives;

    public int MaximumLives => maximumLives;

    public bool IsGameOver => gameOver;

    public bool ContinueUsed => continueUsed;

    public bool IsLevelCompleted => levelCompleted;

    public int CurrentLevel => currentLevel;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadSavedLevel();
    }


    private void Start()
    {
        LoadCurrentLevel();
    }


    private void Update()
    {
#if UNITY_EDITOR

        // R = restart current level
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }


        // P = completely reset progress to Level 1
        if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            ResetSavedProgress();
        }

#endif
    }


    // =========================================================
    // SAVED PROGRESS
    // =========================================================

    private void LoadSavedLevel()
    {
        currentLevel =
            PlayerPrefs.GetInt(
                SavedLevelKey,
                1
            );

        Debug.Log(
            $"LOADED SAVED LEVEL: {currentLevel}"
        );
    }


    private void SaveLevel()
    {
        PlayerPrefs.SetInt(
            SavedLevelKey,
            currentLevel
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"SAVED LEVEL: {currentLevel}"
        );
    }


    public void ResetSavedProgress()
    {
        // Delete saved progress.
        PlayerPrefs.DeleteKey(
            SavedLevelKey
        );

        PlayerPrefs.Save();


        // Return to level 1.
        currentLevel = 1;


        // Reset reveal.
        if (RevealPainter.Instance != null)
        {
            RevealPainter.Instance.ResetMask();
        }


        // Load Level 1 prefab.
        if (LevelLoader.Instance != null)
        {
            bool loaded =
                LevelLoader.Instance.LoadLevel(
                    currentLevel
                );

            if (!loaded)
            {
                Debug.LogError(
                    "FAILED TO LOAD LEVEL 1"
                );

                return;
            }
        }


        // Reset lives / Game Over / UI / etc.
        StartLevel();


        Debug.Log(
            "PROGRESS RESET TO LEVEL 1"
        );
    }


    // =========================================================
    // LOAD CURRENT LEVEL
    // =========================================================

    private void LoadCurrentLevel()
    {
        if (LevelLoader.Instance == null)
        {
            Debug.LogError(
                "LevelLoader.Instance is NULL."
            );

            return;
        }


        // Reset reveal mask before starting
        // the new level.
        if (RevealPainter.Instance != null)
        {
            RevealPainter.Instance.ResetMask();
        }


        bool loaded =
            LevelLoader.Instance.LoadLevel(
                currentLevel
            );


        if (!loaded)
        {
            Debug.LogError(
                $"FAILED TO LOAD LEVEL {currentLevel}"
            );

            return;
        }


        StartLevel();


        Debug.Log(
            $"LEVEL {currentLevel} STARTED"
        );
    }


    // =========================================================
    // START / RESET LEVEL STATE
    // =========================================================

    private void StartLevel()
    {
        currentLives =
            maximumLives;

        gameOver = false;

        continueUsed = false;

        levelCompleted = false;


        if (completionCoroutine != null)
        {
            StopCoroutine(
                completionCoroutine
            );

            completionCoroutine = null;
        }


        if (gameUI != null)
        {
            gameUI.SetLives(
                currentLives
            );

            gameUI.HideGameOver();

            gameUI.SetLevel(
                currentLevel
            );
        }
    }


    // =========================================================
    // LEVEL COMPLETION CHECK
    // =========================================================

    public void CheckLevelCompletion()
    {
        if (gameOver ||
            levelCompleted)
        {
            return;
        }


        DominoLine[] lines =
            FindObjectsByType<DominoLine>(
                FindObjectsSortMode.None
            );


        if (lines == null ||
            lines.Length == 0)
        {
            return;
        }


        foreach (DominoLine line in lines)
        {
            if (line == null)
                continue;


            if (!line.HasStartedLine)
            {
                return;
            }
        }


        // Every line was successfully started.
        levelCompleted = true;


        completionCoroutine =
            StartCoroutine(
                LevelCompleteSequence()
            );
    }


    // =========================================================
    // LEVEL COMPLETE SEQUENCE
    // =========================================================

    private IEnumerator LevelCompleteSequence()
    {
        Debug.Log(
            $"LEVEL {currentLevel} COMPLETED"
        );


        // Give the final dominoes time
        // to finish falling.
        yield return new WaitForSeconds(
            2.0f
        );


        // Final radial reveal.
        if (RevealPainter.Instance != null)
        {
            yield return StartCoroutine(
                RevealPainter.Instance
                    .RevealAllRadial()
            );
        }


        // Let player see completed picture.
        yield return new WaitForSeconds(
            1.0f
        );


        // Important:
        // clear this BEFORE loading next level.
        completionCoroutine = null;


        StartNextLevel();
    }


    // =========================================================
    // NEXT LEVEL
    // =========================================================

    private void StartNextLevel()
    {
        int nextLevel =
            currentLevel + 1;


        if (LevelLoader.Instance == null)
        {
            Debug.LogError(
                "LevelLoader.Instance is NULL."
            );

            return;
        }


        Debug.Log(
            $"TRYING TO LOAD LEVEL {nextLevel}"
        );


        // Reset reveal before new level.
        if (RevealPainter.Instance != null)
        {
            RevealPainter.Instance.ResetMask();
        }


        // Try loading next prefab FIRST.
        //
        // We don't save progress until we know
        // the next prefab actually exists.
        bool loaded =
            LevelLoader.Instance.LoadLevel(
                nextLevel
            );


        if (!loaded)
        {
            Debug.LogWarning(
                $"LEVEL {nextLevel} DOES NOT EXIST YET."
            );

            return;
        }


        // Loading succeeded.
        currentLevel =
            nextLevel;


        // Save this as the new level
        // the player should start from.
        SaveLevel();


        // Reset lives / game state.
        StartLevel();


        Debug.Log(
            $"STARTED LEVEL {currentLevel}"
        );
    }


    // =========================================================
    // WRONG MOVE
    // =========================================================

    public void RegisterWrongMove()
    {
        if (gameOver)
            return;


        if (levelCompleted)
            return;


        if (currentLives <= 0)
            return;


        // Index of the heart
        // that is about to disappear.
        int lostLifeIndex =
            currentLives - 1;


        currentLives--;


        // Camera shake.
        if (cameraShake != null)
        {
            cameraShake.Shake();
        }


        Debug.Log(
            $"WRONG MOVE | Lives remaining: {currentLives}"
        );


        if (gameUI != null)
        {
            gameUI.SetLives(
                currentLives
            );

            gameUI.PlayMistakeFeedback(
                lostLifeIndex
            );
        }


        if (currentLives <= 0)
        {
            GameOver();
        }
    }


    // =========================================================
    // GAME OVER
    // =========================================================

    private void GameOver()
    {
        if (gameOver)
            return;


        gameOver = true;


        Debug.Log(
            "GAME OVER"
        );


        if (gameUI != null)
        {
            gameUI.ShowGameOver(
                !continueUsed
            );
        }
    }


    // =========================================================
    // CONTINUE
    // =========================================================

    public void ContinueGame()
    {
        if (!gameOver)
            return;


        if (continueUsed)
            return;


        continueUsed = true;


        // Continue with exactly one life.
        currentLives = 1;

        gameOver = false;


        if (gameUI != null)
        {
            gameUI.SetLives(
                currentLives
            );

            gameUI.HideGameOver();
        }


        Debug.Log(
            "CONTINUED | Lives remaining: 1"
        );
    }


    // =========================================================
    // RESTART CURRENT LEVEL
    // =========================================================

    public void RestartLevel()
    {
        // Don't allow restart during
        // level-complete transition.
        if (levelCompleted)
            return;


        DominoLine[] lines =
            FindObjectsByType<DominoLine>(
                FindObjectsSortMode.None
            );


        foreach (DominoLine line in lines)
        {
            if (line != null)
            {
                line.ResetLine();
            }
        }


        if (RevealPainter.Instance != null)
        {
            RevealPainter.Instance.ResetMask();
        }


        StartLevel();


        Debug.Log(
            $"LEVEL {currentLevel} RESTARTED"
        );
    }
}