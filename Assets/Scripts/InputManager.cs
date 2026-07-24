using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        Debug.Log("InputManager Awake");
        cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null)
        {
            Debug.Log("Mouse is NULL");
            return;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Debug.Log("Mouse Held");
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Mouse Down");
        }
    }
}
