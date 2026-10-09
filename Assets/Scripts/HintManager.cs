
using UnityEngine;

public class HintManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform handIndicator;

    [Header("Animation")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 65);
    [SerializeField] private float bounceAmount = 12f;
    [SerializeField] private float bounceSpeed = 4f;

    private Domino targetDomino;
    private RectTransform handParent;
    private Camera uiCamera;

    private void Awake()
    {
        if (canvas != null)
        {
            handParent = handIndicator != null
                ? handIndicator.parent as RectTransform
                : null;

            uiCamera = canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
        }

        HideHint();
    }

    private void LateUpdate()
    {
        if (targetDomino == null)
            return;

        if (GameManager.Instance != null &&
            (GameManager.Instance.IsGameOver ||
             GameManager.Instance.IsLevelCompleted))
        {
            HideHint();
            return;
        }

        DominoLine line = targetDomino.ownerLine;

        // The selected line was started or became blocked.
        if (line == null ||
            line.HasStartedLine ||
            !targetDomino.IsStanding())
        {
            HideHint();
            return;
        }

        UpdateHandPosition();
    }

    public void ShowNextHint()
    {
        HideHint();

        if (GameManager.Instance != null &&
            (GameManager.Instance.IsGameOver ||
             GameManager.Instance.IsLevelCompleted))
            return;

        if (gameCamera == null ||
            canvas == null ||
            handIndicator == null ||
            handParent == null)
        {
            Debug.LogWarning("HintManager references missing.");
            return;
        }

        DominoLine[] lines =
            FindObjectsByType<DominoLine>(
                FindObjectsSortMode.None);

        foreach (DominoLine line in lines)
        {
            if (line == null || line.firstDomino == null)
                continue;

            Domino first = line.firstDomino;

            if (first.HasStarted || !first.IsStanding())
                continue;

            // Uses your actual gameplay availability logic.
            if (!line.CanStartLine())
                continue;

            Vector3 screenPoint =
                gameCamera.WorldToScreenPoint(
                    first.transform.position);

            if (screenPoint.z <= 0)
                continue;

            targetDomino = first;
            handIndicator.gameObject.SetActive(true);
            UpdateHandPosition();

            Debug.Log("HINT: " + line.name);
            return;
        }

        Debug.Log("No available domino line for hint.");
    }

    private void UpdateHandPosition()
    {
        if (targetDomino == null ||
            handIndicator == null ||
            handParent == null)
            return;

        Vector3 screenPoint =
            gameCamera.WorldToScreenPoint(
                targetDomino.transform.position);

        if (screenPoint.z <= 0)
        {
            HideHint();
            return;
        }

        Vector2 localPoint;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                handParent,
                screenPoint,
                uiCamera,
                out localPoint))
            return;

        float bounce =
            Mathf.Sin(Time.unscaledTime * bounceSpeed)
            * bounceAmount;

        Vector2 offset =
            screenOffset + new Vector2(0, bounce);

        // Offset is in screen pixels, convert to UI units.
        float scale = canvas.scaleFactor;
        if (scale > 0)
            offset /= scale;

        handIndicator.anchoredPosition =
            localPoint + offset;
    }

    public void HideHint()
    {
        targetDomino = null;

        if (handIndicator != null)
            handIndicator.gameObject.SetActive(false);
    }
}
