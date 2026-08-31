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


    [Header("Connections")]
    public List<Domino> nextDominoes = new();

    [Header("Gameplay")]
    public bool canStartChain;

    [Header("Settings")]
    public float pushForce = 1.5f;
    public float nextDelay = 0.12f;

    [Header("Cleanup")]
    public float fadeDuration = 0.3f;

    [Header("Reveal Footprint")]
    [SerializeField] private float revealLength = 0.65f;
    [SerializeField] private float revealWidth = 0.65f;

    private Rigidbody rb;

    private bool hasStarted;
    private bool destroyScheduled;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Vector3 originalForward;
    private Quaternion originalRotation;


    public bool HasStarted => hasStarted;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        originalScale = transform.localScale;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalForward = transform.forward;

        // IMPORTANT:
        // Standing dominoes must never be knocked down
        // by physics from another line.
        rb.isKinematic = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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


    public void SetRevealSize(float size)
    {
        revealLength = size;
        revealWidth = size;
    }


    private void PaintOriginalFootprint()
    {
        if (RevealPainter.Instance == null)
            return;

        RevealPainter.Instance.Paint(
            originalPosition,
            originalForward,
            new Vector2(
                revealLength,
                revealWidth
            )
        );
    }

    

    public void Fall(Vector3 direction)
    {
        if (hasStarted)
            return;

        hasStarted = true;
        

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

        // Reveal when this domino starts fading away.
        PaintOriginalFootprint();

        Vector3 startScale =
            transform.localScale;

        Vector3 startPosition =
            transform.position;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
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
                    startPosition +
                    Vector3.down * 0.1f,
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

        rb.isKinematic = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
    }
}