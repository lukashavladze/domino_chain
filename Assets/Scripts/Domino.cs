using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Domino : MonoBehaviour
{
    [Header("Connections")]
    public Domino nextDomino;

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

        Debug.Log("Fall: " + name);

        hasStarted = true;

        rb.isKinematic = false;

        rb.AddForce(direction * pushForce, ForceMode.Impulse);

        Invoke(nameof(TriggerNext), nextDelay);
    }

    public void StartChain()
    {
        if (hasStarted)
            return;

        if (nextDomino == null)
            return;

        Vector3 dir =
            (nextDomino.transform.position - transform.position).normalized;

        Fall(dir);
    }

    void TriggerNext()
    {
        if (nextDomino == null)
            return;

        Vector3 dir =
            (nextDomino.transform.position - transform.position).normalized;

        nextDomino.Fall(dir);
    }
}