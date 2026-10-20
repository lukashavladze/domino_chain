
using UnityEngine;

public class CameraLightFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Settings")]
    [SerializeField] private bool followYOnly = true;

    private Quaternion initialLightRotation;
    private Quaternion initialPivotRotation;

    private void Start()
    {
        if (cameraPivot == null)
        {
            Debug.LogError("Camera Pivot is not assigned!");
            enabled = false;
            return;
        }

        initialLightRotation = transform.rotation;
        initialPivotRotation = cameraPivot.rotation;
    }

    private void LateUpdate()
    {
        if (cameraPivot == null) return;

        if (followYOnly)
        {
            float angle =
                Mathf.DeltaAngle(
                    initialPivotRotation.eulerAngles.y,
                    cameraPivot.eulerAngles.y
                );

            transform.rotation =
                Quaternion.AngleAxis(angle, Vector3.up)
                * initialLightRotation;
        }
        else
        {
            Quaternion delta =
                cameraPivot.rotation *
                Quaternion.Inverse(initialPivotRotation);

            transform.rotation =
                delta * initialLightRotation;
        }
    }
}
