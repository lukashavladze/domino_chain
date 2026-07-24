using UnityEngine;
using UnityEngine.InputSystem;

public class DebugInput : MonoBehaviour
{
    void Update()
    {
        if (!Keyboard.current.rKey.wasPressedThisFrame)
            return;

        DominoLine[] lines = FindObjectsByType<DominoLine>(FindObjectsSortMode.None);

        foreach (DominoLine line in lines)
        {
            line.ResetLine();
        }
    }
}