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

    Rigidbody rb;

    bool hasStarted;

    public bool HasStarted => hasStarted;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void ResetDomino()
    {
        hasStarted = false;

        rb.isKinematic = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localRotation = Quaternion.identity;
    }

    public void Fall(Vector3 direction)
    {
        if (hasStarted)
            return;

        hasStarted = true;

        rb.isKinematic = false;

        rb.AddForce(direction.normalized * pushForce, ForceMode.Impulse);

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