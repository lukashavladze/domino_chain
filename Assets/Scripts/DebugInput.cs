using UnityEngine;
using UnityEngine.InputSystem;

public class DebugInput : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null)
            return;


        // ==========================================
        // R = RESET DOMINO LINES
        // ==========================================

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            DominoLine[] lines =
                FindObjectsByType<DominoLine>(
                    FindObjectsSortMode.None
                );

            foreach (DominoLine line in lines)
            {
                line.ResetLine();
            }

            // Also reset reveal mask.
            if (RevealPainter.Instance != null)
            {
                RevealPainter.Instance.ResetMask();
            }
        }


        // ==========================================
        // T = TEST FINAL RADIAL REVEAL
        // ==========================================

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (RevealPainter.Instance != null)
            {
                StartCoroutine(
                    RevealPainter.Instance.RevealAllRadial()
                );
            }
        }
    }
}