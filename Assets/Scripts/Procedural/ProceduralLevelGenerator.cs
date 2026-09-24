using System.Collections.Generic;
using UnityEngine;

public class ProceduralLevelGenerator : MonoBehaviour
{

    private enum LineDirection
    {
        LeftToRight,
        RightToLeft,

        BottomToTop,
        TopToBottom,

        BottomLeftToTopRight,
        TopRightToBottomLeft,

        TopLeftToBottomRight,
        BottomRightToTopLeft
    }
    // =========================================================
    // LEVEL
    // =========================================================

    [Header("Level")]
    [Min(1)]
    public int levelNumber = 1;

    [Tooltip("Same level + same seed produces the same puzzle.")]
    public int baseSeed = 12345;


    private struct DominoFallArea
    {
        public Vector3 start;
        public Vector3 end;
        public float radius;
        public Vector3 direction;
    }


    [Header("Line End Validation")]

    [Tooltip("Multiplier applied to domino height to determine maximum fall reach.")]
    [SerializeField]
    [Range(0.5f, 1.5f)]
    private float endFallReachMultiplier = 1.0f;

    [Tooltip("Extra width around the falling last domino.")]
    [SerializeField]
    [Range(0f, 0.2f)]
    private float endFallPadding = 0.04f;

    [Tooltip("Reject two line ends when their fall corridors overlap.")]
    [SerializeField]
    private bool rejectOpposingEnds = true;


    [Header("Physical Line Clearance")]

    [Tooltip("Minimum world-space distance between dominoes belonging to different lines.")]
    [SerializeField]
    private float minimumDominoClearance = 0.05f;

    // =========================================================
    // PREFABS
    // =========================================================

    [Header("Prefabs")]

    public Domino dominoPrefab;

    [Tooltip(
        "Prefab containing DominoLine. " +
        "It should NOT contain domino children."
    )]
    public DominoLine linePrefab;


    [Header("Validation")]
    public DominoBlockingCalculator blockingCalculator;
    public ProceduralLevelValidator validator;

    public int maxGenerationAttempts = 50;

    // =========================================================
    // BOARD
    // =========================================================

    [Header("Board")]


    [Tooltip("Distance between neighboring domino grid positions.")]
    [Min(0.01f)]
    public float cellSize = 0.25f;

    [Header("Reveal Coverage")]

    [Tooltip("Extra reveal size beyond one grid cell.")]
    [Min(0f)]
    [SerializeField]
    private float revealOverlap = 0.15f;

    [Tooltip("Height of domino pivot above the ground.")]
    public float groundOffset = 0.5f;

    [Min(0f)]
    [SerializeField]
    private float boardEdgeMargin = 0.25f;


    // =========================================================
    // BOARD GROWTH
    // =========================================================

    [Header("Board Growth")]

    public Transform groundTransform;
    public Renderer groundRenderer;

    public Vector2 startingGroundSize =
        new Vector2(6f, 4f);

    public Vector2 maximumGroundSize =
        new Vector2(11f, 7f);

    [Min(1)]
    public int levelsPerSizeIncrease = 5;

    public Vector2 sizeIncreasePerStep =
        new Vector2(0.5f, 0.3f);


    // =========================================================
    // PUZZLE GENERATION
    // =========================================================

    [Header("Puzzle Generation")]

    [Tooltip("Minimum dominoes in one generated line.")]
    [Min(2)]
    [SerializeField]
    private int minDominoesPerLine = 8;

    [Tooltip("Maximum dominoes in one generated line.")]
    [Min(2)]
    [SerializeField]
    private int maxDominoesPerLine = 16;

    [Tooltip(
        "Distance between rows in grid cells. " +
        "2 means every second row receives dominoes."
    )]
    [Min(1)]
    [SerializeField]
    private int rowSpacing = 2;

    [Header("Direction Generation")]

    [Range(0f, 1f)]
    [SerializeField]
    private float targetBoardFill = 0.75f;

    [Min(10)]
    [SerializeField]
    private int maxLineGenerationAttempts = 500;

    [Tooltip("Allow horizontal lines.")]
    [SerializeField]
    private bool allowHorizontalLines = true;

    [Tooltip("Allow vertical lines.")]
    [SerializeField]
    private bool allowVerticalLines = true;

    [Tooltip("Allow 45 degree diagonal lines.")]
    [SerializeField]
    private bool allowDiagonalLines = true;


    // =========================================================
    // BLOCKING
    // =========================================================

    [Header("Blocking")]

    [Tooltip(
        "Chance that a generated line depends on an earlier line."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float blockerChance = 0.75f;

    [Tooltip(
        "Maximum number of prerequisite lines for a generated line."
    )]
    [Range(1, 3)]
    [SerializeField]
    private int maxBlockersPerLine = 1;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Domino Rotation")]

    public Vector3 rotationOffset =
        new Vector3(0f, 90f, 0f);


    // =========================================================
    // RUNTIME DEBUG
    // =========================================================

    [Header("Runtime Debug")]

    [SerializeField]
    private int generatedDominoCount;

    [SerializeField]
    private int generatedLineCount;


    // =========================================================
    // INTERNAL DATA
    // =========================================================

    private GridBoard board;

    private Transform generatedRoot;

    private readonly List<DominoLine> generatedLines =
        new List<DominoLine>();


    // =========================================================
    // PUBLIC GENERATE
    // =========================================================

    [Header("Line Spacing")]

    [Tooltip(
    "Empty grid cells between separate DominoLines."
)]
    [Range(0, 3)]
    [SerializeField]
    private int segmentGapCells = 1;

    public void GenerateLevel()
    {
        if (!ValidateSettings())
            return;

        if (blockingCalculator == null)
        {
            Debug.LogError(
                "DominoBlockingCalculator is not assigned.");

            return;
        }

        if (validator == null)
        {
            Debug.LogError(
                "ProceduralLevelValidator is not assigned.");

            return;
        }

        Vector2 groundSize =
            CalculateGroundSize();

        ResizeGround(groundSize);

        int columns =
            CalculateColumns();

        int rows =
            CalculateRows();

        Vector3 boardCenter =
            groundRenderer != null
                ? groundRenderer.bounds.center
                : transform.position;

        boardCenter.y =
            transform.position.y;

        // =====================================================
        // TRY MULTIPLE GENERATIONS
        // =====================================================

        for (int attempt = 0;
             attempt < maxGenerationAttempts;
             attempt++)
        {
            // ---------------------------------------------
            // CLEAR PREVIOUS ATTEMPT
            // ---------------------------------------------

            ClearLevel();

            // ---------------------------------------------
            // IMPORTANT:
            // Different seed for every retry.
            // ---------------------------------------------

            int seed =
                baseSeed +
                levelNumber * 7919 +
                attempt * 104729;

            Random.InitState(seed);

            // ---------------------------------------------
            // CREATE BOARD
            // ---------------------------------------------

            board =
                new GridBoard(
                    columns,
                    rows,
                    cellSize,
                    boardCenter);

            // ---------------------------------------------
            // CREATE ROOT
            // ---------------------------------------------

            CreateGeneratedRoot();

            generatedDominoCount = 0;
            generatedLineCount = 0;

            generatedLines.Clear();

            // ---------------------------------------------
            // GENERATE GEOMETRY
            // ---------------------------------------------

            GenerateSegmentedCoverage();

            // ---------------------------------------------
            // MAKE SURE CONNECTIONS ARE CURRENT
            // ---------------------------------------------

            foreach (DominoLine line in generatedLines)
            {
                if (line != null)
                    line.AutoConnect();
            }

            // ---------------------------------------------
            // CALCULATE REAL GEOMETRIC BLOCKERS
            // ---------------------------------------------

            blockingCalculator.CalculateBlocking(
                generatedLines);

            // ---------------------------------------------
            // CHECK SOLVABILITY
            // ---------------------------------------------

            bool solvable =
                validator.IsLevelSolvable(
                    generatedLines);

            if (solvable)
            {
                Debug.Log(
                    $"VALID LEVEL {levelNumber} | " +
                    $"Attempt {attempt + 1} | " +
                    $"Ground {groundSize.x:F2} x {groundSize.y:F2} | " +
                    $"Grid {columns} x {rows} | " +
                    $"Lines {generatedLineCount} | " +
                    $"Dominoes {generatedDominoCount} | " +
                    $"Seed {seed}");

                DebugGeneratedDependencies();

                return;
            }

            Debug.LogWarning(
                $"INVALID LEVEL | " +
                $"Attempt {attempt + 1} | " +
                $"Seed {seed} | Regenerating...");
        }

        // =====================================================
        // FAILED ALL ATTEMPTS
        // =====================================================

        ClearLevel();

        Debug.LogError(
            $"FAILED to generate solvable Level {levelNumber} " +
            $"after {maxGenerationAttempts} attempts.");
    }


    private bool HasEnoughPhysicalClearance(
    List<Vector2Int> candidatePath)
    {
        if (candidatePath == null ||
            candidatePath.Count < 2)
        {
            return false;
        }

        // Get collider from domino prefab.
        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
        {
            Debug.LogError(
                "Domino prefab needs a Collider.");

            return false;
        }

        // Minimum allowed distance between
        // centers of dominoes from DIFFERENT lines.
        //
        // cellSize = 0.25
        // gives approximately 0.3125.
        float minimumCenterDistance =
            cellSize * 1.25f;

        float minimumCenterDistanceSqr =
            minimumCenterDistance *
            minimumCenterDistance;

        // =========================================================
        // CHECK EVERY DOMINO POSITION IN CANDIDATE LINE
        // =========================================================

        for (int i = 0; i < candidatePath.Count; i++)
        {
            Vector2Int candidateCell =
                candidatePath[i];

            Vector3 candidatePosition =
                board.CellToWorld(candidateCell);

            candidatePosition.y +=
                groundOffset;

            // -----------------------------------------------------
            // CALCULATE CANDIDATE DOMINO ROTATION
            // -----------------------------------------------------

            Vector3 direction =
                GetPathDirection(
                    candidatePath,
                    i);

            if (direction.sqrMagnitude < 0.001f)
                continue;

            Quaternion candidateRotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up);

            candidateRotation *=
                Quaternion.Euler(
                    rotationOffset);

            // =====================================================
            // COMPARE AGAINST ALL EXISTING LINES
            // =====================================================

            foreach (DominoLine existingLine in generatedLines)
            {
                if (existingLine == null)
                    continue;

                if (existingLine.dominoes == null)
                    continue;

                foreach (Domino existing in existingLine.dominoes)
                {
                    if (existing == null)
                        continue;

                    Vector3 existingPosition =
                        existing.transform.position;

                    // -------------------------------------------------
                    // RULE 1:
                    // MINIMUM CENTER-TO-CENTER DISTANCE
                    // -------------------------------------------------

                    Vector3 difference =
                        candidatePosition -
                        existingPosition;

                    // Ignore height.
                    // We only care about X/Z board distance.
                    difference.y = 0f;

                    if (difference.sqrMagnitude <
                        minimumCenterDistanceSqr)
                    {
                        return false;
                    }

                    // -------------------------------------------------
                    // RULE 2:
                    // ACTUAL ROTATED DOMINO FOOTPRINT
                    // -------------------------------------------------

                    if (FootprintsOverlap(
                        candidatePosition,
                        candidateRotation,
                        existing,
                        prefabCollider))
                    {
                        return false;
                    }
                }
            }
        }

        // No existing domino is too close.
        return true;
    }


    private bool FootprintsOverlap(
    Vector3 candidatePosition,
    Quaternion candidateRotation,
    Domino existing,
    Collider prefabCollider)
    {
        Collider existingCollider =
            existing.GetComponentInChildren<Collider>();

        if (existingCollider == null)
            return false;

        // Local collider dimensions from prefab.
        Vector3 localSize =
            prefabCollider.bounds.size;

        float candidateHalfX =
            localSize.x * 0.5f +
            minimumDominoClearance;

        float candidateHalfZ =
            localSize.z * 0.5f +
            minimumDominoClearance;

        // Candidate's horizontal axes.
        Vector3 candidateRight =
            candidateRotation * Vector3.right;

        Vector3 candidateForward =
            candidateRotation * Vector3.forward;

        candidateRight.y = 0f;
        candidateForward.y = 0f;

        candidateRight.Normalize();
        candidateForward.Normalize();

        // Existing domino's horizontal axes.
        Vector3 existingRight =
            existing.transform.right;

        Vector3 existingForward =
            existing.transform.forward;

        existingRight.y = 0f;
        existingForward.y = 0f;

        existingRight.Normalize();
        existingForward.Normalize();

        Bounds existingBounds =
            existingCollider.bounds;

        float existingHalfX =
            Mathf.Max(
                existingBounds.extents.x,
                existingBounds.extents.z);

        // Conservative radius for existing domino.
        float existingRadius =
            existingHalfX +
            minimumDominoClearance;

        Vector3 difference =
            existing.transform.position -
            candidatePosition;

        difference.y = 0f;

        // Candidate rectangle -> approximate closest point.
        float localX =
            Vector3.Dot(
                difference,
                candidateRight);

        float localZ =
            Vector3.Dot(
                difference,
                candidateForward);

        float closestX =
            Mathf.Clamp(
                localX,
                -candidateHalfX,
                candidateHalfX);

        float closestZ =
            Mathf.Clamp(
                localZ,
                -candidateHalfZ,
                candidateHalfZ);

        Vector3 closestPoint =
            candidatePosition +
            candidateRight * closestX +
            candidateForward * closestZ;

        Vector3 distance =
            existing.transform.position -
            closestPoint;

        distance.y = 0f;

        return distance.sqrMagnitude <
               existingRadius * existingRadius;
    }




    // =========================================================
    // GROUND SIZE
    // =========================================================

    private Vector2 CalculateGroundSize()
    {
        int step =
            (levelNumber - 1) /
            levelsPerSizeIncrease;


        float width =
            startingGroundSize.x +
            step * sizeIncreasePerStep.x;


        float depth =
            startingGroundSize.y +
            step * sizeIncreasePerStep.y;


        width =
            Mathf.Min(
                width,
                maximumGroundSize.x
            );


        depth =
            Mathf.Min(
                depth,
                maximumGroundSize.y
            );


        return new Vector2(
            width,
            depth
        );
    }


    // =========================================================
    // GRID DIMENSIONS
    // =========================================================

    private int CalculateColumns()
    {
        Vector2 groundSize =
            CalculateGroundSize();

        float usableWidth =
            Mathf.Max(
                cellSize,
                groundSize.x -
                boardEdgeMargin * 2f
            );

        return Mathf.Max(
            2,
            Mathf.FloorToInt(
                usableWidth / cellSize
            ) + 1
        );
    }


    private int CalculateRows()
    {
        Vector2 groundSize =
            CalculateGroundSize();

        float usableDepth =
            Mathf.Max(
                cellSize,
                groundSize.y -
                boardEdgeMargin * 2f
            );

        return Mathf.Max(
            2,
            Mathf.FloorToInt(
                usableDepth / cellSize
            ) + 1
        );
    }


    // =========================================================
    // RESIZE GROUND
    // =========================================================

    private void ResizeGround(
        Vector2 targetSize)
    {
        if (groundTransform == null ||
            groundRenderer == null)
        {
            Debug.LogError(
                "Ground Transform or Ground Renderer is missing."
            );

            return;
        }


        Bounds bounds =
            groundRenderer.bounds;


        float currentWidth =
            bounds.size.x;

        float currentDepth =
            bounds.size.z;


        if (currentWidth <= 0.001f ||
            currentDepth <= 0.001f)
        {
            Debug.LogError(
                "Ground has invalid dimensions."
            );

            return;
        }


        Vector3 scale =
            groundTransform.localScale;


        scale.x *=
            targetSize.x /
            currentWidth;


        scale.z *=
            targetSize.y /
            currentDepth;


        groundTransform.localScale =
            scale;
    }


    // =========================================================
    // GENERATE BOARD COVERAGE
    // =========================================================

    private void GenerateSegmentedCoverage()
    {
        int lineIndex = 0;

        int totalCells =
            board.Width * board.Height;

        int targetOccupiedCells =
            Mathf.RoundToInt(
                totalCells * targetBoardFill
            );

        int occupiedCells = 0;

        int attempts = 0;

        while (
            occupiedCells < targetOccupiedCells &&
            attempts < maxLineGenerationAttempts)
        {
            attempts++;

            LineDirection direction =
                GetRandomLineDirection();

            Vector2Int step =
                GetDirectionStep(direction);

            int desiredLength =
                Random.Range(
                    minDominoesPerLine,
                    maxDominoesPerLine + 1
                );

            if (!TryGetRandomStartCell(
        out Vector2Int startCell))
            {
                continue;
            }

            List<Vector2Int> path =
     TryCreateStraightPath(
         startCell,
         step,
         desiredLength);

            if (path == null ||
                path.Count < 2)
            {
                continue;
            }

            if (!IsCandidateEndSafe(path))
            {
                continue;
            }

            DominoLine line =
                CreateProceduralLine(
                    path,
                    lineIndex);

            if (line == null)
                continue;

            generatedLines.Add(line);

            lineIndex++;

            occupiedCells +=
                path.Count;
        }

        Debug.Log(
            $"Straight path generation complete. " +
            $"Occupied {occupiedCells}/{totalCells} cells. " +
            $"Attempts: {attempts}"
        );
    }

    private bool IsCandidateEndSafe(
    List<Vector2Int> candidatePath)
    {
        if (!TryGetCandidateFallArea(
                candidatePath,
                out DominoFallArea candidateArea))
        {
            return false;
        }

        foreach (DominoLine existingLine in generatedLines)
        {
            if (existingLine == null)
                continue;

            if (!TryGetExistingLineFallArea(
                    existingLine,
                    out DominoFallArea existingArea))
            {
                continue;
            }

            // =====================================================
            // ONLY CARE ABOUT ENDS THAT FACE EACH OTHER
            // =====================================================

            if (!AreEndsFacingEachOther(
                    candidateArea,
                    existingArea))
            {
                continue;
            }

            // =====================================================
            // DISTANCE BETWEEN THE TWO LAST DOMINOES
            // =====================================================

            Vector3 candidateLast =
                candidateArea.start;

            Vector3 existingLast =
                existingArea.start;

            candidateLast.y = 0f;
            existingLast.y = 0f;

            float distance =
                Vector3.Distance(
                    candidateLast,
                    existingLast);

            // =====================================================
            // HOW FAR EACH DOMINO CAN FALL
            // =====================================================

            float candidateReach =
                Vector3.Distance(
                    candidateArea.start,
                    candidateArea.end);

            float existingReach =
                Vector3.Distance(
                    existingArea.start,
                    existingArea.end);

            // Small safety margin.
            float safetyMargin = 0.03f;

            float requiredDistance =
                candidateReach +
                existingReach +
                safetyMargin;

            // =====================================================
            // NOT ENOUGH ROOM FOR BOTH TO FALL
            // =====================================================

            if (distance < requiredDistance)
            {
                return false;
            }
        }

        return true;
    }

    private LineDirection GetRandomLineDirection()
    {
        List<LineDirection> available =
            new List<LineDirection>();

        if (allowHorizontalLines)
        {
            available.Add(
                LineDirection.LeftToRight
            );

            available.Add(
                LineDirection.RightToLeft
            );
        }

        if (allowVerticalLines)
        {
            available.Add(
                LineDirection.BottomToTop
            );

            available.Add(
                LineDirection.TopToBottom
            );
        }

        if (allowDiagonalLines)
        {
            available.Add(
                LineDirection.BottomLeftToTopRight
            );

            available.Add(
                LineDirection.TopRightToBottomLeft
            );

            available.Add(
                LineDirection.TopLeftToBottomRight
            );

            available.Add(
                LineDirection.BottomRightToTopLeft
            );
        }

        if (available.Count == 0)
        {
            return LineDirection.LeftToRight;
        }

        return available[
            Random.Range(
                0,
                available.Count
            )
        ];
    }

    private Vector2Int GetDirectionStep(
    LineDirection direction)
    {
        switch (direction)
        {
            case LineDirection.LeftToRight:
                return new Vector2Int(1, 0);

            case LineDirection.RightToLeft:
                return new Vector2Int(-1, 0);

            case LineDirection.BottomToTop:
                return new Vector2Int(0, 1);

            case LineDirection.TopToBottom:
                return new Vector2Int(0, -1);

            case LineDirection.BottomLeftToTopRight:
                return new Vector2Int(1, 1);

            case LineDirection.TopRightToBottomLeft:
                return new Vector2Int(-1, -1);

            case LineDirection.TopLeftToBottomRight:
                return new Vector2Int(1, -1);

            case LineDirection.BottomRightToTopLeft:
                return new Vector2Int(-1, 1);
        }

        return Vector2Int.right;
    }

    private bool TryGetRandomStartCell(
    out Vector2Int result)
    {
        const int attempts = 30;

        for (int i = 0; i < attempts; i++)
        {
            Vector2Int cell =
                new Vector2Int(
                    Random.Range(0, board.Width),
                    Random.Range(0, board.Height));

            if (IsCellClearFromOtherLines(
                    cell,
                    segmentGapCells))
            {
                result = cell;
                return true;
            }
        }

        result = default;
        return false;
    }

    private List<Vector2Int> TryCreateStraightPath(
    Vector2Int start,
    Vector2Int step,
    int desiredLength)
    {
        List<Vector2Int> path =
            new List<Vector2Int>();

        Vector2Int current =
            start;

        for (int i = 0; i < desiredLength; i++)
        {
            // Outside board.
            if (!board.IsInside(current))
                break;

            // IMPORTANT:
            // Keep distance from every PREVIOUS line.
            if (!IsCellClearFromOtherLines(
                    current,
                    segmentGapCells))
            {
                break;
            }

            path.Add(current);

            current += step;
        }

        // Don't create tiny useless lines.
        if (path.Count < minDominoesPerLine)
            return null;

        return path;
    }


    private bool IsCellClearFromOtherLines(
    Vector2Int cell,
    int gap)
    {
        // Never allow the exact occupied cell.
        if (board.IsOccupied(cell))
            return false;

        // Check surrounding cells.
        for (int x = -gap; x <= gap; x++)
        {
            for (int y = -gap; y <= gap; y++)
            {
                Vector2Int nearby =
                    new Vector2Int(
                        cell.x + x,
                        cell.y + y
                    );

                // Outside board does not matter here.
                if (!board.IsInside(nearby))
                    continue;

                if (board.IsOccupied(nearby))
                    return false;
            }
        }

        return true;
    }


    // =========================================================
    // LEFT -> RIGHT ROW
    // =========================================================

    private void GenerateRowLeftToRight(
        int y,
        ref int lineIndex)
    {
        int x = 0;


        while (x < board.Width)
        {
            int remaining =
                board.Width - x;


            if (remaining < 2)
                break;


            int length =
                GetSegmentLength(
                    remaining
                );


            List<Vector2Int> path =
                new List<Vector2Int>();


            for (
                int i = 0;
                i < length;
                i++)
            {
                Vector2Int cell =
                    new Vector2Int(
                        x + i,
                        y
                    );


                if (board.IsOccupied(cell))
                {
                    Debug.LogWarning(
                        $"Attempted duplicate cell {cell}."
                    );

                    continue;
                }


                path.Add(cell);
            }


            if (path.Count >= 2)
            {
                DominoLine line =
                    CreateProceduralLine(
                        path,
                        lineIndex
                    );


                if (line != null)
                {
                    generatedLines.Add(
                        line
                    );

                    lineIndex++;
                }
            }


            x +=
    length +
    segmentGapCells;
        }
    }


    // =========================================================
    // RIGHT -> LEFT ROW
    // =========================================================

    private void GenerateRowRightToLeft(
        int y,
        ref int lineIndex)
    {
        int x =
            board.Width - 1;


        while (x >= 0)
        {
            int remaining =
                x + 1;


            if (remaining < 2)
                break;


            int length =
                GetSegmentLength(
                    remaining
                );


            List<Vector2Int> path =
                new List<Vector2Int>();


            for (
                int i = 0;
                i < length;
                i++)
            {
                Vector2Int cell =
                    new Vector2Int(
                        x - i,
                        y
                    );


                if (board.IsOccupied(cell))
                {
                    Debug.LogWarning(
                        $"Attempted duplicate cell {cell}."
                    );

                    continue;
                }


                path.Add(cell);
            }


            if (path.Count >= 2)
            {
                DominoLine line =
                    CreateProceduralLine(
                        path,
                        lineIndex
                    );


                if (line != null)
                {
                    generatedLines.Add(
                        line
                    );

                    lineIndex++;
                }
            }


            x -=
     length +
     segmentGapCells;
        }
    }


    // =========================================================
    // SEGMENT LENGTH
    // =========================================================

    private int GetSegmentLength(
        int remaining)
    {
        int length =
            Random.Range(
                minDominoesPerLine,
                maxDominoesPerLine + 1
            );


        length =
            Mathf.Clamp(
                length,
                2,
                remaining
            );


        // Prevent:
        //
        // XXXXXXXX X
        //
        // where one unusable single cell remains.
        int leftover =
            remaining -
            length;


        if (leftover == 1)
        {
            length++;
        }


        return length;
    }


    // =========================================================
    // CREATE PROCEDURAL LINE
    // =========================================================

    private DominoLine CreateProceduralLine(
        List<Vector2Int> path,
        int lineIndex)
    {
        if (path == null ||
            path.Count < 2)
        {
            return null;
        }


        DominoLine line =
            Instantiate(
                linePrefab,
                generatedRoot
            );


        line.name =
            $"Generated_Line_{lineIndex:000}";


        // -----------------------------------------------------
        // IMPORTANT
        // Procedural levels use generated dependencies.
        // -----------------------------------------------------

        line.useGeneratedBlocking = true;

        line.blockedByLines.Clear();


        // -----------------------------------------------------
        // CREATE DOMINOES
        // -----------------------------------------------------

        for (
            int i = 0;
            i < path.Count;
            i++)
        {
            Vector2Int cell =
                path[i];


            if (board.IsOccupied(cell))
            {
                Debug.LogError(
                    $"Cell {cell} was already occupied."
                );

                continue;
            }


            board.Occupy(cell);


            Vector3 worldPosition =
                board.CellToWorld(cell);


            worldPosition.y +=
                groundOffset;


            Domino domino =
                Instantiate(
                    dominoPrefab,
                    line.transform
                );

            domino.SetRevealSize(
    cellSize + revealOverlap
);


            domino.name =
                $"Domino_{i:000}";


            domino.transform.position =
                worldPosition;


            // -------------------------------------------------
            // ROTATION
            // -------------------------------------------------

            Vector3 direction =
                GetPathDirection(
                    path,
                    i
                );


            if (direction.sqrMagnitude >
                0.001f)
            {
                Quaternion rotation =
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up
                    );


                rotation *=
                    Quaternion.Euler(
                        rotationOffset
                    );


                domino.transform.rotation =
                    rotation;
            }


            generatedDominoCount++;
        }


        // Connect domino 0 -> 1 -> 2 -> ...
        line.AutoConnect();


        generatedLineCount++;


        return line;
    }


    // =========================================================
    // PATH DIRECTION
    // =========================================================

    private Vector3 GetPathDirection(
        List<Vector2Int> path,
        int index)
    {
        if (path == null ||
            path.Count < 2)
        {
            return Vector3.forward;
        }


        Vector2Int from;
        Vector2Int to;


        if (index <
            path.Count - 1)
        {
            from =
                path[index];

            to =
                path[index + 1];
        }
        else
        {
            from =
                path[index - 1];

            to =
                path[index];
        }


        Vector3 fromWorld =
            board.CellToWorld(
                from
            );


        Vector3 toWorld =
            board.CellToWorld(
                to
            );


        Vector3 direction =
            toWorld -
            fromWorld;


        direction.y = 0f;


        if (direction.sqrMagnitude <
            0.001f)
        {
            return Vector3.forward;
        }


        return direction.normalized;
    }


    // =========================================================
    // DEPENDENCIES / BLOCKERS
    // =========================================================

    private void GenerateDependencies()
    {
        if (generatedLines.Count == 0)
            return;


        // -----------------------------------------------------
        // LINE 0 MUST ALWAYS BE AVAILABLE.
        // -----------------------------------------------------

        generatedLines[0]
            .blockedByLines
            .Clear();


        // -----------------------------------------------------
        // REMAINING LINES
        // -----------------------------------------------------

        for (
            int i = 1;
            i < generatedLines.Count;
            i++)
        {
            DominoLine current =
                generatedLines[i];


            current.blockedByLines.Clear();


            // Some lines deliberately remain available.
            if (Random.value >
                blockerChance)
            {
                continue;
            }


            int blockerCount =
                Random.Range(
                    1,
                    maxBlockersPerLine + 1
                );


            blockerCount =
                Mathf.Min(
                    blockerCount,
                    i
                );


            HashSet<int> chosenIndices =
                new HashSet<int>();


            for (
                int blockerNumber = 0;
                blockerNumber < blockerCount;
                blockerNumber++)
            {
                int attempts = 0;


                while (attempts < 30)
                {
                    attempts++;


                    // ONLY earlier lines may block this line.
                    //
                    // This guarantees there can never be:
                    //
                    // A blocked by B
                    // B blocked by C
                    // C blocked by A
                    //
                    int blockerIndex =
                        Random.Range(
                            0,
                            i
                        );


                    if (chosenIndices.Contains(
                            blockerIndex))
                    {
                        continue;
                    }


                    chosenIndices.Add(
                        blockerIndex
                    );


                    current.blockedByLines.Add(
                        generatedLines[
                            blockerIndex
                        ]
                    );


                    break;
                }
            }
        }


        DebugGeneratedDependencies();
    }


    private bool TryGetExistingLineFallArea(
    DominoLine line,
    out DominoFallArea area)
    {
        area = default;

        if (line == null ||
            line.dominoes == null ||
            line.dominoes.Count < 2)
        {
            return false;
        }

        Domino last =
            line.dominoes[
                line.dominoes.Count - 1];

        Domino previous =
            line.dominoes[
                line.dominoes.Count - 2];

        if (last == null ||
            previous == null)
        {
            return false;
        }

        Vector3 direction =
            last.transform.position -
            previous.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();

        Collider collider =
            last.GetComponentInChildren<Collider>();

        if (collider == null)
            return false;

        Bounds bounds =
            collider.bounds;

        float fallReach =
            bounds.size.y *
            endFallReachMultiplier;

        float radius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z)
            + endFallPadding;

        Vector3 start =
            last.transform.position +
            direction * 0.05f;

        Vector3 end =
            last.transform.position +
            direction * fallReach;

        start.y = last.transform.position.y;
        end.y = last.transform.position.y;

        area = new DominoFallArea
        {
            start = start,
            end = end,
            radius = radius,
            direction = direction
        };

        return true;
    }


    private bool TryGetCandidateFallArea(
    List<Vector2Int> path,
    out DominoFallArea area)
    {
        area = default;

        if (path == null ||
            path.Count < 2)
        {
            return false;
        }

        Vector2Int previousCell =
            path[path.Count - 2];

        Vector2Int lastCell =
            path[path.Count - 1];

        Vector3 previousPosition =
            board.CellToWorld(previousCell);

        Vector3 lastPosition =
            board.CellToWorld(lastCell);

        previousPosition.y += groundOffset;
        lastPosition.y += groundOffset;

        Vector3 direction =
            lastPosition -
            previousPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();

        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
        {
            Debug.LogError(
                "Domino prefab needs a Collider.");

            return false;
        }

        Bounds bounds =
            prefabCollider.bounds;

        float fallReach =
            bounds.size.y *
            endFallReachMultiplier;

        float radius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z)
            + endFallPadding;

        Vector3 start =
            lastPosition +
            direction * 0.05f;

        Vector3 end =
            lastPosition +
            direction * fallReach;

        start.y = lastPosition.y;
        end.y = lastPosition.y;

        area = new DominoFallArea
        {
            start = start,
            end = end,
            radius = radius,
            direction = direction
        };

        return true;
    }

    private bool FallAreasOverlap(
    DominoFallArea a,
    DominoFallArea b)
    {
        float distance =
            DistanceBetweenSegmentsXZ(
                a.start,
                a.end,
                b.start,
                b.end);

        float combinedRadius =
            a.radius +
            b.radius;

        return distance <
               combinedRadius;
    }

    private float DistanceBetweenSegmentsXZ(
    Vector3 a1,
    Vector3 a2,
    Vector3 b1,
    Vector3 b2)
    {
        // Flatten onto board.
        a1.y = 0f;
        a2.y = 0f;
        b1.y = 0f;
        b2.y = 0f;

        // Sample along A.
        // This is more than accurate enough for generation
        // because these are short domino-fall segments.

        const int samples = 12;

        float minimumDistance =
            float.MaxValue;

        for (int i = 0; i <= samples; i++)
        {
            float t =
                i / (float)samples;

            Vector3 point =
                Vector3.Lerp(
                    a1,
                    a2,
                    t);

            Vector3 closest =
                ClosestPointOnSegmentXZ(
                    b1,
                    b2,
                    point);

            float distance =
                Vector3.Distance(
                    point,
                    closest);

            if (distance < minimumDistance)
            {
                minimumDistance =
                    distance;
            }
        }

        // Also sample B against A.
        for (int i = 0; i <= samples; i++)
        {
            float t =
                i / (float)samples;

            Vector3 point =
                Vector3.Lerp(
                    b1,
                    b2,
                    t);

            Vector3 closest =
                ClosestPointOnSegmentXZ(
                    a1,
                    a2,
                    point);

            float distance =
                Vector3.Distance(
                    point,
                    closest);

            if (distance < minimumDistance)
            {
                minimumDistance =
                    distance;
            }
        }

        return minimumDistance;
    }


    private Vector3 ClosestPointOnSegmentXZ(
    Vector3 start,
    Vector3 end,
    Vector3 point)
    {
        start.y = 0f;
        end.y = 0f;
        point.y = 0f;

        Vector3 segment =
            end - start;

        float lengthSquared =
            segment.sqrMagnitude;

        if (lengthSquared < 0.0001f)
            return start;

        float t =
            Vector3.Dot(
                point - start,
                segment)
            / lengthSquared;

        t = Mathf.Clamp01(t);

        return start +
               segment * t;
    }

    private bool AreEndsFacingEachOther(
    DominoFallArea a,
    DominoFallArea b)
    {
        Vector3 fromAToB =
            b.start - a.start;

        fromAToB.y = 0f;

        if (fromAToB.sqrMagnitude < 0.001f)
            return true;

        fromAToB.Normalize();

        Vector3 fromBToA =
            -fromAToB;

        float aFacesB =
            Vector3.Dot(
                a.direction,
                fromAToB);

        float bFacesA =
            Vector3.Dot(
                b.direction,
                fromBToA);

        // > 0 means each line is generally pointing
        // toward the other.
        return
            aFacesB > 0.25f &&
            bFacesA > 0.25f;
    }

    // =========================================================
    // DEPENDENCY DEBUGGING
    // =========================================================

    private void DebugGeneratedDependencies()
    {
        foreach (
            DominoLine line
            in generatedLines)
        {
            if (line == null)
                continue;


            if (line.blockedByLines.Count == 0)
            {
                Debug.Log(
                    $"{line.name} = FREE"
                );

                continue;
            }


            string blockerNames = "";


            for (
                int i = 0;
                i < line.blockedByLines.Count;
                i++)
            {
                DominoLine blocker =
                    line.blockedByLines[i];


                if (blocker == null)
                    continue;


                if (blockerNames.Length > 0)
                {
                    blockerNames += ", ";
                }


                blockerNames +=
                    blocker.name;
            }


            Debug.Log(
                $"{line.name} BLOCKED BY: " +
                blockerNames
            );
        }
    }


    // =========================================================
    // ROOT
    // =========================================================

    private void CreateGeneratedRoot()
    {
        GameObject root =
            new GameObject(
                "_GeneratedLevel"
            );


        root.transform.SetParent(
            transform
        );


        root.transform.localPosition =
            Vector3.zero;


        root.transform.localRotation =
            Quaternion.identity;


        root.transform.localScale =
            Vector3.one;


        generatedRoot =
            root.transform;
    }


    // =========================================================
    // CLEAR
    // =========================================================

    public void ClearLevel()
    {
        Transform existing =
            transform.Find(
                "_GeneratedLevel"
            );


        if (existing != null)
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                DestroyImmediate(
                    existing.gameObject
                );
            }
            else
            {
                Destroy(
                    existing.gameObject
                );
            }

#else

            Destroy(
                existing.gameObject
            );

#endif
        }


        generatedRoot = null;

        board = null;

        generatedLines.Clear();

        generatedDominoCount = 0;
        generatedLineCount = 0;
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private bool ValidateSettings()
    {
        if (dominoPrefab == null)
        {
            Debug.LogError(
                "ProceduralLevelGenerator: Domino Prefab is missing."
            );

            return false;
        }


        if (linePrefab == null)
        {
            Debug.LogError(
                "ProceduralLevelGenerator: Line Prefab is missing."
            );

            return false;
        }


        if (groundTransform == null)
        {
            Debug.LogError(
                "ProceduralLevelGenerator: Ground Transform is missing."
            );

            return false;
        }


        if (groundRenderer == null)
        {
            Debug.LogError(
                "ProceduralLevelGenerator: Ground Renderer is missing."
            );

            return false;
        }


        if (cellSize <= 0f)
        {
            Debug.LogError(
                "Cell Size must be greater than zero."
            );

            return false;
        }


        if (levelsPerSizeIncrease <= 0)
        {
            Debug.LogError(
                "Levels Per Size Increase must be greater than zero."
            );

            return false;
        }


        if (startingGroundSize.x <= 0f ||
            startingGroundSize.y <= 0f)
        {
            Debug.LogError(
                "Starting Ground Size must be greater than zero."
            );

            return false;
        }


        if (maximumGroundSize.x <
            startingGroundSize.x ||
            maximumGroundSize.y <
            startingGroundSize.y)
        {
            Debug.LogError(
                "Maximum Ground Size must be >= Starting Ground Size."
            );

            return false;
        }


        if (minDominoesPerLine < 2)
        {
            Debug.LogError(
                "Min Dominoes Per Line must be at least 2."
            );

            return false;
        }


        if (maxDominoesPerLine <
            minDominoesPerLine)
        {
            Debug.LogError(
                "Max Dominoes Per Line must be >= Min Dominoes Per Line."
            );

            return false;
        }


        if (rowSpacing < 1)
        {
            Debug.LogError(
                "Row Spacing must be at least 1."
            );

            return false;
        }


        if (maxBlockersPerLine < 1)
        {
            Debug.LogError(
                "Max Blockers Per Line must be at least 1."
            );

            return false;
        }


        return true;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (cellSize <= 0f)
            return;


        int columns =
            board != null
                ? board.Width
                : CalculateColumns();


        int rows =
            board != null
                ? board.Height
                : CalculateRows();


        if (columns <= 0 ||
            rows <= 0)
        {
            return;
        }


        float width =
            (columns - 1) *
            cellSize;


        float depth =
            (rows - 1) *
            cellSize;


        Vector3 center =
            transform.position;


        if (groundRenderer != null)
        {
            center =
                groundRenderer.bounds.center;
        }


        Vector3 bottomLeft =
            center -
            new Vector3(
                width * 0.5f,
                0f,
                depth * 0.5f
            );


        bottomLeft.y +=
            0.02f;


        // Vertical grid lines.
        for (
            int x = 0;
            x < columns;
            x++)
        {
            Vector3 start =
                bottomLeft +
                Vector3.right *
                (x * cellSize);


            Vector3 end =
                start +
                Vector3.forward *
                depth;


            Gizmos.DrawLine(
                start,
                end
            );
        }


        // Horizontal grid lines.
        for (
            int y = 0;
            y < rows;
            y++)
        {
            Vector3 start =
                bottomLeft +
                Vector3.forward *
                (y * cellSize);


            Vector3 end =
                start +
                Vector3.right *
                width;


            Gizmos.DrawLine(
                start,
                end
            );
        }
    }
}