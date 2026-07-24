using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public DominoLine line;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            line.StartLine();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            line.ResetLine();
        }
    }
}