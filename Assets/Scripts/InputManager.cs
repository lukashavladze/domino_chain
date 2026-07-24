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
        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Domino domino = hit.collider.GetComponent<Domino>();

        if (domino == null)
            return;

        if (!domino.canStartChain)
            return;

        domino.StartChain();

        //Vector3 direction =
        //    domino.transform.forward;

        //domino.Fall(direction);
    }
}