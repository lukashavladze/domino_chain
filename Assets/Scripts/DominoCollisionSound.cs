
using UnityEngine;

public class DominoCollisionSound : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minImpactSpeed = 0.10f;
    [SerializeField] private float soundCooldown = 0.045f;

    private float lastSoundTime = -100f;

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastSoundTime < soundCooldown)
            return;

        Domino myDomino =
            GetComponentInParent<Domino>();

        Domino otherDomino =
            collision.collider.GetComponentInParent<Domino>();

        if (myDomino == null || otherDomino == null)
            return;

        // Ignore collisions while either domino is fading.
        if (myDomino.IsFading || otherDomino.IsFading)
            return;

        // Ignore contacts between two dominoes
        // that have not started falling.
        if (!myDomino.HasStarted &&
            !otherDomino.HasStarted)
            return;

        float impactSpeed =
            collision.relativeVelocity.magnitude;

        if (impactSpeed < minImpactSpeed)
            return;

        if (DominoSoundManager.Instance == null)
            return;

        lastSoundTime = Time.time;

        DominoSoundManager.Instance.PlayCollision(impactSpeed);
    }
}
