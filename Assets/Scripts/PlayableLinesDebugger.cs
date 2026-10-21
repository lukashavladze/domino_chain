
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlayableLinesDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebug = true;
    public bool showBlockedLines = true;
    public bool showLineNumbers = true;
    public bool showBlockerConnections = false;

    [Header("Visual Settings")]
    public float markerRadius = 0.20f;
    public float markerHeight = 1.4f;

    private void OnDrawGizmos()
    {
        if (!showDebug)
            return;

        DominoLine[] lines =
            GetComponentsInChildren<DominoLine>(true);

        foreach (DominoLine line in lines)
        {
            if (line == null ||
                line.dominoes == null ||
                line.dominoes.Count == 0)
                continue;

            Domino first = line.dominoes[0];

            if (first == null)
                continue;

            bool started = line.HasStartedLine;

            bool blocked = false;

            if (!started)
            {
                foreach (DominoLine blocker in line.blockedByLines)
                {
                    if (blocker != null &&
                        !blocker.HasStartedLine)
                    {
                        blocked = true;
                        break;
                    }
                }
            }

            if (blocked && !showBlockedLines)
                continue;

            Color color = started
                ? Color.yellow
                : blocked
                    ? Color.red
                    : Color.green;

            Vector3 position =
                first.transform.position +
                Vector3.up * markerHeight;

            Gizmos.color = color;
            Gizmos.DrawSphere(position, markerRadius);

            Gizmos.DrawLine(
                position,
                first.transform.position);

#if UNITY_EDITOR
            if (showLineNumbers)
            {
                string status = started
                    ? "STARTED"
                    : blocked
                        ? "BLOCKED"
                        : "PLAYABLE";

                Handles.color = color;

                Handles.Label(
                    position + Vector3.up * 0.3f,
                    line.name + "\n" + status);
            }
#endif

            if (showBlockerConnections)
            {
                foreach (DominoLine blocker in line.blockedByLines)
                {
                    if (blocker == null ||
                        blocker.dominoes == null ||
                        blocker.dominoes.Count == 0)
                        continue;

                    Domino blockerFirst =
                        blocker.dominoes[0];

                    if (blockerFirst == null)
                        continue;

                    Gizmos.color = Color.magenta;

                    Gizmos.DrawLine(
                        position,
                        blockerFirst.transform.position +
                        Vector3.up * markerHeight);
                }
            }
        }
    }
}
