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
                    Debug.Log(
                        $"{name} BLOCKED BY GENERATED DEPENDENCY: " +
                        $"{blocker.name}");

                    return true;
                }
            }
        }

        // ==============================================
        // 2. REAL PHYSICAL BLOCKING
        // ==============================================

        if (IsPhysicallyBlocked())
        {
            Debug.Log(
                $"{name} BLOCKED BY PHYSICAL CHECK");

            return true;
        }

        Debug.Log(
            $"{name} IS FREE");

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

        Collider lastCollider =
            last.GetComponentInChildren<Collider>();

        if (lastCollider == null)
            return false;

        // =====================================================
        // FALL DIRECTION
        // =====================================================

        Vector3 fallDirection =
            last.transform.position -
            previous.transform.position;

        fallDirection.y = 0f;

        if (fallDirection.sqrMagnitude < 0.001f)
            return false;

        fallDirection.Normalize();

        // =====================================================
        // GET REAL LOCAL COLLIDER SIZE
        // =====================================================

        BoxCollider box =
            lastCollider as BoxCollider;

        if (box == null)
        {
            Debug.LogWarning(
                "Domino should use a BoxCollider " +
                "for accurate blocking detection.");

            return false;
        }

        Vector3 scaledSize =
            Vector3.Scale(
                box.size,
                box.transform.lossyScale);

        // Your standing domino dimensions.
        float width =
            Mathf.Abs(scaledSize.x);

        float height =
            Mathf.Abs(scaledSize.y);

        float thickness =
            Mathf.Abs(scaledSize.z);

        // =====================================================
        // CREATE FALLEN-DOMINO ORIENTATION
        // =====================================================

        // The fallen domino lies along fallDirection.
        //
        // right = domino width
        // forward = fallen length/height

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                fallDirection);

        right.Normalize();

        Quaternion fallenRotation =
            Quaternion.LookRotation(
                fallDirection,
                Vector3.up);

        // =====================================================
        // FALLEN DOMINO CENTER
        // =====================================================

        // When it falls, approximately half of its height
        // extends forward from its pivot.

        Vector3 fallenCenter =
            last.transform.position +
            fallDirection * (height * 0.5f);

        // Keep the box close to the ground.
        fallenCenter.y =
            last.transform.position.y;

        // =====================================================
        // FALLEN BOX SIZE
        // =====================================================

        Vector3 halfExtents =
            new Vector3(
                width * 0.5f,
                thickness * 0.5f,
                height * 0.5f);

        // Small tolerance.
        //
        // IMPORTANT:
        // Don't make this large.
        halfExtents.x +=
            fallCheckPadding;

        halfExtents.z +=
            fallCheckPadding;

        // =====================================================
        // CHECK COLLISION
        // =====================================================

        Collider[] hits =
            Physics.OverlapBox(
                fallenCenter,
                halfExtents,
                fallenRotation,
                blockerMask,
                QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            Domino other =
                hit.GetComponentInParent<Domino>();

            if (other == null)
                continue;

            // Ignore our own line.
            if (other.ownerLine == this)
                continue;

            // Already fallen/started domino doesn't block us.
            if (other.HasStarted)
                continue;

            if (!other.IsStanding())
                continue;

            return true;
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