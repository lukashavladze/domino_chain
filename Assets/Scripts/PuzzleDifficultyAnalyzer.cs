
using System.Collections.Generic;
using UnityEngine;

public static class PuzzleDifficultyAnalyzer
{
    public struct Result
    {
        // General
        public int totalLines;
        public int initialPlayable;
        public int blockedLines;
        public int blockerLinks;
        public int multiBlockedLines;
        public bool solvable;

        // Blocker statistics
        public float averageBlockersPerBlockedLine;
        public int maximumBlockersOnLine;

        // Dependency depth
        // maxDepth counts lines in the longest chain.
        // maximumPrerequisiteSteps counts edges in that chain.
        public int maxDepth;
        public int maximumPrerequisiteSteps;
        public float averagePrerequisiteSteps;

        // Unlock statistics
        public float averageUnlocks;
        public float averageUnlocksAllMoves;
        public int maximumUnlocksFromOneMove;
        public int productiveMoves;
        public int totalUnlocks;
    }

    public static Result Analyze(List<DominoLine> lines)
    {
        Result result = new Result();

        if (lines == null || lines.Count == 0)
            return result;

        // =====================================================
        // 1. COLLECT VALID, UNIQUE LINES
        // =====================================================

        List<DominoLine> valid =
            new List<DominoLine>();

        HashSet<DominoLine> validSet =
            new HashSet<DominoLine>();

        foreach (DominoLine line in lines)
        {
            if (line != null && validSet.Add(line))
                valid.Add(line);
        }

        result.totalLines = valid.Count;

        if (valid.Count == 0)
            return result;

        // =====================================================
        // 2. BUILD DEPENDENCY GRAPH
        // =====================================================

        Dictionary<DominoLine, List<DominoLine>> dependents =
            new Dictionary<DominoLine, List<DominoLine>>();

        Dictionary<DominoLine, int> remainingBlockers =
            new Dictionary<DominoLine, int>();

        Dictionary<DominoLine, int> depths =
            new Dictionary<DominoLine, int>();

        foreach (DominoLine line in valid)
        {
            dependents[line] = new List<DominoLine>();
            depths[line] = 1;
        }

        foreach (DominoLine line in valid)
        {
            HashSet<DominoLine> blockers =
                new HashSet<DominoLine>();

            if (line.blockedByLines != null)
            {
                foreach (DominoLine blocker in line.blockedByLines)
                {
                    if (blocker == null ||
                        blocker == line ||
                        !validSet.Contains(blocker))
                    {
                        continue;
                    }

                    blockers.Add(blocker);
                }
            }

            int blockerCount = blockers.Count;

            remainingBlockers[line] = blockerCount;

            result.blockerLinks += blockerCount;

            result.maximumBlockersOnLine =
                Mathf.Max(
                    result.maximumBlockersOnLine,
                    blockerCount);

            if (blockerCount == 0)
            {
                result.initialPlayable++;
            }
            else
            {
                result.blockedLines++;

                if (blockerCount >= 2)
                    result.multiBlockedLines++;
            }

            foreach (DominoLine blocker in blockers)
            {
                dependents[blocker].Add(line);
            }
        }

        // =====================================================
        // 3. BLOCKER STATISTICS
        // =====================================================

        result.averageBlockersPerBlockedLine =
            result.blockedLines > 0
                ? (float)result.blockerLinks /
                  result.blockedLines
                : 0f;

        // =====================================================
        // 4. FIND INITIAL PLAYABLE LINES
        // =====================================================

        Queue<DominoLine> ready =
            new Queue<DominoLine>();

        foreach (DominoLine line in valid)
        {
            if (remainingBlockers[line] == 0)
                ready.Enqueue(line);
        }

        // =====================================================
        // 5. SIMULATE A VALID UNLOCK ORDER
        // =====================================================

        int completedCount = 0;
        int totalPrerequisiteSteps = 0;

        while (ready.Count > 0)
        {
            DominoLine chosen = ready.Dequeue();

            completedCount++;

            int newlyUnlocked = 0;

            foreach (DominoLine dependent in dependents[chosen])
            {
                remainingBlockers[dependent]--;

                int nextDepth =
                    depths[chosen] + 1;

                depths[dependent] =
                    Mathf.Max(
                        depths[dependent],
                        nextDepth);

                if (remainingBlockers[dependent] == 0)
                {
                    ready.Enqueue(dependent);
                    newlyUnlocked++;
                }
            }

            // Longest dependency chain
            result.maxDepth =
                Mathf.Max(
                    result.maxDepth,
                    depths[chosen]);

            totalPrerequisiteSteps +=
                Mathf.Max(0, depths[chosen] - 1);

            // Newly playable lines after this move
            result.totalUnlocks += newlyUnlocked;

            result.maximumUnlocksFromOneMove =
                Mathf.Max(
                    result.maximumUnlocksFromOneMove,
                    newlyUnlocked);

            if (newlyUnlocked > 0)
                result.productiveMoves++;
        }

        // =====================================================
        // 6. SOLVABILITY
        // =====================================================

        result.solvable =
            completedCount == valid.Count;

        if (!result.solvable)
        {
            // These values are undefined for a graph
            // that cannot be completely resolved.
            result.averageUnlocks = 0f;
            result.averageUnlocksAllMoves = 0f;
            result.averagePrerequisiteSteps = 0f;
            result.maximumPrerequisiteSteps = 0;

            return result;
        }

        // =====================================================
        // 7. DEPENDENCY DEPTH STATISTICS
        // =====================================================

        result.maximumPrerequisiteSteps =
            Mathf.Max(0, result.maxDepth - 1);

        result.averagePrerequisiteSteps =
            result.totalLines > 0
                ? (float)totalPrerequisiteSteps /
                  result.totalLines
                : 0f;

        // =====================================================
        // 8. UNLOCK STATISTICS
        // =====================================================

        // Average newly unlocked lines among moves
        // that unlock at least one line.
        result.averageUnlocks =
            result.productiveMoves > 0
                ? (float)result.totalUnlocks /
                  result.productiveMoves
                : 0f;

        // Average newly unlocked lines across ALL moves,
        // including moves that unlock nothing.
        result.averageUnlocksAllMoves =
            result.totalLines > 0
                ? (float)result.totalUnlocks /
                  result.totalLines
                : 0f;

        return result;
    }
}
