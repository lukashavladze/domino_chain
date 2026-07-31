using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Domino : MonoBehaviour
{
    [Header("Connections")]
    public List<Domino> nextDominoes = new();

    [Header("Gameplay")]
    public bool canStartChain;

    [Header("Settings")]
    public float pushForce = 2f;
    public float nextDelay = 0.08f;

    [Header("Cleanup")]
    public float fadeDuration = 0.3f;

    Rigidbody rb;

    bool hasStarted;
    bool destroyScheduled;

    Vector3 originalScale;

    public bool HasStarted => hasStarted;

    [Header("Reveal")]
    [SerializeField] private float revealPaintDistance = 0.08f;
    [SerializeField] private float revealStartAngle = 45f;

    private Vector3 lastPaintPosition;
    private bool hasPaintPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (!hasStarted)
            return;

        float tiltAngle = Vector3.Angle(
            transform.up,
            Vector3.up
        );

        // Do not reveal while the domino is still upright.
        if (tiltAngle < revealStartAngle)
            return;

        if (RevealPainter.Instance == null)
            return;

        float distanceMoved = hasPaintPosition
            ? Vector3.Distance(
                transform.position,
                lastPaintPosition
            )
            : float.MaxValue;

        if (distanceMoved < revealPaintDistance)
            return;

        RevealPainter.Instance.Paint(
            transform.position,
            transform.forward
        );

        lastPaintPosition = transform.position;
        hasPaintPosition = true;
    }

    IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(1f);

        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float p = t / fadeDuration;

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            transform.position = Vector3.Lerp(
                startPos,
                startPos + Vector3.down * 0.1f,
                p);

            yield return null;
        }

        Destroy(gameObject);
    }

    public void ResetDomino()
    {
        StopAllCoroutines();

        hasPaintPosition = false;
        hasStarted = false;
        destroyScheduled = false;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;
    }

    public void Fall(Vector3 direction)
    {
        if (hasStarted)
            return;

        hasStarted = true;
        hasPaintPosition = false;

        hasPaintPosition = false;

        rb.isKinematic = false;

        rb.AddForce(direction.normalized * pushForce, ForceMode.Impulse);

        if (!destroyScheduled)
        {
            destroyScheduled = true;
            StartCoroutine(FadeAndDestroy());
        }

        Invoke(nameof(TriggerNext), nextDelay);
    }

    public void StartChain()
    {
        if (hasStarted)
            return;

        if (nextDominoes.Count == 0)
            return;

        Vector3 dir =
            (nextDominoes[0].transform.position - transform.position).normalized;

        Fall(dir);
    }

    void TriggerNext()
    {
        foreach (Domino domino in nextDominoes)
        {
            if (domino == null)
                continue;

            Vector3 dir =
                (domino.transform.position - transform.position).normalized;

            domino.Fall(dir);
        }
    }
}