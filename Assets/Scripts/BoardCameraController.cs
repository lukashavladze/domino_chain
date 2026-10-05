using UnityEngine;
using UnityEngine.InputSystem;

public class BoardCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform ground;

    [Header("Pan")]
    [SerializeField] private float mousePanSpeed = 0.01f;
    [SerializeField] private float touchPanSpeed = 0.01f;
    [SerializeField] private float keyboardPanSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float mouseZoomSpeed = 2f;
    [SerializeField] private float touchZoomSpeed = 0.01f;

    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 30f;

    [Header("Movement Bounds")]
    [SerializeField] private float extraMovementMargin = 1f;

    private Vector3 initialCameraLocalPosition;

    private Vector2 lastMousePosition;
    private bool mouseDragging;

    private float lastTouchDistance;


    [Header("90 Degree Orbit")]
    [SerializeField] private float rotationDuration = 0.35f;

    private bool isRotating;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            initialCameraLocalPosition =
                targetCamera.transform.localPosition;
        }
    }
    // თუ გვინდა იმოძრაოს გვერდებში და ზევით ქვევით ბოარდ-მა ეს აფდეითი გავააქტიუროთ და მეორე გავაუქმოთ
    //private void Update()
    //{
    //    HandleKeyboard();
    //    HandleMouse();
    //    HandleTouch();

    //    ClampPosition();
    //}

    private void Update()
    {
        HandleRotationInput();
        HandleZoom();
    }

    private void HandleRotationInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            RotateLeft();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            RotateRight();
        }
    }

    // =========================================================
    // KEYBOARD
    // =========================================================
    private void HandleKeyboard()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            RotateLeft();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            RotateRight();
        }

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            move += Vector3.forward;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            move += Vector3.back;
        }

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            move += Vector3.left;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            move += Vector3.right;
        }

        if (move.sqrMagnitude > 0f)
        {
            transform.position +=
                move.normalized *
                keyboardPanSpeed *
                Time.deltaTime;
        }
    }

    // =========================================================
    // MOUSE
    // =========================================================

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        // -------------------------
        // DRAG
        // -------------------------

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseDragging = true;
            lastMousePosition =
                Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            mouseDragging = false;
        }

        if (mouseDragging)
        {
            Vector2 current =
                Mouse.current.position.ReadValue();

            Vector2 delta =
                current - lastMousePosition;

            PanFromScreenDelta(
                delta,
                mousePanSpeed);

            lastMousePosition = current;
        }
    }

    private void HandleZoom()
    {
        if (targetCamera == null)
            return;

        // ==========================
        // MOUSE WHEEL
        // ==========================

        if (Mouse.current != null)
        {
            float scroll =
                Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                float direction =
                    Mathf.Sign(scroll);

                ZoomOrthographic(
                    -direction * mouseZoomSpeed
                );
            }
        }

        // ==========================
        // KEYBOARD + / -
        // ==========================

        if (Keyboard.current != null)
        {
            if (Keyboard.current.equalsKey.wasPressedThisFrame ||
                Keyboard.current.numpadPlusKey.wasPressedThisFrame)
            {
                ZoomOrthographic(-mouseZoomSpeed);
            }

            if (Keyboard.current.minusKey.wasPressedThisFrame ||
                Keyboard.current.numpadMinusKey.wasPressedThisFrame)
            {
                ZoomOrthographic(mouseZoomSpeed);
            }
        }
    }

    private void ZoomIn()

    {
        if (targetCamera == null)
            return;

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize =
                Mathf.Clamp(
                    targetCamera.orthographicSize - mouseZoomSpeed,
                    minZoom,
                    maxZoom
                );
        }
    }

    private void ZoomOut()
    {
        if (targetCamera == null)
            return;

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize =
                Mathf.Clamp(
                    targetCamera.orthographicSize + mouseZoomSpeed,
                    minZoom,
                    maxZoom
                );
        }
    }

    // =========================================================
    // TOUCH
    // =========================================================

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        var touchscreen = Touchscreen.current;

        bool touch0 =
            touchscreen.touches[0].press.isPressed;

        bool touch1 =
            touchscreen.touches[1].press.isPressed;

        // =====================================================
        // TWO FINGERS = ZOOM
        // =====================================================

        if (touch0 && touch1)
        {
            Vector2 p0 =
                touchscreen.touches[0].position.ReadValue();

            Vector2 p1 =
                touchscreen.touches[1].position.ReadValue();

            float distance =
                Vector2.Distance(p0, p1);

            if (lastTouchDistance > 0f)
            {
                float difference =
                    distance - lastTouchDistance;

                ZoomOrthographic(
                    -difference *
                    touchZoomSpeed);
            }

            lastTouchDistance = distance;

            return;
        }

        lastTouchDistance = 0f;

        // =====================================================
        // ONE FINGER = PAN
        // =====================================================

        if (touch0)
        {
            Vector2 delta =
                touchscreen.touches[0]
                    .delta
                    .ReadValue();

            PanFromScreenDelta(
                delta,
                touchPanSpeed);
        }
    }

    // =========================================================
    // PAN
    // =========================================================

    private void PanFromScreenDelta(
        Vector2 delta,
        float speed)
    {
        if (targetCamera == null)
            return;

        Vector3 right =
            targetCamera.transform.right;

        Vector3 forward =
            targetCamera.transform.forward;

        // We only move over the board plane.
        right.y = 0f;
        forward.y = 0f;

        right.Normalize();
        forward.Normalize();

        Vector3 movement =
            (-right * delta.x -
             forward * delta.y) *
            speed;

        transform.position += movement;
    }

    // =========================================================
    // ZOOM
    // =========================================================
    private void ZoomOrthographic(float amount)
    {
        if (targetCamera == null)
            return;

        if (!targetCamera.orthographic)
            return;

        targetCamera.orthographicSize =
            Mathf.Clamp(
                targetCamera.orthographicSize + amount,
                minZoom,
                maxZoom
            );
    }

    // =========================================================
    // MOVEMENT LIMITS
    // =========================================================

    private void ClampPosition()
    {
        if (ground == null)
            return;

        Renderer groundRenderer =
            ground.GetComponent<Renderer>();

        if (groundRenderer == null)
            return;

        Bounds bounds =
            groundRenderer.bounds;

        Vector3 position =
            transform.position;

        position.x =
            Mathf.Clamp(
                position.x,
                bounds.min.x - extraMovementMargin,
                bounds.max.x + extraMovementMargin);

        position.z =
            Mathf.Clamp(
                position.z,
                bounds.min.z - extraMovementMargin,
                bounds.max.z + extraMovementMargin);

        transform.position = position;
    }


    public void RotateLeft()
    {
        if (!isRotating)
            StartCoroutine(OrbitCamera(-90f));
    }

    public void RotateRight()
    {
        if (!isRotating)
            StartCoroutine(OrbitCamera(90f));
    }
    private System.Collections.IEnumerator OrbitCamera(float angle)
    {
        if (targetCamera == null || ground == null)
            yield break;

        isRotating = true;

        Renderer groundRenderer =
            ground.GetComponent<Renderer>();

        if (groundRenderer == null)
            groundRenderer =
                ground.GetComponentInChildren<Renderer>();

        if (groundRenderer == null)
        {
            isRotating = false;
            yield break;
        }

        // Board center in WORLD space.
        Vector3 center = groundRenderer.bounds.center;

        // Keep orbit horizontal.
        Vector3 startPosition = transform.position;

        Vector3 horizontalOffset =
            new Vector3(
                startPosition.x - center.x,
                0f,
                startPosition.z - center.z
            );

        // Rotate position exactly 90 degrees around board.
        Vector3 rotatedOffset =
            Quaternion.AngleAxis(
                angle,
                Vector3.up
            ) * horizontalOffset;

        Vector3 targetPosition =
            new Vector3(
                center.x + rotatedOffset.x,
                startPosition.y,
                center.z + rotatedOffset.z
            );

        // IMPORTANT:
        // Preserve existing X tilt.
        Vector3 startEuler = transform.eulerAngles;

        float targetY =
            startEuler.y + angle;

        Quaternion startRotation =
            transform.rotation;

        Quaternion targetRotation =
            Quaternion.Euler(
                startEuler.x,
                targetY,
                startEuler.z
            );

        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / rotationDuration
                );

            // Smooth start/end.
            t = t * t * (3f - 2f * t);

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        isRotating = false;
    }
}