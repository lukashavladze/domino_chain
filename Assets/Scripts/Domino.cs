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

    [Header("Fall Physics")]
    [SerializeField] private float gravityMultiplier = 2.0f;

    [Header("Gameplay")]
    public bool canStartChain;

    [Header("Settings")]
    public float pushForce = 2.0f;
    [Header("Physics Chain")]
    [Tooltip("How close the falling domino must be before the next domino becomes dynamic.")]
    [SerializeField] private float activationDistance = 0.22f;

    [Header("Cleanup")]
    public float fadeDuration = 0.15f;

    [Header("Reveal Footprint")]
    [SerializeField] private float revealLength = 0.65f;
    [SerializeField] private float revealWidth = 0.65f;

    private Rigidbody rb;

    private bool hasStarted;
    private bool fadeScheduled;
    private bool nextActivated;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Vector3 originalForward;
    private Quaternion originalRotation;

    private Vector3 fallDirection;

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

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

    }


    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic)
            return;

        rb.AddForce(
            Physics.gravity * (gravityMultiplier - 1f),
            ForceMode.Acceleration
        );
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

        Vector3 direction = fallDirection;

        if (direction.sqrMagnitude < 0.001f)
            direction = originalForward;

        direction.y = 0f;
        direction.Normalize();

        Vector3 revealPosition =
            originalPosition +
            direction * (revealLength * 0.5f);

        RevealPainter.Instance.Paint(
            revealPosition,
            direction,
            new Vector2(
                revealWidth,
                revealLength
            )
        );
    }



    public void Fall(Vector3 direction)
    {
        if (hasStarted)
            return;

        hasStarted = true;
        nextActivated = false;

        fallDirection = direction.normalized;

        rb.isKinematic = false;

        rb.AddForce(
            direction.normalized * pushForce,
            ForceMode.Impulse
        );

        if (!fadeScheduled)
        {
            fadeScheduled = true;
            StartCoroutine(FadeOut());
        }

        StartCoroutine(ActivateNextBeforeContact());
    }


   private void ActivateFromPrevious(Vector3 direction)
{
    if (hasStarted)
        return;

    hasStarted = true;
    nextActivated = false;

    fallDirection = direction.normalized;

    rb.isKinematic = false;

    if (!fadeScheduled)
    {
        fadeScheduled = true;
        StartCoroutine(FadeOut());
    }

    StartCoroutine(ActivateNextBeforeContact());
}

    private IEnumerator ActivateNextBeforeContact()
    {
        if (nextDominoes == null ||
            nextDominoes.Count == 0)
        {
            yield break;
        }

        while (!nextActivated)
        {
            foreach (Domino next in nextDominoes)
            {
                if (next == null || next.hasStarted)
                    continue;

                Vector3 difference =
                    next.transform.position -
                    transform.position;

                difference.y = 0f;

                float currentDistance =
                    difference.magnitude;

                Vector3 originalDifference =
                    next.originalPosition -
                    originalPosition;

                originalDifference.y = 0f;

                float originalDistance =
                    originalDifference.magnitude;

                // IMPORTANT:
                // Do not activate merely because the dominoes
                // naturally stand close to each other.
                //
                // Current distance must have become noticeably
                // smaller than their original standing distance.
                float triggerDistance =
                    Mathf.Max(
                        activationDistance,
                        originalDistance * 0.72f
                    );

                if (currentDistance <= triggerDistance)
                {
                    nextActivated = true;

                    Vector3 direction =
                        next.originalPosition -
                        originalPosition;

                    direction.y = 0f;

                    if (direction.sqrMagnitude > 0.001f)
                        direction.Normalize();

                    next.ActivateFromPrevious(direction);

                    yield break;
                }
            }

            yield return new WaitForFixedUpdate();
        }
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
            next.originalPosition -
            originalPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Fall(direction.normalized);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasStarted)
            return;

        if (nextDominoes == null ||
            nextDominoes.Count == 0)
        {
            return;
        }

        Domino hitDomino =
            collision.collider.GetComponentInParent<Domino>();

        if (hitDomino == null)
            return;

        // ONLY activate dominoes that are explicitly
        // connected as our next domino.
        if (!nextDominoes.Contains(hitDomino))
            return;

        if (hitDomino.HasStarted)
            return;

        Vector3 direction =
            hitDomino.originalPosition -
            originalPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        nextActivated = true;

        hitDomino.ActivateFromPrevious(direction);
    }


    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(1.8f);

        Vector3 startScale = transform.localScale;
        Vector3 startPosition = transform.position;

        Vector3 endPosition =
            startPosition + Vector3.down * 0.1f;

        float timer = 0f;

        bool painted = false;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / fadeDuration
            );

            float easedT =
                t * t * (3f - 2f * t);

            // Shrink + sink.
            transform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                easedT
            );

            transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                easedT
            );

            // Reveal around the middle of the disappearance.
            if (!painted && t >= 0.5f)
            {
                painted = true;
                PaintOriginalFootprint();
            }

            yield return null;
        }

        // Safety: make sure it was painted even if
        // fadeDuration was extremely short.
        if (!painted)
            PaintOriginalFootprint();

        transform.localScale = Vector3.zero;
        transform.position = endPosition;

        rb.isKinematic = true;
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetDomino()
    {
        StopAllCoroutines();
        CancelInvoke();

        hasStarted = false;
        fadeScheduled = false;
        nextActivated = false;

        rb.isKinematic = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;

        gameObject.SetActive(true);
    }
}