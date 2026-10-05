using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float magnitude = 0.12f;

    private Vector3 originalLocalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    public void Shake()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float strength = 1.5f - (elapsed / duration);

            float x = Random.Range(-1f, 1f) * magnitude * strength;
            float y = Random.Range(-1f, 1f) * magnitude * strength;

            transform.localPosition =
                originalLocalPosition + new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        shakeCoroutine = null;
    }
}