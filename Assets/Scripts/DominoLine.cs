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

    [Header("Full Fall Sweep")]

    [Range(4, 16)]
    public int fallSweepSamples = 8;

    [Range(0f, 0.1f)]
    public float fallSweepPadding = 0.030f;

    private bool lineStarted;

    [Header("Procedural Blocking")]

    [Tooltip("Use explicit generated dependencies instead of Physics overlap blocking.")]
    public bool useGeneratedBlocking = false;

    [Tooltip("These lines must start before this line becomes available.")]
    public List<DominoLine> blockedByLines = new();

    public bool HasStartedLine => lineStarted;

    private float nextAvailabilityCheck;


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

            // Only first domino can start the line.
            domino.canStartChain = (i == 0);

            // ==========================================
            // MATERIAL
            // ==========================================

            Renderer renderer =
                domino.GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                if (i == 0)
                {
                    // First/start domino.
                    renderer.sharedMaterial =
                        firstDominoMaterial;
                }
                else
                {
                    // All normal dominoes.
                    renderer.sharedMaterial =
                        normalMaterial;
                }
            }

            // ==========================================
            // CONNECTIONS
            // ==========================================

            domino.nextDominoes.Clear();

            if (i < dominoes.Count - 1)
            {
                domino.nextDominoes.Add(
                    dominoes[i + 1]);
            }
        }

        RefreshAvailability();

        Debug.Log(
            "Auto Connected " +
            dominoes.Count +
            " dominoes.");
    }

    public bool IsProcedurallyBlocked()
    {
        if (!useGeneratedBlocking)
            return false;

        foreach (DominoLine blocker in blockedByLines)
        {
            if (blocker != null && !blocker.HasStartedLine)
                return true;
        }

        return false;
    }

    public void ClearGeneratedBlocking()
    {
        blockedByLines.Clear();
    }

    public void AddBlocker(DominoLine line)
    {
        if (line == null || line == this)
            return;

        if (!blockedByLines.Contains(line))
            blockedByLines.Add(line);
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
        if (Time.time < nextAvailabilityCheck)
            return;

        nextAvailabilityCheck = Time.time + 0.2f;

        if (!lineStarted)
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
                    //Debug.Log(
                    //    $"{name} BLOCKED BY GENERATED DEPENDENCY: " +
                    //    $"{blocker.name}");

                    return true;
                }
            }
        }

        // ==============================================
        // 2. REAL PHYSICAL BLOCKING
        // ==============================================

        if (IsPhysicallyBlocked())
        {
            //Debug.Log(
            //    $"{name} BLOCKED BY PHYSICAL CHECK");

            return true;
        }

        //Debug.Log(
        //    $"{name} IS FREE");

        return false;
    }

    private bool IsPhysicallyBlocked()
    {
        if (dominoes == null || dominoes.Count < 2)
            return false;

        Physics.SyncTransforms();

        // Check the entire chain, not only its last domino.
        for (int i = 0; i < dominoes.Count; i++)
        {
            Domino domino = dominoes[i];

            if (domino == null)
                continue;

            Vector3 direction =
                GetDominoFallDirection(i);

            if (direction.sqrMagnitude < 0.001f)
                continue;

            if (DoesFallSweepHitStandingLine(
                    domino,
                    direction))
            {
                return true;
            }
        }

        return false;
    }


    private Vector3 GetDominoFallDirection(int index)
    {
        if (dominoes == null ||
            dominoes.Count < 2 ||
            index < 0 ||
            index >= dominoes.Count)
        {
            return Vector3.zero;
        }

        Domino current = dominoes[index];

        if (current == null)
            return Vector3.zero;

        Vector3 direction;

        if (index < dominoes.Count - 1)
        {
            Domino next = dominoes[index + 1];

            if (next == null)
                return Vector3.zero;

            direction =
                next.transform.position -
                current.transform.position;
        }
        else
        {
            Domino previous = dominoes[index - 1];

            if (previous == null)
                return Vector3.zero;

            direction =
                current.transform.position -
                previous.transform.position;
        }

        direction.y = 0f;

        return direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector3.zero;
    }


    private bool DoesFallSweepHitStandingLine(
        Domino fallingDomino,
        Vector3 fallDirection)
    {
        BoxCollider box =
            fallingDomino.GetComponentInChildren<BoxCollider>();

        if (box == null)
            return false;

        Vector3 scale = box.transform.lossyScale;

        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        scale.z = Mathf.Abs(scale.z);

        Vector3 size =
            Vector3.Scale(box.size, scale);

        Vector3 halfExtents = size * 0.5f;

        halfExtents +=
            Vector3.one * fallSweepPadding;

        Vector3 standingCenter =
            box.transform.TransformPoint(box.center);

        Vector3 pivot =
            standingCenter -
            box.transform.up * (size.y * 0.5f);

        Vector3 rotationAxis =
            Vector3.Cross(
                Vector3.up,
                fallDirection);

        if (rotationAxis.sqrMagnitude < 0.001f)
            return false;

        rotationAxis.Normalize();

        int samples = Mathf.Max(4, fallSweepSamples);

        for (int i = 1; i <= samples; i++)
        {
            float angle =
                Mathf.Lerp(
                    5f,
                    90f,
                    i / (float)samples);

            Quaternion deltaRotation =
                Quaternion.AngleAxis(
                    angle,
                    rotationAxis);

            Vector3 center =
                pivot +
                deltaRotation *
                (standingCenter - pivot);

            Quaternion rotation =
                deltaRotation *
                box.transform.rotation;

            Collider[] hits =
                Physics.OverlapBox(
                    center,
                    halfExtents,
                    rotation,
                    blockerMask,
                    QueryTriggerInteraction.Ignore);

            foreach (Collider hit in hits)
            {
                Domino other =
                    hit.GetComponentInParent<Domino>();

                if (other == null)
                    continue;

                // Ignore the falling domino itself.
                if (other == fallingDomino)
                    continue;

                // Same-chain collisions are intentional.
                if (other.ownerLine == this)
                    continue;

                // Already activated dominoes are not
                // treated as standing obstacles.
                if (other.HasStarted)
                    continue;

                if (!other.IsStanding())
                    continue;

                return true;
            }
        }

        return false;
    }

    public void StartLine()
    {
        TryStartLine();
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

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (dominoes == null ||
            dominoes.Count < 2)
        {
            return;
        }

        Domino last =
            dominoes[dominoes.Count - 1];

        Domino previous =
            dominoes[dominoes.Count - 2];

        if (last == null ||
            previous == null)
        {
            return;
        }

        BoxCollider box =
            last.GetComponentInChildren<BoxCollider>();

        if (box == null)
            return;

        Vector3 direction =
            last.transform.position -
            previous.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        Vector3 size =
            Vector3.Scale(
                box.size,
                box.transform.lossyScale);

        float width =
            Mathf.Abs(size.x);

        float height =
            Mathf.Abs(size.y);

        float thickness =
            Mathf.Abs(size.z);

        Vector3 center =
            last.transform.position +
            direction * (height * 0.5f);

        center.y =
            last.transform.position.y;

        Quaternion rotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up);

        Vector3 drawSize =
            new Vector3(
                width + fallCheckPadding * 2f,
                thickness,
                height + fallCheckPadding * 2f);

        Matrix4x4 oldMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                center,
                rotation,
                Vector3.one);

        Gizmos.DrawWireCube(
            Vector3.zero,
            drawSize);

        Gizmos.matrix =
            oldMatrix;
    }

#endif
}