using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Lives")]
    [SerializeField] private Image[] lifeImages;

    [SerializeField] private Sprite fullLifeSprite;
    [SerializeField] private Sprite emptyLifeSprite;


    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private RectTransform gameOverPopup;

    [Header("Level")]
    [SerializeField] private TMP_Text levelText;


    [Header("Mistake Feedback")]
    [SerializeField] private float pulseScale = 1.3f;
    [SerializeField] private float pulseDuration = 0.15f;

    [Header("Level Complete")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private RectTransform levelCompletePopup;

    private Coroutine levelCompleteCoroutine;

    [SerializeField] private ParticleSystem levelCompleteSparkles;


    private Coroutine feedbackCoroutine;

    [SerializeField] private Button continueButton;

    [Header("Game Over Animation")]
    [SerializeField] private float gameOverDuration = 0.65f;
    [SerializeField] private float gameOverDropDistance = 450f;
    [SerializeField] private float gameOverStartScale = 0.85f;
    [SerializeField] private float gameOverOvershoot = 1.08f;

    private Coroutine gameOverCoroutine;
    private RectTransform gameOverRect;
    private CanvasGroup gameOverCanvasGroup;
    private Vector2 gameOverOriginalPosition;
    private Vector3 gameOverOriginalScale;

    // ==========================================
    // LIVES
    // ==========================================


    private void Awake()
    {
        if (gameOverPanel == null || gameOverPopup == null)
        {
            Debug.LogError(
                "Game Over references are not assigned!"
            );
            return;
        }

        // Only animate the popup.
        gameOverRect = gameOverPopup;

        gameOverCanvasGroup =
            gameOverPopup.GetComponent<CanvasGroup>();

        if (gameOverCanvasGroup == null)
        {
            gameOverCanvasGroup =
                gameOverPopup.gameObject.AddComponent<CanvasGroup>();
        }

        gameOverOriginalPosition =
            gameOverRect.anchoredPosition;

        gameOverOriginalScale =
            gameOverRect.localScale;
    }

    public void ShowLevelComplete()
    {
        if (levelCompletePanel == null ||
            levelCompletePopup == null)
            return;

        if (levelCompleteCoroutine != null)
            StopCoroutine(levelCompleteCoroutine);

        levelCompletePanel.SetActive(true);

        levelCompleteCoroutine =
            StartCoroutine(AnimateLevelComplete());

        StartCoroutine(PlayLevelCompleteParticles());
    }

    private IEnumerator PlayLevelCompleteParticles()
    {
        // Wait for the popup to expand.
        yield return new WaitForSecondsRealtime(0.15f);

        if (levelCompleteSparkles == null)
        {
            Debug.LogError("LevelCompleteSparkles not assigned!");
            yield break;
        }

        levelCompleteSparkles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        levelCompleteSparkles.Play(true);

        Debug.Log("LEVEL COMPLETE PARTICLES TRIGGERED!");
    }

    public void HideLevelComplete()
    {
        if (levelCompleteCoroutine != null)
        {
            StopCoroutine(levelCompleteCoroutine);
            levelCompleteCoroutine = null;
        }

        if (levelCompletePopup != null)
            levelCompletePopup.localScale = Vector3.one;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    private IEnumerator AnimateLevelComplete()
    {
        float duration = 0.32f;
        float elapsed = 0f;

        Vector3 originalScale = Vector3.one;

        levelCompletePopup.localScale =
            originalScale * 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float scale;

            if (t < 0.5f)
            {
                float p = t / 0.5f;
                p = 1f - Mathf.Pow(1f - p, 3f);

                scale = Mathf.Lerp(0.4f, 1.12f, p);
            }
            else
            {
                float p = (t - 0.5f) / 0.5f;
                p = p * p * (3f - 2f * p);

                scale = Mathf.Lerp(1.12f, 1f, p);
            }

            levelCompletePopup.localScale =
                originalScale * scale;

            yield return null;
        }

        levelCompletePopup.localScale = originalScale;
        levelCompleteCoroutine = null;
    }

    public void SetLives(int currentLives)
    {
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null)
                continue;

            lifeImages[i].sprite =
                i < currentLives
                    ? fullLifeSprite
                    : emptyLifeSprite;
        }
    }

    // ==========================================
    // LEVEL
    // ==========================================

    public void SetLevel(int level)
    {
        if (levelText == null)
            return;

        levelText.text = $"LEVEL {level}";
    }

    public void PlayMistakeFeedback(int lostLifeIndex)
    {
        if (lostLifeIndex < 0 ||
            lostLifeIndex >= lifeImages.Length)
        {
            return;
        }

        Image target =
            lifeImages[lostLifeIndex];

        if (target == null)
            return;


        if (feedbackCoroutine != null)
        {
            StopCoroutine(
                feedbackCoroutine
            );
        }


        feedbackCoroutine =
            StartCoroutine(
                PulseLife(
                    target.rectTransform
                )
            );
    }


    // ==========================================
    // GAME OVER
    // ==========================================

    public void HideGameOver()
    {
        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }

        if (gameOverPanel == null)
            return;

        if (gameOverRect != null)
        {
            gameOverRect.anchoredPosition =
                gameOverOriginalPosition;

            gameOverRect.localScale =
                gameOverOriginalScale;
        }

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 1f;
            gameOverCanvasGroup.interactable = true;
            gameOverCanvasGroup.blocksRaycasts = true;
        }

        gameOverPanel.SetActive(false);
    }


    // ==========================================
    // BUTTONS
    // ==========================================

    public void OnContinuePressed()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.ContinueGame();
    }


    public void OnRestartPressed()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.RestartLevel();
    }

    public void ShowGameOver(bool canContinue)
    {
        if (gameOverPanel == null)
            return;

        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }

        gameOverPanel.SetActive(true);

        if (continueButton != null)
            continueButton.interactable = canContinue;

        gameOverCoroutine =
            StartCoroutine(AnimateGameOver());
    }

    private IEnumerator AnimateGameOver()
    {
        float duration = 0.35f;
        float elapsed = 0f;

        // Always appear at center.
        gameOverRect.anchoredPosition =
            gameOverOriginalPosition;

        // Start small.
        gameOverRect.localScale =
            gameOverOriginalScale * 0.4f;

        gameOverCanvasGroup.alpha = 0f;
        gameOverCanvasGroup.interactable = false;
        gameOverCanvasGroup.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float scale;

            if (t < 0.4f)
            {
                // Fast expansion.
                float p = t / 0.4f;

                p = 1f - Mathf.Pow(1f - p, 3f);

                scale = Mathf.Lerp(0.4f, 1.15f, p);
            }
            else if (t < 0.7f)
            {
                // Small compression.
                float p = (t - 0.4f) / 0.3f;

                p = p * p * (3f - 2f * p);

                scale = Mathf.Lerp(1.15f, 0.95f, p);
            }
            else
            {
                // Final settle.
                float p = (t - 0.7f) / 0.3f;

                p = p * p * (3f - 2f * p);

                scale = Mathf.Lerp(0.95f, 1f, p);
            }

            gameOverRect.localScale =
                gameOverOriginalScale * scale;

            // Fast fade.
            gameOverCanvasGroup.alpha =
                Mathf.Clamp01(t * 8f);

            yield return null;
        }

        gameOverRect.localScale =
            gameOverOriginalScale;

        gameOverCanvasGroup.alpha = 1f;
        gameOverCanvasGroup.interactable = true;
        gameOverCanvasGroup.blocksRaycasts = true;

        gameOverCoroutine = null;
    }


    // ==========================================
    // HEART ANIMATION
    // ==========================================

    private IEnumerator PulseLife(
        RectTransform target
    )
    {
        Vector3 originalScale =
            Vector3.one;

        Vector3 enlargedScale =
            Vector3.one * pulseScale;


        float halfDuration =
            pulseDuration * 0.5f;


        float timer = 0f;


        while (timer < halfDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / halfDuration
                );


            target.localScale =
                Vector3.Lerp(
                    originalScale,
                    enlargedScale,
                    t
                );


            yield return null;
        }


        timer = 0f;


        while (timer < halfDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / halfDuration
                );


            target.localScale =
                Vector3.Lerp(
                    enlargedScale,
                    originalScale,
                    t
                );


            yield return null;
        }


        target.localScale =
            originalScale;

        feedbackCoroutine = null;
    }
}