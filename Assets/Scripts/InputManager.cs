using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    private Camera cam;


    private void Awake()
    {
        cam = Camera.main;
    }


    private void Update()
    {
        // No gameplay input after Game Over.
        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameOver)
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
        Ray ray =
            cam.ScreenPointToRay(
                screenPosition
            );


        RaycastHit[] hits =
            Physics.RaycastAll(ray);


        if (hits == null ||
            hits.Length == 0)
        {
            return;
        }


        // RaycastAll does not guarantee distance order.
        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );


        foreach (RaycastHit hit in hits)
        {
            Domino domino =
                hit.collider
                    .GetComponentInParent<Domino>();


            if (domino == null)
                continue;


            // Ignore dominoes that already started.
            if (domino.HasStarted)
                continue;


            // Ignore dominoes that are no longer standing.
            if (!domino.IsStanding())
                continue;


            DominoLine line =
                domino.ownerLine;


            if (line == null)
                continue;


            // ==========================================
            // ONLY STARTER DOMINOES ARE GAMEPLAY INPUT
            // ==========================================

            if (domino != line.firstDomino)
            {
                return;
            }


            // ==========================================
            // ATTEMPT TO START THIS LINE
            // ==========================================

            bool started =
                line.TryStartLine();


            if (started)
            {
                // Correct move.
                return;
            }


            // ==========================================
            // WRONG MOVE
            // ==========================================
            //
            // Player clicked a valid starter domino,
            // but the line is currently blocked.
            //

            if (GameManager.Instance != null)
            {
                GameManager.Instance
                    .RegisterWrongMove();
            }


            return;
        }
    }
}