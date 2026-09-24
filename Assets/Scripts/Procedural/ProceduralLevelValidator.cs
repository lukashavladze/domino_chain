using System.Collections.Generic;
using UnityEngine;

public class ProceduralLevelValidator : MonoBehaviour
{
    [Header("Validation")]
    [Tooltip("Extra distance around dominoes used when checking line blocking.")]
    public float clearance = 0.08f;

    /// <summary>
    /// Returns true when every line can eventually be played.
    /// </summary>
    public bool IsLevelSolvable(List<DominoLine> lines)
    {
        if (lines == null || lines.Count == 0)
            return false;

        HashSet<DominoLine> remaining =
            new HashSet<DominoLine>(lines);

        int safety = 0;

        while (remaining.Count > 0)
        {
            safety++;

            if (safety > 1000)
            {
                Debug.LogError("Solver safety limit reached.");
                return false;
            }

            List<DominoLine> playable =
                new List<DominoLine>();

            foreach (DominoLine line in remaining)
            {
                bool blocked = false;

                foreach (DominoLine blocker in line.blockedByLines)
                {
                    if (blocker != null &&
                        remaining.Contains(blocker))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                    playable.Add(line);
            }

            // Nobody can move -> circular dependency / deadlock.
            if (playable.Count == 0)
            {
                Debug.LogWarning(
                    "Generated level is UNSOLVABLE. " +
                    "Circular blocking detected.");

                return false;
            }

            foreach (DominoLine line in playable)
                remaining.Remove(line);
        }

        return true;
    }
}