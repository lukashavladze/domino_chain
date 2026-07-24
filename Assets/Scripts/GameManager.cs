using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    DominoLine[] lines;

    void Awake()
    {
        lines = FindObjectsByType<DominoLine>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            foreach (DominoLine line in lines)
            {
                line.ResetLine();
            }
        }
    }
}