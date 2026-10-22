
using System.Collections.Generic;
using UnityEngine;

public static class PuzzleDifficultyAnalyzer
{
    public struct Result
    {
        public int totalLines;
        public int initialPlayable;
        public int blockedLines;
        public int blockerLinks;
        public int multiBlockedLines;
        public int maxDepth;
        public float averageUnlocks;
        public bool solvable;
    }

    public static Result Analyze(List<DominoLine> lines)
    {
        Result result = new Result();

        if (lines == null || lines.Count == 0)
            return result;

        HashSet<DominoLine> valid =
            new HashSet<DominoLine>();

        foreach (var line in lines)
        {
            if (line != null)
                valid.Add(line);
        }

        result.totalLines = valid.Count;

        Dictionary<DominoLine, HashSet<DominoLine>> dependencies =
            new Dictionary<DominoLine, HashSet<DominoLine>>();

        foreach (var line in valid)
        {
            var blockers = new HashSet<DominoLine>();

            if (line.blockedByLines != null)
            {
                foreach (var blocker in line.blockedByLines)
                {
                    if (blocker != null &&
                        blocker != line &&
                        valid.Contains(blocker))
                    {
                        blockers.Add(blocker);
                    }
                }
            }

            dependencies[line] = blockers;

            result.blockerLinks += blockers.Count;

            if (blockers.Count == 0)
                result.initialPlayable++;
            else
                result.blockedLines++;

            if (blockers.Count >= 2)
                result.multiBlockedLines++;
        }

        var completed = new HashSet<DominoLine>();
        var depths = new Dictionary<DominoLine, int>();

        int totalNewUnlocks = 0;
        int moves = 0;

        // Simulate a deterministic valid topological order.
        while (completed.Count < valid.Count)
        {
            var available = new List<DominoLine>();

            foreach (var line in valid)
            {
                if (completed.Contains(line))
                    continue;

                if (completed.IsSupersetOf(dependencies[line]))
                    available.Add(line);
            }

            if (available.Count == 0)
            {
                result.solvable = false;
                return result;
            }

            // Choose a stable order based on input list.
            DominoLine chosen = null;

            foreach (var line in lines)
            {
                if (line != null && available.Contains(line))
                {
                    chosen = line;
                    break;
                }
            }

            int depth = 1;

            foreach (var blocker in dependencies[chosen])
            {
                depth = Mathf.Max(
                    depth, depths[blocker] + 1);
            }

            depths[chosen] = depth;
            result.maxDepth = Mathf.Max(result.maxDepth, depth);

            int availableBefore = available.Count;

            completed.Add(chosen);
            moves++;

            int availableAfter = 0;

            foreach (var line in valid)
            {
                if (!completed.Contains(line) &&
                    completed.IsSupersetOf(dependencies[line]))
                {
                    availableAfter++;
                }
            }

            int newlyUnlocked = Mathf.Max(
                0, availableAfter - (availableBefore - 1));

            totalNewUnlocks += newlyUnlocked;
        }

        result.averageUnlocks =
            moves > 0
                ? (float)totalNewUnlocks / moves
                : 0f;

        result.solvable = true;
        return result;
    }
}
