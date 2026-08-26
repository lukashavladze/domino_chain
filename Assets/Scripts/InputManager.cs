using UnityEngine;
using UnityEngine.InputSystem;
using System;

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
        Ray ray = cam.ScreenPointToRay(screenPosition);

        RaycastHit[] hits = Physics.RaycastAll(ray);

        if (hits == null || hits.Length == 0)
            return;

        // RaycastAll does not guarantee distance order.
        Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            Domino domino =
                hit.collider.GetComponentInParent<Domino>();

            if (domino == null)
                continue;

            // Ignore dominoes already falling / fallen.
            if (domino.HasStarted)
                continue;

            // Ignore dominoes that are no longer standing.
            if (!domino.IsStanding())
                continue;

            DominoLine line = domino.ownerLine;

            if (line == null)
                continue;

            // Only first domino starts a line.
            if (domino != line.firstDomino)
                continue;

            // TryStartLine performs the final blocking check.
            if (line.TryStartLine())
                return;
        }
    }
}