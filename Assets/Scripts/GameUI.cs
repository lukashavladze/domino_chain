using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Lives")]
    [SerializeField] private Image[] lifeImages;

    [SerializeField] private Sprite fullLifeSprite;
    [SerializeField] private Sprite emptyLifeSprite;

    [Header("Mistake Feedback")]
    [SerializeField] private float pulseScale = 1.3f;
    [SerializeField] private float pulseDuration = 0.15f;

    private Coroutine feedbackCoroutine;

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

        Image target = lifeImages[lostLifeIndex];

        if (target == null)
            return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine =
            StartCoroutine(PulseLife(target.rectTransform));
    }

    private IEnumerator PulseLife(RectTransform target)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 enlargedScale =
            Vector3.one * pulseScale;

        float halfDuration =
            pulseDuration * 0.5f;

        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.unscaledDeltaTime;

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
            timer += Time.unscaledDeltaTime;

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

        target.localScale = originalScale;

        feedbackCoroutine = null;
    }
}