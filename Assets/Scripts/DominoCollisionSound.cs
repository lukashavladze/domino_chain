using UnityEngine;

public class DominoCollisionSound : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minImpactSpeed = 0.25f;
    [SerializeField] private float soundCooldown = 0.045f;

    private float lastSoundTime = -100f;

    private void Start()
    {
        Debug.Log(
            "DOMINO SOUND COMPONENT ACTIVE: " + gameObject.name
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(
        $"DOMINO COLLISION: {gameObject.name} hit " +
        $"{collision.gameObject.name} | " +
        $"Speed: {collision.relativeVelocity.magnitude:F2}"
    );
        if (Time.time - lastSoundTime < soundCooldown)
            return;

        Domino otherDomino =
            collision.collider
                .GetComponentInParent<Domino>();

        // Only domino-to-domino collisions.
        if (otherDomino == null)
            return;

        float impactSpeed =
            collision.relativeVelocity.magnitude;

        if (impactSpeed < minImpactSpeed)
            return;

        // Avoid playing the same collision twice.
        Domino myDomino =
            GetComponentInParent<Domino>();

        if (myDomino == null)
            return;

        if (myDomino.GetInstanceID() >
            otherDomino.GetInstanceID())
            return;

        if (DominoSoundManager.Instance == null)
            return;

        lastSoundTime = Time.time;

        DominoSoundManager.Instance
            .PlayCollision(impactSpeed);
    }
}