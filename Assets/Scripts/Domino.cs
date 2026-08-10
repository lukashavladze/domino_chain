using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Domino : MonoBehaviour
{
    [Header("Line")]
    public DominoLine ownerLine;

    [Header("Blocking")]
    [SerializeField] private float standingAngle = 20f;

    [Header("Reveal Wave")]
    [SerializeField] private RevealWave revealWavePrefab;
    [SerializeField] private float waveGroundOffset = 0.03f;

    private bool waveSpawned;

    [Header("Connections")]
    public List<Domino> nextDominoes = new();

    [Header("Gameplay")]
    public bool canStartChain;

    [Header("Settings")]
    public float pushForce = 2f;
    public float nextDelay = 0.08f;

    [Header("Cleanup")]
    public float fadeDuration = 0.3f;

    [Header("Reveal")]
    [SerializeField] private float revealPaintDistance = 0.08f;
    [SerializeField] private float revealStartAngle = 45f;

    private Rigidbody rb;

    private bool hasStarted;
    private bool destroyScheduled;

    private Vector3 originalScale;

    private Vector3 lastPaintPosition;
    private bool hasPaintPosition;

    public bool HasStarted => hasStarted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
    }

    public bool IsStanding()
    {
        if (hasStarted)
            return false;

        float angle = Vector3.Angle(
            transform.up,
            Vector3.up
        );

        return angle <= standingAngle;
    }

    private void Update()
    {
        if (!hasStarted)
            return;

        float tiltAngle = Vector3.Angle(
            transform.up,
            Vector3.up
        );

        // Don't reveal or spawn wave while still upright.
        if (tiltAngle < revealStartAngle)
            return;

        SpawnRevealWave();

        PaintReveal();
    }

    private void SpawnRevealWave()
    {
        if (waveSpawned)
            return;

        waveSpawned = true;

        if (revealWavePrefab == null)
            return;

        Vector3 wavePosition = transform.position;

        // Assumes ground is around Y = 0.
        wavePosition.y = waveGroundOffset;

        Instantiate(
            revealWavePrefab,
            wavePosition,
            Quaternion.identity
        );
    }

    private void PaintReveal()
    {
        if (RevealPainter.Instance == null)
            return;

        float distanceMoved =
            hasPaintPosition
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

    public void Fall(Vector3 direction)
    {
        if (hasStarted)
            return;

        hasStarted = true;

        hasPaintPosition = false;
        waveSpawned = false;

        rb.isKinematic = false;

        rb.AddForce(
            direction.normalized * pushForce,
            ForceMode.Impulse
        );

        if (!destroyScheduled)
        {
            destroyScheduled = true;
            StartCoroutine(FadeAndDestroy());
        }

        CancelInvoke(nameof(TriggerNext));
        Invoke(nameof(TriggerNext), nextDelay);
    }

    public void StartChain()
    {
        if (hasStarted)
            return;

        if (nextDominoes.Count == 0)
            return;

        Domino next = nextDominoes[0];

        if (next == null)
            return;

        Vector3 direction =
            next.transform.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Fall(direction.normalized);
    }

    private void TriggerNext()
    {
        foreach (Domino domino in nextDominoes)
        {
            if (domino == null)
                continue;

            Vector3 direction =
                domino.transform.position -
                transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                continue;

            domino.Fall(direction.normalized);
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(1f);

        Vector3 startScale = transform.localScale;
        Vector3 startPosition = transform.position;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / fadeDuration
            );

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    t
                );

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    startPosition + Vector3.down * 0.1f,
                    t
                );

            yield return null;
        }

        Destroy(gameObject);
    }

    public void ResetDomino()
    {
        StopAllCoroutines();
        CancelInvoke();

        hasStarted = false;
        destroyScheduled = false;

        hasPaintPosition = false;
        waveSpawned = false;

        rb.isKinematic = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;
    }
}