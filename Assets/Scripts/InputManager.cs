using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null)
            return;

        // Mouse
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHit(Mouse.current.position.ReadValue());
        }

        // Touch
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                TryHit(touch.position.ReadValue());
            }
        }
    }

    void TryHit(Vector2 screenPosition)
    {
        Ray ray =
            cam.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            return;
        }

        Domino domino =
            hit.collider.GetComponentInParent<Domino>();

        if (domino == null)
            return;

        DominoLine line =
            domino.ownerLine;

        if (line == null)
            return;

        // Only the FIRST domino is allowed to be clicked.
        if (domino != line.firstDomino)
            return;

        // This safely checks blocking/state at the exact
        // moment the player clicks.
        line.TryStartLine();
    }
}