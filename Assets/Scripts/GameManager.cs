using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lives")]
    [SerializeField] private int maximumLives = 3;

    [Header("References")]
    [SerializeField] private GameUI gameUI;
    [SerializeField] private CameraShake cameraShake;

    private int currentLives;

    private bool gameOver;

    public int CurrentLives => currentLives;
    public int MaximumLives => maximumLives;
    public bool IsGameOver => gameOver;

    private bool continueUsed;

    private bool levelCompleted;
    private Coroutine completionCoroutine;

    public bool IsLevelCompleted => levelCompleted;



    public bool ContinueUsed => continueUsed;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        StartLevel();
    }


    private void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
#endif
    }


    private void StartLevel()
    {
        currentLives = maximumLives;

        gameOver = false;
        continueUsed = false;
        levelCompleted = false;

        if (completionCoroutine != null)
        {
            StopCoroutine(completionCoroutine);
            completionCoroutine = null;
        }

        if (gameUI != null)
        {
            gameUI.SetLives(currentLives);
            gameUI.HideGameOver();
        }
    }

    public void CheckLevelCompletion()
    {
        if (gameOver || levelCompleted)
            return;

        DominoLine[] lines =
            FindObjectsByType<DominoLine>(
                FindObjectsSortMode.None
            );

        if (lines == null || lines.Length == 0)
            return;

        foreach (DominoLine line in lines)
        {
            if (line == null)
                continue;

            if (!line.HasStartedLine)
            {
                return;
            }
        }

        // Every line has been successfully started.
        levelCompleted = true;

        completionCoroutine =
            StartCoroutine(LevelCompleteSequence());
    }

    private System.Collections.IEnumerator LevelCompleteSequence()
    {
        Debug.Log("ALL LINES COMPLETED");

        // Wait for the final domino chain to finish falling
        // and for its normal reveal/fade animation.
        yield return new WaitForSeconds(2.0f);

        if (RevealPainter.Instance != null)
        {
            yield return StartCoroutine(
                RevealPainter.Instance.RevealAllRadial()
            );
        }

        Debug.Log("LEVEL COMPLETED");

        // Later:
        // gameUI.ShowLevelComplete();
        // LoadNextLevel();
    }


    public void RegisterWrongMove()
    {
        if (gameOver)
            return;

        if (currentLives <= 0)
            return;


        // Index of the heart that is about to disappear.
        int lostLifeIndex =
            currentLives - 1;


        currentLives--;

        // Camera shake when a life is lost.
        if (cameraShake != null)
        {
            cameraShake.Shake();
        }


        Debug.Log(
            $"WRONG MOVE | Lives remaining: {currentLives}"
        );


        if (gameUI != null)
        {
            gameUI.SetLives(currentLives);

            gameUI.PlayMistakeFeedback(
                lostLifeIndex
            );
        }


        if (currentLives <= 0)
        {
            GameOver();
        }
    }


    private void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;

        Debug.Log("GAME OVER");

        if (gameUI != null)
        {
            gameUI.ShowGameOver(!continueUsed);
        }
    }

    public void ContinueGame()
    {
        if (!gameOver)
            return;

        if (continueUsed)
            return;

        continueUsed = true;

        // Continue with exactly 1 life.
        currentLives = 1;
        gameOver = false;

        if (gameUI != null)
        {
            gameUI.SetLives(currentLives);
            gameUI.HideGameOver();
        }

        Debug.Log("CONTINUED | Lives remaining: 1");
    }


    public void RestartLevel()
    {
        DominoLine[] lines =
            FindObjectsByType<DominoLine>(
                FindObjectsSortMode.None
            );

        foreach (DominoLine line in lines)
        {
            if (line != null)
                line.ResetLine();
        }


        if (RevealPainter.Instance != null)
        {
            RevealPainter.Instance.ResetMask();
        }


        StartLevel();

        Debug.Log("LEVEL RESTARTED");
    }
}