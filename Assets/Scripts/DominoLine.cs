using System.Collections.Generic;
using UnityEngine;

public class DominoLine : MonoBehaviour
{
    [Header("Dominoes")]
    public Domino firstDomino;
    public List<Domino> dominoes = new();

    [Header("Arrow-Style Blocking")]
    [Tooltip("How far in front of the final domino we check.")]
    public float blockerCheckDistance = 0.28f;

    [Tooltip("Radius used to detect another standing domino.")]
    public float blockerCheckRadius = 0.14f;

    public LayerMask blockerMask = ~0;

    // Once we discover a blocker, keep the line blocked
    // until that domino is actually destroyed/disappears.
    private Domino lockedBlocker;

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
        // We previously found a blocker.
        // Unity destroyed objects compare == null,
        // so this stays blocked until that domino disappears.
        if (lockedBlocker != null)
        {
            return true;
        }

        if (dominoes.Count < 2)
            return false;

        Domino last = dominoes[dominoes.Count - 1];
        Domino previous = dominoes[dominoes.Count - 2];

        if (last == null || previous == null)
            return false;

        Vector3 direction =
            last.transform.position -
            previous.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();

        Vector3 checkPosition =
            last.transform.position +
            direction * blockerCheckDistance;

        Collider[] hits = Physics.OverlapSphere(
            checkPosition,
            blockerCheckRadius,
            blockerMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            Domino other =
                hit.GetComponentInParent<Domino>();

            if (other == null)
                continue;

            // Don't block ourselves.
            if (other.ownerLine == this)
                continue;

            // Only an upright / unfallen domino creates
            // a NEW blocking relationship.
            if (!other.IsStanding())
                continue;

            lockedBlocker = other;

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
        lockedBlocker = null;

        foreach (Domino domino in dominoes)
        {
            if (domino != null)
                domino.ResetDomino();
        }

        RefreshAvailability();
    }

    private void OnDrawGizmosSelected()
    {
        if (dominoes == null || dominoes.Count < 2)
            return;

        Domino last = dominoes[dominoes.Count - 1];
        Domino previous = dominoes[dominoes.Count - 2];

        if (last == null || previous == null)
            return;

        Vector3 direction =
            last.transform.position -
            previous.transform.position;

        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        Vector3 checkPosition =
            last.transform.position +
            direction * blockerCheckDistance;

        Gizmos.DrawWireSphere(
            checkPosition,
            blockerCheckRadius
        );
    }
}