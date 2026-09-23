using System.Collections;
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


    [Header("Mistake Feedback")]
    [SerializeField] private float pulseScale = 1.3f;
    [SerializeField] private float pulseDuration = 0.15f;




    private Coroutine feedbackCoroutine;

    [SerializeField] private Button continueButton;

    // ==========================================
    // LIVES
    // ==========================================

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
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
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
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (continueButton != null)
            continueButton.interactable = canContinue;
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