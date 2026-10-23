
using System.Collections;
using UnityEngine;

public class VictoryBannerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform banner;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float appearDelay = 0.15f;
    [SerializeField] private float duration = 0.65f;
    [SerializeField] private float startScale = 0.65f;
    [SerializeField] private float overshootScale = 1.08f;

    private Vector3 originalScale;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (banner == null)
            banner = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalScale = banner.localScale;

        if (originalScale.x <= 0.001f ||
            originalScale.y <= 0.001f ||
            originalScale.z <= 0.001f)
        {
            originalScale = Vector3.one;
        }

        banner.localScale = originalScale;

        Debug.Log("VICTORY BANNER INITIALIZED");
    }

    public void Play()
    {
        Debug.Log("VICTORY BANNER PLAY CALLED");

        if (banner == null || canvasGroup == null)
        {
            Debug.LogError("VICTORY BANNER REFERENCES MISSING");
            return;
        }

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        banner.localScale = originalScale * startScale;
        canvasGroup.alpha = 0f;

        animationCoroutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        yield return new WaitForSecondsRealtime(appearDelay);

        Debug.Log("VICTORY BANNER ANIMATION STARTED");

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float eased = 1f - Mathf.Pow(1f - t, 3f);

            float scale;

            if (t < 0.65f)
            {
                float p = t / 0.65f;
                p = 1f - Mathf.Pow(1f - p, 3f);

                scale = Mathf.Lerp(
                    startScale,
                    overshootScale,
                    p
                );
            }
            else
            {
                float p = (t - 0.65f) / 0.35f;
                p = p * p * (3f - 2f * p);

                scale = Mathf.Lerp(
                    overshootScale,
                    1f,
                    p
                );
            }

            banner.localScale = originalScale * scale;

            // Fade in quickly.
            canvasGroup.alpha = Mathf.Clamp01(eased * 1.5f);

            yield return null;
        }

        banner.localScale = originalScale;
        canvasGroup.alpha = 1f;

        animationCoroutine = null;

        Debug.Log("VICTORY BANNER ANIMATION FINISHED");
    }

    public void ResetAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (banner != null)
            banner.localScale = originalScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (banner != null)
            banner.localScale = originalScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}
