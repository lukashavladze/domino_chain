
using System.Collections;
using UnityEngine;

public class VictoryBannerAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform banner;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float duration = 0.55f;

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        if (banner == null || canvasGroup == null)
            yield break;

        banner.localScale = Vector3.one * 0.65f;
        canvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-out-back for a subtle premium overshoot.
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float p = 1f + c3 * Mathf.Pow(t - 1f, 3f)
                         + c1 * Mathf.Pow(t - 1f, 2f);

            banner.localScale =
                Vector3.one * Mathf.LerpUnclamped(0.65f, 1f, p);

            canvasGroup.alpha = t;

            yield return null;
        }

        banner.localScale = Vector3.one;
        canvasGroup.alpha = 1f;
    }
}
