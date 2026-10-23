
using System.Collections;
using UnityEngine;

public class VictoryGoldenRays : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rays;
    [SerializeField] private CanvasGroup raysCanvasGroup;

    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 0.45f;
    [SerializeField] private float appearDuration = 0.65f;

    private Coroutine appearCoroutine;
    private float visibility;

    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        if (rays == null || raysCanvasGroup == null)
            return;

        if (appearCoroutine != null)
            StopCoroutine(appearCoroutine);

        rays.localScale = Vector3.one * 0.7f;
        raysCanvasGroup.alpha = 0f;
        visibility = 0f;

        appearCoroutine = StartCoroutine(Appear());
    }

    private IEnumerator Appear()
    {
        float elapsed = 0f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(0.01f, appearDuration)
            );

            float smooth = t * t * (3f - 2f * t);

            visibility = smooth;

            rays.localScale = Vector3.one *
                Mathf.Lerp(0.7f, 1f, smooth);

            yield return null;
        }

        visibility = 1f;
        rays.localScale = Vector3.one;
        appearCoroutine = null;
    }

    private void Update()
    {
        if (rays == null || raysCanvasGroup == null)
            return;

        rays.Rotate(
            0f,
            0f,
            rotationSpeed * Time.unscaledDeltaTime
        );

        float pulse =
            (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f)
            * 0.5f;

        raysCanvasGroup.alpha =
            visibility * Mathf.Lerp(minAlpha, maxAlpha, pulse);
    }
}
