
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

    [Header("Final Scale")]
    [SerializeField] private float endScale = 1.20f;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem victoryBurst;
    private ParticleSystem victoryBurstInstance;
    //[SerializeField] private float burstDelay = 0.35f;


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

        if (banner == null)
        {
            Debug.LogError(
                "VICTORY BANNER RECT TRANSFORM MISSING");
            return;
        }

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
            Debug.LogError(
                "VICTORY BANNER REFERENCES MISSING");
            return;
        }

        StopCurrentAnimation();
        StopParticles();

        banner.localScale = originalScale * startScale;
        canvasGroup.alpha = 0f;

        animationCoroutine = StartCoroutine(Animate());
    }


    private IEnumerator Animate()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, appearDelay));

        Debug.Log("VICTORY BANNER ANIMATION STARTED");

        float elapsed = 0f;
        bool burstPlayed = false;

        float safeDuration = Mathf.Max(0.001f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            float scale;

            if (t < 0.65f)
            {
                // Phase 1: Grow to overshoot scale.
                float p = t / 0.65f;
                p = 1f - Mathf.Pow(1f - p, 3f);

                scale = Mathf.Lerp(
                    startScale,
                    overshootScale,
                    p);
            }
            else
            {
                // Phase 2: Overshoot reached.
                // Trigger golden particles exactly once.
                if (!burstPlayed)
                {
                    banner.localScale =
                        originalScale * overshootScale;

                    PlayParticles();
                    burstPlayed = true;

                    Debug.Log(
                        "GOLDEN BURST AT OVERSHOOT SCALE");
                }

                // Settle to final scale.
                float p = (t - 0.65f) / 0.35f;
                p = p * p * (3f - 2f * p);

                scale = Mathf.Lerp(
                    overshootScale,
                    endScale,
                    p);
            }

            banner.localScale = originalScale * scale;
            canvasGroup.alpha = Mathf.Clamp01(eased * 1.5f);

            yield return null;
        }

        // Handle unusually short animations.
        if (!burstPlayed)
            PlayParticles();

        banner.localScale = originalScale * endScale;
        canvasGroup.alpha = 1f;

        animationCoroutine = null;

        Debug.Log("VICTORY BANNER ANIMATION FINISHED");
    }




    private void PlayParticles()
    {
        if (victoryBurst == null)
        {
            Debug.LogError("VICTORY BURST PREFAB IS NULL!");
            return;
        }

        // Create a scene instance from the prefab once.
        if (victoryBurstInstance == null)
        {
            victoryBurstInstance = Instantiate(victoryBurst);

            victoryBurstInstance.name = "VictoryGoldenBurst_Runtime";

            // Keep it independent of the popup hierarchy.
            victoryBurstInstance.transform.SetParent(null, true);

            Debug.Log("VICTORY BURST PREFAB INSTANTIATED");
        }

        // Ensure the instance is active.
        if (!victoryBurstInstance.gameObject.activeSelf)
            victoryBurstInstance.gameObject.SetActive(true);

        // Restart the burst.
        victoryBurstInstance.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear);

        victoryBurstInstance.Play(true);

        Debug.Log(
            $"GOLDEN BURST PLAYED | " +
            $"Playing={victoryBurstInstance.isPlaying}");
    }



    private void StopParticles()
    {
        if (victoryBurstInstance == null)
            return;

        victoryBurstInstance.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void StopCurrentAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (victoryBurstInstance != null)
        {
            Destroy(victoryBurstInstance.gameObject);
            victoryBurstInstance = null;
        }
    }

    public void ResetAnimation()
    {
        StopCurrentAnimation();
        StopParticles();

        if (banner != null)
            banner.localScale = originalScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Debug.Log("VICTORY BANNER RESET");
    }

    private void OnDisable()
    {
        StopCurrentAnimation();
        StopParticles();

        if (banner != null)
            banner.localScale = originalScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}
