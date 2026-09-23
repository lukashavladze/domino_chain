using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lives")]
    [SerializeField] private int maximumLives = 3;

    [Header("References")]
    [SerializeField] private GameUI gameUI;

    private int currentLives;

    private bool gameOver;

    public int CurrentLives => currentLives;
    public int MaximumLives => maximumLives;
    public bool IsGameOver => gameOver;

    private bool continueUsed;

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

        if (gameUI != null)
        {
            gameUI.SetLives(currentLives);
            gameUI.HideGameOver();
        }
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