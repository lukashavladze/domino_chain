using System.Collections.Generic;
using UnityEngine;

public class DominoLine : MonoBehaviour
{
    [Header("Dominoes")]
    public Domino firstDomino;
    public List<Domino> dominoes = new();

    [Header("Physical Blocking")]

    [Tooltip("Multiplier for how far the last domino can reach when falling.")]
    [Range(0.5f, 1.5f)]
    public float fallReachMultiplier = 1.0f;

    [Tooltip("Extra width added to the falling collision check.")]
    [Range(0f, 0.3f)]
    public float fallCheckPadding = 0.05f;

    public LayerMask blockerMask = ~0;

    private bool lineStarted;

    [Header("Procedural Blocking")]

    [Tooltip("Use explicit generated dependencies instead of Physics overlap blocking.")]
    public bool useGeneratedBlocking = false;

    [Tooltip("These lines must start before this line becomes available.")]
    public List<DominoLine> blockedByLines = new();

    public bool HasStartedLine => lineStarted;



    [Header("Visual")]
    [SerializeField] private Renderer dominoRenderer;

    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material firstDominoMaterial;

    public void AutoConnect()
    {
        dominoes.Clear();

        foreach (Transform child in transform)
        {
            Domino domino = child.GetComponent<Domino>();

            if (domino != null)
                dominoes.Add(domino);
        }

        if (dominoes.Count == 0)
        {
            firstDomino = null;
            return;
        }

        firstDomino = dominoes[0];

        for (int i = 0; i < dominoes.Count; i++)
        {
            Domino domino = dominoes[i];

            domino.ownerLine = this;

            // Don't decide availability here anymore.
            domino.canStartChain = (i == 0);
            // Set material:
            // first domino = green
            // all others = normal
            Renderer renderer =
                domino.GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    (i == 0)
                        ? firstDominoMaterial
                        : normalMaterial;
            }

            domino.nextDominoes.Clear();

            if (i < dominoes.Count - 1)
            {
                domino.nextDominoes.Add(dominoes[i + 1]);
            }
        }

        RefreshAvailability();

        Debug.Log("Auto Connected " + dominoes.Count + " dominoes.");
    }

    public void SetFirstDominoVisual(bool isFirst)
    {
        if (dominoRenderer == null)
            dominoRenderer = GetComponentInChildren<Renderer>();

        if (dominoRenderer == null)
            return;

        dominoRenderer.sharedMaterial =
            isFirst
                ? firstDominoMaterial
                : normalMaterial;
    }

    private void Update()
    {
        RefreshAvailability();
    }

    public bool CanStartLine()
    {
        if (lineStarted)
            return false;

        if (firstDomino == null)
            return false;

        if (firstDomino.HasStarted)
            return false;

        return !IsBlocked();
    }

    public bool TryStartLine()
    {
        if (!CanStartLine())
            return false;

        lineStarted = true;

        firstDomino.StartChain();

        return true;
    }

    private void RefreshAvailability()
    {
        if (firstDomino == null)
            return;

        // Already started = cannot click again.
        if (firstDomino.HasStarted)
        {
            firstDomino.canStartChain = false;
         
            return;
        }

        bool blocked = IsBlocked();

        firstDomino.canStartChain = !blocked;

       
    }
    private bool IsBlocked()
    {
        // ==============================================
        // 1. GENERATED DEPENDENCIES
        // ==============================================

        if (useGeneratedBlocking)
        {
            foreach (DominoLine blocker in blockedByLines)
            {
                if (blocker == null)
                    continue;

                if (!blocker.HasStartedLine)
                {
                    return true;
                }
            }
        }


        // ==============================================
        // 2. REAL PHYSICAL BLOCKING
        // ==============================================

        if (IsPhysicallyBlocked())
        {
            return true;
        }


        return false;
    }

    private bool IsPhysicallyBlocked()
    {
        if (dominoes == null ||
            dominoes.Count < 2)
        {
            return false;
        }

        Domino last =
            dominoes[dominoes.Count - 1];

        Domino previous =
            dominoes[dominoes.Count - 2];

        if (last == null ||
            previous == null)
        {
            return false;
        }

        Vector3 direction =
            last.transform.position -
            previous.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();

        // -------------------------------------------------
        // Approximate the space the LAST domino occupies
        // while falling forward.
        // -------------------------------------------------

        Collider lastCollider =
            last.GetComponentInChildren<Collider>();

        if (lastCollider == null)
            return false;

        Bounds bounds =
            lastCollider.bounds;

        // Domino height becomes approximately its forward
        // reach when it falls.
        float fallReach =
     bounds.size.y *
     fallReachMultiplier;

        // Thickness / width of the domino.
        float halfWidth =
    Mathf.Max(
        bounds.extents.x,
        bounds.extents.z
    ) +
    fallCheckPadding;

        // Start slightly in front of the last domino.
        Vector3 start =
            last.transform.position +
            direction * 0.05f;

        // End where the top of the domino would roughly land.
        Vector3 end =
            last.transform.position +
            direction * fallReach;

        // Keep check near the center height of possible collision.
        float checkHeight =
            last.transform.position.y;

        start.y = checkHeight;
        end.y = checkHeight;

        Collider[] hits =
            Physics.OverlapCapsule(
                start,
                end,
                halfWidth,
                blockerMask,
                QueryTriggerInteraction.Ignore
            );

        foreach (Collider hit in hits)
        {
            Domino other =
                hit.GetComponentInParent<Domino>();

            if (other == null)
                continue;

            // Ignore our own line.
            if (other.ownerLine == this)
                continue;

            // Ignore dominoes that already started.
            if (other.HasStarted)
                continue;

            // Only standing dominoes count.
            if (!other.IsStanding())
                continue;

            return true;
        }

        return false;
    }

    public void StartLine()
    {
        if (firstDomino == null)
            return;

        if (!firstDomino.canStartChain)
            return;

        firstDomino.StartChain();
    }

    public void ResetLine()
    {
        lineStarted = false;

        foreach (Domino domino in dominoes)
        {
            if (domino != null)
                domino.ResetDomino();
        }

        RefreshAvailability();
    }

    //private void OnDrawGizmosSelected()
    //{
    //    if (dominoes == null || dominoes.Count < 2)
    //        return;

    //    Domino last = dominoes[dominoes.Count - 1];
    //    Domino previous = dominoes[dominoes.Count - 2];

    //    if (last == null || previous == null)
    //        return;

    //    Vector3 direction =
    //        last.transform.position -
    //        previous.transform.position;

    //    direction.y = 0;

    //    if (direction.sqrMagnitude < 0.001f)
    //        return;

    //    direction.Normalize();

    //    Vector3 checkPosition =
    //        last.transform.position +
    //        direction * blockerCheckDistance;

    //    Gizmos.DrawWireSphere(
    //        checkPosition,
    //        blockerCheckRadius
    //    );
    //}
}