using UnityEngine;

public class BoardFrame : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private Renderer groundRenderer;


    [Header("Edges")]
    [SerializeField] private Transform top;
    [SerializeField] private Transform bottom;
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;


    [Header("Corners")]
    [SerializeField] private Transform cornerTL;
    [SerializeField] private Transform cornerTR;
    [SerializeField] private Transform cornerBL;
    [SerializeField] private Transform cornerBR;


    [Header("Edge Settings")]
    [SerializeField] private float frameWidth = 0.30f;
    [SerializeField] private float frameHeight = 0.12f;
    [SerializeField] private float gap = 0.02f;


    [Header("Corner Settings")]
    [SerializeField] private float cornerSize = 0.45f;
    [SerializeField] private float cornerHeight = 0.14f;


    public void Resize()
    {
        if (groundRenderer == null)
        {
            Debug.LogWarning(
                "BoardFrame: Ground Renderer is not assigned."
            );

            return;
        }


        Bounds bounds =
            groundRenderer.bounds;


        float groundWidth =
            bounds.size.x;

        float groundDepth =
            bounds.size.z;


        Vector3 center =
            bounds.center;


        // ==========================================
        // HEIGHTS
        // ==========================================

        float edgeY =
            bounds.max.y +
            frameHeight * 0.5f;


        float cornerY =
            bounds.max.y +
            cornerHeight * 0.5f;


        // ==========================================
        // FRAME OFFSET
        // ==========================================

        float edgeOffset =
            frameWidth * 0.5f +
            gap;


        // ==========================================
        // TOP
        // ==========================================

        if (top != null)
        {
            top.position =
                new Vector3(
                    center.x,
                    edgeY,
                    bounds.max.z + edgeOffset
                );

            top.rotation = Quaternion.Euler(0f, 0f, 0f);


            top.localScale =
     new Vector3(
         groundWidth + cornerSize,
         frameHeight,
         frameWidth
     );
        }


        // ==========================================
        // BOTTOM
        // ==========================================

        if (bottom != null)
        {
            bottom.position =
                new Vector3(
                    center.x,
                    edgeY,
                    bounds.min.z - edgeOffset
                );

            bottom.rotation = Quaternion.Euler(0f, 180f, 0f);

            bottom.localScale =
    new Vector3(
        groundWidth + cornerSize,
        frameHeight,
        frameWidth
    );
        }


        // ==========================================
        // LEFT
        // ==========================================

        if (left != null)
        {
            left.position =
                new Vector3(
                    bounds.min.x - edgeOffset,
                    edgeY,
                    center.z
                );
            left.rotation = Quaternion.Euler(0f, 0f, 90f);


            left.localScale =
     new Vector3(
         frameHeight,
         frameWidth,
         groundDepth + cornerSize
         
         
     );
        }


        // ==========================================
        // RIGHT
        // ==========================================

        if (right != null)
        {
            right.position =
                new Vector3(
                    bounds.max.x + edgeOffset,
                    edgeY,
                    center.z
                );
            right.rotation = Quaternion.Euler(0f, 0f, 90f);

            right.localScale =
    new Vector3(
        frameHeight,
        frameWidth,
        groundDepth + cornerSize
    );
        }


        // ==========================================
        // CORNERS
        // ==========================================

        float cornerOffset =
            cornerSize * 0.5f +
            gap;


        // TOP LEFT
        if (cornerTL != null)
        {
            cornerTL.position =
                new Vector3(
                    bounds.min.x - cornerOffset,
                    cornerY,
                    bounds.max.z + cornerOffset
                );
        }


        // TOP RIGHT
        if (cornerTR != null)
        {
            cornerTR.position =
                new Vector3(
                    bounds.max.x + cornerOffset,
                    cornerY,
                    bounds.max.z + cornerOffset
                );
        }


        // BOTTOM LEFT
        if (cornerBL != null)
        {
            cornerBL.position =
                new Vector3(
                    bounds.min.x - cornerOffset,
                    cornerY,
                    bounds.min.z - cornerOffset
                );
        }


        // BOTTOM RIGHT
        if (cornerBR != null)
        {
            cornerBR.position =
                new Vector3(
                    bounds.max.x + cornerOffset,
                    cornerY,
                    bounds.min.z - cornerOffset
                );
        }
    }


    // Lets you test resizing from the Inspector.
    [ContextMenu("Resize Board Frame")]
    private void ResizeFromInspector()
    {
        Resize();
    }
}