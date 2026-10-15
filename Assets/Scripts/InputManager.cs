using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera cam;


    private void Update()
    {
        // No gameplay input after Game Over.
        if (GameManager.Instance != null &&
    (GameManager.Instance.IsGameOver ||
     GameManager.Instance.IsLevelCompleted))
        {
            return;
        }


        if (cam == null)
            return;


        // ================================
        // MOUSE
        // ================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHit(
                Mouse.current.position.ReadValue()
            );
        }


        // ================================
        // TOUCH
        // ================================

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                TryHit(
                    touch.position.ReadValue()
                );
            }
        }
    }



    private void TryHit(Vector2 screenPosition)
    {
        Ray ray = cam.ScreenPointToRay(screenPosition);

        RaycastHit[] hits = Physics.RaycastAll(ray);

        Array.Sort(hits, (a, b) =>
            a.distance.CompareTo(b.distance));

        Debug.Log($"RAYCAST HITS: {hits.Length}");

        foreach (RaycastHit hit in hits)
        {
            Domino domino =
                hit.collider.GetComponentInParent<Domino>();

            if (domino == null)
                continue;

            DominoLine line = domino.ownerLine;

            Debug.Log(
                $"HIT: {domino.name} | " +
                $"HasStarted: {domino.HasStarted} | " +
                $"Standing: {domino.IsStanding()} | " +
                $"OwnerLine: {(line != null ? line.name : "NULL")} | " +
                $"IsFirst: {(line != null && domino == line.firstDomino)} | " +
                $"CanStartChain: {domino.canStartChain}"
            );

            if (domino.HasStarted)
                continue;

            if (!domino.IsStanding())
                continue;

            if (line == null)
                continue;

            if (domino != line.firstDomino)
            {
                Debug.Log("IGNORED: Not first domino");
                return;
            }

            Debug.Log("ATTEMPTING TO START LINE");

            bool started = line.TryStartLine();

            Debug.Log("START RESULT: " + started);

            if (started)
            {
                HintManager hint =
                    FindFirstObjectByType<HintManager>();

                if (hint != null)
                    hint.HideHint();

                return;
            }

            Debug.Log("WRONG MOVE - REGISTERING LIFE LOSS");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterWrongMove();
            }
            else
            {
                Debug.LogError("GameManager.Instance is NULL");
            }

            return;
        }
    }

}