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
    private enum PathShape
    {
        Straight,
        Arc,
        HalfCircle,
        Circle
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

    [Header("Real Fall Sweep Validation")]

    [Tooltip("Number of orientations tested while a domino rotates from standing to fallen.")]
    [Range(4, 16)]
    [SerializeField]
    private int fallSweepSamples = 6;

    [Tooltip("Extra safety added to the simulated falling collider.")]
    [Range(0f, 0.1f)]
    [SerializeField]
    private float fallSweepPadding = 0.015f;


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
    public BoardFrame boardFrame;

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
    private float targetBoardFill = 1f;

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

    [Header("Curved Path Generation")]

    [Tooltip("Allow curved arc-shaped lines.")]
    [SerializeField]
    private bool allowArcLines = true;

    [Tooltip("Allow half-circle lines.")]
    [SerializeField]
    private bool allowHalfCircleLines = true;

    [Tooltip("Allow full-circle lines.")]
    [SerializeField]
    private bool allowCircleLines = true;

    [Tooltip("Chance that a generated line will try to be curved instead of straight.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float curvedLineChance = 0.60f;

    [Tooltip("Minimum radius of generated curves, measured in grid cells.")]
    [Min(2)]
    [SerializeField]
    private int minCurveRadius = 3;

    [Tooltip("Maximum radius of generated curves, measured in grid cells.")]
    [Min(2)]
    [SerializeField]
    private int maxCurveRadius = 6;


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

        // Resize premium frame to match new Ground.
        if (boardFrame != null)
        {
            boardFrame.Resize();
        }

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
            // FINAL PHYSICAL FALL VALIDATION
            // ---------------------------------------------

            // ---------------------------------------------
            // CALCULATE REAL GEOMETRIC BLOCKERS FIRST
            // ---------------------------------------------

            blockingCalculator.CalculateBlocking(
                generatedLines);

            // ---------------------------------------------
            // CHECK SOLVABILITY OF DEPENDENCIES
            // ---------------------------------------------

            bool dependencySolvable =
                validator.IsLevelSolvable(
                    generatedLines);

            if (!dependencySolvable)
            {
                Debug.LogWarning(
                    $"INVALID DEPENDENCY LAYOUT | " +
                    $"Attempt {attempt + 1} | " +
                    $"Seed {seed} | Regenerating..."
                );

                continue;
            }

            // ---------------------------------------------
            // CHECK PHYSICAL FALLS IN A PLAYABLE ORDER
            // ---------------------------------------------

            if (!ValidatePlayableFallOrder())
            {
                Debug.LogWarning(
                    $"INVALID PLAYABLE PHYSICAL LAYOUT | " +
                    $"Attempt {attempt + 1} | " +
                    $"Seed {seed} | Regenerating..."
                );

                continue;
            }

            bool solvable = true;

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


    private bool DoesPlayableDominoHitStandingLine(
     Domino fallingDomino,
     DominoLine fallingLine,
     Vector3 fallDirection,
     HashSet<DominoLine> standingLines)
    {
        if (fallingDomino == null ||
            fallingLine == null ||
            standingLines == null)
        {
            return false;
        }

        BoxCollider box =
            fallingDomino.GetComponentInChildren<BoxCollider>();

        if (box == null)
            return false;


        fallDirection.y = 0f;

        if (fallDirection.sqrMagnitude <
            0.001f)
        {
            return false;
        }

        fallDirection.Normalize();


        // =====================================================
        // COLLIDER SIZE
        // =====================================================

        Vector3 scale =
            box.transform.lossyScale;

        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        scale.z = Mathf.Abs(scale.z);


        Vector3 size =
            Vector3.Scale(
                box.size,
                scale);


        Vector3 halfExtents =
            size * 0.5f;

        halfExtents.x += fallSweepPadding;
        halfExtents.y += fallSweepPadding;
        halfExtents.z += fallSweepPadding;


        // =====================================================
        // REAL COLLIDER CENTER
        // =====================================================

        Vector3 standingCenter =
            box.transform.TransformPoint(
                box.center);


        // Approximate pivot at bottom of collider.
        Vector3 pivot =
            standingCenter -
            box.transform.up *
            (size.y * 0.5f);


        // =====================================================
        // FALL ROTATION AXIS
        // =====================================================

        Vector3 rotationAxis =
            Vector3.Cross(
                Vector3.up,
                fallDirection);

        if (rotationAxis.sqrMagnitude <
            0.001f)
        {
            return false;
        }

        rotationAxis.Normalize();


        int samples =
            Mathf.Max(
                4,
                fallSweepSamples);


        // =====================================================
        // SIMULATE ROTATION FROM STANDING -> FALLEN
        // =====================================================

        for (int i = 1;
             i <= samples;
             i++)
        {
            float t =
                i / (float)samples;

            float angle =
                Mathf.Lerp(
                    5f,
                    90f,
                    t);


            Quaternion deltaRotation =
                Quaternion.AngleAxis(
                    angle,
                    rotationAxis);


            // Rotate collider center around its bottom pivot.
            Vector3 center =
                pivot +
                deltaRotation *
                (standingCenter - pivot);


            Quaternion rotation =
                deltaRotation *
                box.transform.rotation;


            Collider[] hits =
                Physics.OverlapBox(
                    center,
                    halfExtents,
                    rotation,
                    ~0,
                    QueryTriggerInteraction.Ignore);


            foreach (Collider hit in hits)
            {
                Domino other =
                    hit.GetComponentInParent<Domino>();

                if (other == null)
                    continue;


                // -----------------------------------------
                // Ignore ourselves
                // -----------------------------------------

                if (other == fallingDomino)
                    continue;


                // -----------------------------------------
                // Same line is intentional
                // -----------------------------------------

                if (other.ownerLine ==
                    fallingLine)
                {
                    continue;
                }


                DominoLine otherLine =
                    other.ownerLine;

                if (otherLine == null)
                    continue;


                // =================================================
                // MOST IMPORTANT PART:
                //
                // If that line was already cleared earlier
                // in our simulated solution, its dominoes
                // are NOT standing anymore.
                //
                // Therefore IGNORE it.
                // =================================================

                if (!standingLines.Contains(
                        otherLine))
                {
                    continue;
                }


                Debug.LogWarning(
                    $"PLAYABLE FALL COLLISION | " +
                    $"{fallingLine.name}/" +
                    $"{fallingDomino.name} -> " +
                    $"{otherLine.name}/" +
                    $"{other.name} | " +
                    $"Angle={angle:F1}");

                return true;
            }
        }

        return false;
    }

    private Vector3 GetActualDominoFallDirection(
    DominoLine line,
    int index)
    {
        if (line == null ||
            line.dominoes == null ||
            line.dominoes.Count < 2)
        {
            return Vector3.zero;
        }

        int count =
            line.dominoes.Count;

        Domino current =
            line.dominoes[index];

        if (current == null)
            return Vector3.zero;

        Vector3 direction;

        // Normal domino:
        // fall toward its connected next domino.
        if (index < count - 1)
        {
            Domino next =
                line.dominoes[index + 1];

            if (next == null)
                return Vector3.zero;

            direction =
                next.transform.position -
                current.transform.position;
        }
        else
        {
            // Last domino continues in the direction
            // established by the previous domino.
            Domino previous =
                line.dominoes[index - 1];

            if (previous == null)
                return Vector3.zero;

            direction =
                current.transform.position -
                previous.transform.position;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return direction.normalized;
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
                totalCells * targetBoardFill);

        int occupiedCells = 0;

        int attempts = 0;

        while (
            occupiedCells < targetOccupiedCells &&
            attempts < maxLineGenerationAttempts)
        {
            attempts++;

            // =============================================
            // TRY CURVED LINE
            // =============================================

            bool tryCurved =
                Random.value < curvedLineChance &&
                (
                    allowArcLines ||
                    allowHalfCircleLines ||
                    allowCircleLines
                );

            if (tryCurved)
            {
                PathShape shape =
                    GetRandomPathShape();

                CurvedPath curvedPath =
                    TryCreateCurvedPath(shape);

                if (curvedPath == null ||
                    curvedPath.positions.Count < 2)
                {
                    continue;
                }

                if (!IsCurvedCandidateEndSafe(
                        curvedPath))
                {
                    continue;
                }

                if (!HasEnoughCurvedPhysicalClearance(
        curvedPath))
                {
                    continue;
                }

                if (!HasSafeCurvedFallCorridors(
        curvedPath))
                {
                    continue;
                }

                if (!HasSafeInternalCurvedFallCorridors(
        curvedPath))

                {
                    continue;
                }

                if (!HasSafeCurvedSweepAgainstOtherLines(
        curvedPath))
                {
                    continue;
                }

                DominoLine curvedLine =
                    CreateCurvedProceduralLine(
                        curvedPath,
                        lineIndex);

                if (curvedLine == null)
                    continue;

                generatedLines.Add(
                    curvedLine);

                lineIndex++;

                occupiedCells +=
                    curvedPath.occupiedCells.Count;

                continue;
            }

            // =============================================
            // ORIGINAL STRAIGHT GENERATION
            // =============================================

            LineDirection lineDirection =
                GetRandomLineDirection();

            Vector2Int step =
                GetDirectionStep(
                    lineDirection);

            int desiredLength =
                Random.Range(
                    minDominoesPerLine,
                    maxDominoesPerLine + 1);

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
            $"Path generation complete. " +
            $"Occupied {occupiedCells}/{totalCells} cells. " +
            $"Attempts: {attempts}");
    }

    

    

    private float DistancePointToSegmentXZ(
    Vector3 point,
    Vector3 start,
    Vector3 end)
    {
        point.y = 0f;
        start.y = 0f;
        end.y = 0f;

        Vector3 segment = end - start;

        float lengthSqr =
            segment.sqrMagnitude;

        if (lengthSqr < 0.0001f)
        {
            return Vector3.Distance(
                point,
                start
            );
        }

        float t =
            Vector3.Dot(
                point - start,
                segment
            ) / lengthSqr;

        t = Mathf.Clamp01(t);

        Vector3 closest =
            start +
            segment * t;

        return Vector3.Distance(
            point,
            closest
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

    private PathShape GetRandomPathShape()
    {
        List<PathShape> available =
            new List<PathShape>();

        if (allowArcLines)
            available.Add(PathShape.Arc);

        if (allowHalfCircleLines)
            available.Add(PathShape.HalfCircle);

        if (allowCircleLines)
            available.Add(PathShape.Circle);

        if (available.Count == 0)
            return PathShape.Straight;

        return available[
            Random.Range(0, available.Count)
        ];
    }
    private class CurvedPath
    {
        public List<Vector3> positions =
            new List<Vector3>();

        public List<Vector3> directions =
            new List<Vector3>();

        public List<Vector2Int> occupiedCells =
            new List<Vector2Int>();
    }


    private CurvedPath TryCreateCurvedPath(
    PathShape shape)
    {
        // Radius is selected in grid-cell units,
        // then converted to actual world-space distance.
        int radiusCells =
            Random.Range(
                minCurveRadius,
                maxCurveRadius + 1);

        float radius =
            radiusCells * cellSize;

        // ---------------------------------------------
        // SELECT SWEEP
        // ---------------------------------------------

        float sweepDegrees;

        switch (shape)
        {
            case PathShape.Arc:
                sweepDegrees =
                    Random.Range(70f, 140f);
                break;

            case PathShape.HalfCircle:
                sweepDegrees = 180f;
                break;

            case PathShape.Circle:
                sweepDegrees = 360f;
                break;

            default:
                return null;
        }

        // Random clockwise/counter-clockwise.
        float directionSign =
            Random.value < 0.5f
                ? -1f
                : 1f;

        sweepDegrees *= directionSign;

        float startAngle =
            Random.Range(0f, 360f);

        // ---------------------------------------------
        // NUMBER OF DOMINOES
        // Keep curved domino spacing consistent.
        // ---------------------------------------------

        float totalAngleRadians =
            Mathf.Abs(sweepDegrees) *
            Mathf.Deg2Rad;

        // We want approximately the same world-space
        // distance between neighboring domino CENTERS
        // as straight lines use: cellSize.
        //
        // For a circle:
        // chord = 2 * radius * sin(angleStep / 2)
        //
        // Solve for angleStep using desired chord = cellSize.
        float ratio =
            Mathf.Clamp(
                cellSize / (2f * radius),
                0f,
                0.9999f
            );

        float angleStepRadians =
            2f * Mathf.Asin(ratio);

        if (angleStepRadians <= 0.0001f)
            return null;

        int dominoCount;

        if (shape == PathShape.Circle)
        {
            // Circle does not duplicate first/last point.
            dominoCount =
                Mathf.RoundToInt(
                    (Mathf.PI * 2f) /
                    angleStepRadians
                );

            dominoCount =
                Mathf.Max(
                    minDominoesPerLine,
                    dominoCount
                );
        }
        else
        {
            // Arc / half-circle includes BOTH ends.
            int intervals =
                Mathf.RoundToInt(
                    totalAngleRadians /
                    angleStepRadians
                );

            intervals = Mathf.Max(1, intervals);

            dominoCount = intervals + 1;

            dominoCount =
                Mathf.Max(
                    minDominoesPerLine,
                    dominoCount
                );
        }

        if (dominoCount < 2)
            return null;

        // ---------------------------------------------
        // CHOOSE CENTER
        // ---------------------------------------------

        // We deliberately choose a grid cell as center,
        // but domino positions themselves are NOT
        // restricted to the grid.
        Vector2Int centerCell =
            new Vector2Int(
                Random.Range(0, board.Width),
                Random.Range(0, board.Height));

        Vector3 center =
            board.CellToWorld(centerCell);

        CurvedPath result =
            new CurvedPath();

        HashSet<Vector2Int> checkedCells =
            new HashSet<Vector2Int>();

        // ---------------------------------------------
        // GENERATE SMOOTH CURVE
        // ---------------------------------------------

        for (int i = 0; i < dominoCount; i++)
        {
            float t;

            if (shape == PathShape.Circle)
            {
                // Do NOT duplicate 0 and 360 degrees.
                t = i / (float)dominoCount;
            }
            else
            {
                // Arc / half-circle should include
                // both ends.
                t =
                    dominoCount <= 1
                        ? 0f
                        : i / (float)(dominoCount - 1);
            }

            float angleDegrees =
                startAngle +
                sweepDegrees * t;

            float angle =
                angleDegrees *
                Mathf.Deg2Rad;

            // -----------------------------------------
            // EXACT WORLD POSITION
            // -----------------------------------------

            Vector3 position =
                center +
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);

            // -----------------------------------------
            // EXACT CURVE TANGENT
            // -----------------------------------------

            Vector3 tangent =
                new Vector3(
                    -Mathf.Sin(angle),
                    0f,
                    Mathf.Cos(angle));

            tangent *= directionSign;

            tangent.Normalize();

            // -----------------------------------------
            // MAP TO GRID ONLY FOR VALIDATION
            // -----------------------------------------

            Vector2Int cell =
                board.WorldToCell(position);

            if (!board.IsInside(cell))
                return null;

            // Only check each mapped cell once.
            if (checkedCells.Add(cell))
            {
                if (!IsCellClearFromOtherLines(
                        cell,
                        segmentGapCells))
                {
                    return null;
                }

                result.occupiedCells.Add(cell);
            }

            result.positions.Add(position);
            result.directions.Add(tangent);
        }

        if (result.positions.Count <
            minDominoesPerLine)
        {
            return null;
        }

        return result;
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

    private DominoLine CreateCurvedProceduralLine(
    CurvedPath path,
    int lineIndex)
    {
        if (path == null ||
            path.positions == null ||
            path.positions.Count < 2)
        {
            return null;
        }

        DominoLine line =
            Instantiate(
                linePrefab,
                generatedRoot);

        line.name =
            $"Generated_Curve_{lineIndex:000}";

        line.useGeneratedBlocking = true;
        line.blockedByLines.Clear();

        // ---------------------------------------------
        // RESERVE GRID SPACE
        // ---------------------------------------------

        foreach (Vector2Int cell
                 in path.occupiedCells)
        {
            if (board.IsInside(cell))
                board.Occupy(cell);
        }

        // Reserve the physical area that these curved
        // dominoes will sweep through when they fall.
        // This prevents FUTURE generated lines from
        // entering the curve's fall corridor.
        ReserveCurvedFallSpace(path);

        // ---------------------------------------------
        // CREATE DOMINOES
        // ---------------------------------------------

        for (int i = 0;
             i < path.positions.Count;
             i++)
        {
            Vector3 worldPosition =
                path.positions[i];

            worldPosition.y +=
                groundOffset;

            Domino domino =
                Instantiate(
                    dominoPrefab,
                    line.transform);

            domino.SetRevealSize(
                cellSize + revealOverlap);

            domino.name =
                $"Domino_{i:000}";

            domino.transform.position =
                worldPosition;

            // Exact tangent calculated from the circle.
            Vector3 direction =
                path.directions[i];

            if (direction.sqrMagnitude >
                0.001f)
            {
                Quaternion rotation =
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up);

                rotation *=
                    Quaternion.Euler(
                        rotationOffset);

                domino.transform.rotation =
                    rotation;
            }

            generatedDominoCount++;
        }

        line.AutoConnect();

        generatedLineCount++;

        return line;
    }

    private bool IsCurvedCandidateEndSafe(
    CurvedPath path)
    {
        if (path == null ||
            path.positions.Count < 2)
        {
            return false;
        }

        Vector3 previous =
            path.positions[
                path.positions.Count - 2];

        Vector3 last =
            path.positions[
                path.positions.Count - 1];

        previous.y += groundOffset;
        last.y += groundOffset;

        Vector3 direction =
            last - previous;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();

        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
            return false;

        Bounds bounds =
            prefabCollider.bounds;

        float fallReach =
            bounds.size.y *
            endFallReachMultiplier;

        DominoFallArea candidateArea =
            new DominoFallArea
            {
                start =
                    last +
                    direction * 0.05f,

                end =
                    last +
                    direction * fallReach,

                radius =
                    Mathf.Max(
                        bounds.extents.x,
                        bounds.extents.z)
                    + endFallPadding,

                direction = direction
            };

        foreach (DominoLine existingLine
                 in generatedLines)
        {
            if (existingLine == null)
                continue;

            if (!TryGetExistingLineFallArea(
                    existingLine,
                    out DominoFallArea existingArea))
            {
                continue;
            }

            if (!AreEndsFacingEachOther(
                    candidateArea,
                    existingArea))
            {
                continue;
            }

            Vector3 a =
                candidateArea.start;

            Vector3 b =
                existingArea.start;

            a.y = 0f;
            b.y = 0f;

            float distance =
                Vector3.Distance(a, b);

            float candidateReach =
                Vector3.Distance(
                    candidateArea.start,
                    candidateArea.end);

            float existingReach =
                Vector3.Distance(
                    existingArea.start,
                    existingArea.end);

            float requiredDistance =
                candidateReach +
                existingReach +
                0.03f;

            if (distance < requiredDistance)
                return false;
        }

        return true;
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
                //Debug.Log(
                //    $"{line.name} = FREE"
                //);

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


            //Debug.Log(
            //    $"{line.name} BLOCKED BY: " +
            //    blockerNames
            //);
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

    private bool HasEnoughCurvedPhysicalClearance(
    CurvedPath candidatePath)
    {
        if (candidatePath == null ||
            candidatePath.positions == null)
        {
            return false;
        }

        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
            return false;

        // Horizontal footprint of one standing domino.
        float dominoRadius =
            Mathf.Max(
                prefabCollider.bounds.extents.x,
                prefabCollider.bounds.extents.z
            );

        float requiredDistance =
            dominoRadius * 2f +
            minimumDominoClearance;

        float requiredDistanceSqr =
            requiredDistance * requiredDistance;

        foreach (Vector3 candidatePosition
                 in candidatePath.positions)
        {
            Vector3 candidate = candidatePosition;
            candidate.y = 0f;

            foreach (DominoLine existingLine
                     in generatedLines)
            {
                if (existingLine == null ||
                    existingLine.dominoes == null)
                {
                    continue;
                }

                foreach (Domino existingDomino
                         in existingLine.dominoes)
                {
                    if (existingDomino == null)
                        continue;

                    Vector3 existing =
                        existingDomino.transform.position;

                    existing.y = 0f;

                    if ((candidate - existing).sqrMagnitude <
                        requiredDistanceSqr)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool HasSafeCurvedFallCorridors(CurvedPath candidatePath)
    {
        if (candidatePath == null ||
            candidatePath.positions == null ||
            candidatePath.directions == null)
        {
            return false;
        }

        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
            return false;

        Bounds bounds =
            prefabCollider.bounds;

        // Maximum horizontal reach of a falling domino.
        float fallReach =
            bounds.size.y *
            endFallReachMultiplier;

        float dominoRadius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z
            );

        float fallRadius =
            dominoRadius +
            endFallPadding;

        // =====================================================
        // CHECK EVERY DOMINO OF THIS CURVE
        // AGAINST EVERY DOMINO OF EXISTING LINES
        // =====================================================

        for (int i = 0;
             i < candidatePath.positions.Count;
             i++)
        {
            Vector3 start =
                candidatePath.positions[i];

            Vector3 direction =
    GetRuntimeCurvedFallDirection(
        candidatePath,
        i
    );

            start.y = 0f;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                continue;

            direction.Normalize();

            Vector3 end =
                start +
                direction * fallReach;

            foreach (DominoLine existingLine
                     in generatedLines)
            {
                if (existingLine == null ||
                    existingLine.dominoes == null)
                {
                    continue;
                }

                foreach (Domino existingDomino
                         in existingLine.dominoes)
                {
                    if (existingDomino == null)
                        continue;

                    Vector3 existingPosition =
                        existingDomino.transform.position;

                    existingPosition.y = 0f;

                    float distance =
                        DistancePointToSegmentXZ(
                            existingPosition,
                            start,
                            end
                        );

                    float requiredClearance =
                        fallRadius +
                        dominoRadius +
                        minimumDominoClearance;

                    if (distance <
                        requiredClearance)
                    {
                        // This curved domino could physically
                        // fall into another generated line.
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool HasSafeCurvedSweepAgainstOtherLines(
    CurvedPath candidatePath)
    {
        if (candidatePath == null ||
            candidatePath.positions == null ||
            candidatePath.positions.Count == 0)
        {
            return false;
        }

        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
            return false;

        Bounds bounds =
            prefabCollider.bounds;

        // Height of the domino represents approximately how far
        // its upper end can sweep away from its standing position.
        float fallReach =
            bounds.size.y *
            endFallReachMultiplier;

        float dominoHalfWidth =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z
            );

        // Do NOT use the complete fall height as a circle around
        // the domino. That would reject almost every useful curve.
        //
        // We use a fraction representing the sideways swept region.
        float sweepRadius =
            fallReach * 0.55f +
            dominoHalfWidth +
            endFallPadding;

        float otherRadius =
            dominoHalfWidth;

        float requiredDistance =
            sweepRadius +
            otherRadius +
            minimumDominoClearance;

        float requiredDistanceSqr =
            requiredDistance *
            requiredDistance;

        foreach (Vector3 candidatePosition
                 in candidatePath.positions)
        {
            Vector3 candidate =
                candidatePosition;

            candidate.y = 0f;

            foreach (DominoLine existingLine
                     in generatedLines)
            {
                if (existingLine == null ||
                    existingLine.dominoes == null)
                {
                    continue;
                }

                foreach (Domino existingDomino
                         in existingLine.dominoes)
                {
                    if (existingDomino == null)
                        continue;

                    Vector3 existing =
                        existingDomino.transform.position;

                    existing.y = 0f;

                    if ((candidate - existing).sqrMagnitude <
                        requiredDistanceSqr)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void ReserveCurvedFallSpace(
    CurvedPath path)
    {
        if (path == null ||
            path.positions == null ||
            path.directions == null)
        {
            return;
        }

        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
            return;

        Bounds bounds =
            prefabCollider.bounds;

        // How far the domino can physically reach while falling.
        float fallReach =
            bounds.size.y *
            endFallReachMultiplier;

        // Small sideways safety around the falling domino.
        float halfWidth =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z
            );

        float safetyRadius =
            halfWidth +
            endFallPadding;

        // Sample much smaller than one grid cell so
        // we don't leave holes in the reserved corridor.
        float sampleStep =
            Mathf.Max(
                cellSize * 0.25f,
                0.02f
            );

        for (int i = 0;
             i < path.positions.Count;
             i++)
        {
            Vector3 start =
                path.positions[i];

            Vector3 direction =
    GetRuntimeCurvedFallDirection(
        path,
        i
    );

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                continue;

            direction.Normalize();

            int samples =
                Mathf.CeilToInt(
                    fallReach / sampleStep
                );

            for (int s = 0;
                 s <= samples;
                 s++)
            {
                float distance =
                    Mathf.Min(
                        s * sampleStep,
                        fallReach
                    );

                Vector3 point =
                    start +
                    direction * distance;

                Vector2Int centerCell =
                    board.WorldToCell(point);

                // Reserve enough neighboring grid cells to
                // represent the physical width of the domino.
                int radiusCells =
                    Mathf.Max(
                        0,
                        Mathf.CeilToInt(
                            safetyRadius /
                            cellSize
                        )
                    );

                for (int x = -radiusCells;
                     x <= radiusCells;
                     x++)
                {
                    for (int y = -radiusCells;
                         y <= radiusCells;
                         y++)
                    {
                        Vector2Int cell =
                            new Vector2Int(
                                centerCell.x + x,
                                centerCell.y + y
                            );

                        if (!board.IsInside(cell))
                            continue;

                        // Check actual world-space distance so
                        // corners aren't reserved unnecessarily.
                        Vector3 cellWorld =
                            board.CellToWorld(cell);

                        Vector3 flatPoint = point;
                        flatPoint.y = 0f;

                        cellWorld.y = 0f;

                        float allowedRadius =
                            safetyRadius +
                            cellSize * 0.5f;

                        if (Vector3.Distance(
                                cellWorld,
                                flatPoint)
                            <= allowedRadius)
                        {
                            board.Occupy(cell);
                        }
                    }
                }
            }
        }
    }


    private bool HasSafeInternalCurvedFallCorridors(
    CurvedPath path)
    {
        if (path == null ||
            path.positions == null ||
            path.directions == null ||
            path.positions.Count < 3)
        {
            return false;
        }

        Collider prefabCollider =
            dominoPrefab.GetComponentInChildren<Collider>();

        if (prefabCollider == null)
            return false;

        Bounds bounds =
            prefabCollider.bounds;

        float fallReach =
            bounds.size.y *
            endFallReachMultiplier;

        float dominoRadius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.z
            );

        float requiredClearance =
            dominoRadius * 2f +
            endFallPadding;

        int count =
            path.positions.Count;

        for (int i = 0; i < count; i++)
        {
            Vector3 start =
                path.positions[i];

            Vector3 direction =
    GetRuntimeCurvedFallDirection(
        path,
        i
    );

            start.y = 0f;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                continue;

            direction.Normalize();

            Vector3 end =
                start +
                direction * fallReach;

            for (int j = 0; j < count; j++)
            {
                if (i == j)
                    continue;

                // Immediate neighbors are SUPPOSED
                // to interact with each other.
                if (Mathf.Abs(i - j) <= 1)
                    continue;

                // Full circle:
                // first and last are also neighbors.
                bool wrapNeighbor =
                    (i == 0 && j == count - 1) ||
                    (j == 0 && i == count - 1);

                if (wrapNeighbor)
                    continue;

                Vector3 other =
                    path.positions[j];

                other.y = 0f;

                float distance =
                    DistancePointToSegmentXZ(
                        other,
                        start,
                        end
                    );

                if (distance < requiredClearance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool ValidatePlayableFallOrder()
    {
        Physics.SyncTransforms();

        HashSet<DominoLine> remaining =
            new HashSet<DominoLine>();

        foreach (DominoLine line in generatedLines)
        {
            if (line != null)
                remaining.Add(line);
        }

        int safetyCounter = 0;

        while (remaining.Count > 0)
        {
            safetyCounter++;

            // Absolute protection against accidental infinite loops.
            if (safetyCounter > generatedLines.Count + 5)
            {
                Debug.LogWarning(
                    "PHYSICAL VALIDATION SAFETY BREAK");

                return false;
            }


            // =====================================================
            // FIND CURRENTLY PLAYABLE LINES
            // =====================================================

            List<DominoLine> playableLines =
                new List<DominoLine>();

            foreach (DominoLine line in remaining)
            {
                if (line == null)
                    continue;

                bool blocked = false;

                foreach (DominoLine blocker
                         in line.blockedByLines)
                {
                    if (blocker == null)
                        continue;

                    // Blocker is still standing.
                    if (remaining.Contains(blocker))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                    playableLines.Add(line);
            }


            // =====================================================
            // NO PLAYABLE LINE = DEPENDENCY DEADLOCK
            // =====================================================

            if (playableLines.Count == 0)
            {
                Debug.LogWarning(
                    $"PHYSICAL VALIDATION DEADLOCK | " +
                    $"Remaining={remaining.Count}");

                return false;
            }


            // =====================================================
            // FIND ONE PLAYABLE + PHYSICALLY SAFE LINE
            // =====================================================

            DominoLine safeLine = null;

            foreach (DominoLine line in playableLines)
            {
                if (CanPlayableLineFallSafely(
                        line,
                        remaining))
                {
                    safeLine = line;
                    break;
                }
            }


            // =====================================================
            // PLAYABLE LINES EXIST,
            // BUT NONE CAN PHYSICALLY FALL
            // =====================================================

            if (safeLine == null)
            {
                Debug.LogWarning(
                    $"NO SAFE PLAYABLE LINE | " +
                    $"Playable={playableLines.Count} | " +
                    $"Remaining={remaining.Count}");

                return false;
            }


            // =====================================================
            // SIMULATE THIS LINE BEING COMPLETED
            // =====================================================

            remaining.Remove(safeLine);
        }


        Debug.Log(
            $"PHYSICAL PLAY ORDER VALID | " +
            $"Lines={generatedLines.Count}");

        return true;
    }

    private bool CanPlayableLineFallSafely(
    DominoLine playableLine,
    HashSet<DominoLine> standingLines)
    {
        if (playableLine == null ||
            playableLine.dominoes == null ||
            playableLine.dominoes.Count < 2)
        {
            return false;
        }

        // Make sure Physics queries see the latest
        // generated transforms.
        Physics.SyncTransforms();

        for (int i = 0;
             i < playableLine.dominoes.Count;
             i++)
        {
            Domino domino =
                playableLine.dominoes[i];

            if (domino == null)
                continue;

            Vector3 fallDirection =
                GetActualDominoFallDirection(
                    playableLine,
                    i);

            if (fallDirection.sqrMagnitude <
                0.001f)
            {
                return false;
            }

            if (DoesPlayableDominoHitStandingLine(
                    domino,
                    playableLine,
                    fallDirection,
                    standingLines))
            {
                Debug.LogWarning(
                    $"PLAYABLE LINE UNSAFE | " +
                    $"{playableLine.name} | " +
                    $"Domino={domino.name}");

                return false;
            }
        }

        return true;
    }


    private bool CanLineFallIntoOtherLine(
     DominoLine fallingLine,
     DominoLine otherLine,
     float fallReach
     )
    {
        if (fallingLine == null ||
            otherLine == null ||
            fallingLine.dominoes == null ||
            otherLine.dominoes == null ||
            fallingLine.dominoes.Count < 2)
        {
            return false;
        }

        for (int i = 0;
             i < fallingLine.dominoes.Count;
             i++)
        {
            Domino fallingDomino =
                fallingLine.dominoes[i];

            if (fallingDomino == null)
                continue;

            // ---------------------------------------------
            // ACTUAL RUNTIME FALL DIRECTION
            // ---------------------------------------------

            Vector3 fallDirection;

            if (i == 0)
            {
                Domino next =
                    fallingLine.dominoes[1];

                if (next == null)
                    continue;

                fallDirection =
                    next.transform.position -
                    fallingDomino.transform.position;
            }
            else
            {
                Domino previous =
                    fallingLine.dominoes[i - 1];

                if (previous == null)
                    continue;

                fallDirection =
                    fallingDomino.transform.position -
                    previous.transform.position;
            }

            fallDirection.y = 0f;

            if (fallDirection.sqrMagnitude < 0.001f)
                continue;

            fallDirection.Normalize();

            Vector3 sideDirection =
                Vector3.Cross(
                    Vector3.up,
                    fallDirection
                ).normalized;

            // ---------------------------------------------
            // GET REAL COLLIDER SIZE
            // ---------------------------------------------

            Collider fallingCollider =
                fallingDomino.GetComponentInChildren<Collider>();

            if (fallingCollider == null)
                continue;

            Bounds fallingBounds =
                fallingCollider.bounds;

            float fallingHalfWidth =
                Mathf.Min(
                    fallingBounds.extents.x,
                    fallingBounds.extents.z
                );

            // Small extra safety only.
            float sideSafety =
                fallingHalfWidth +
                endFallPadding;

            Vector3 start =
                fallingCollider.bounds.center;

            start.y = 0f;

            // ---------------------------------------------
            // CHECK OTHER LINE
            // ---------------------------------------------

            foreach (Domino otherDomino
                     in otherLine.dominoes)
            {
                if (otherDomino == null)
                    continue;

                Collider otherCollider =
                    otherDomino.GetComponentInChildren<Collider>();

                if (otherCollider == null)
                    continue;

                Vector3 otherPosition =
                    otherCollider.bounds.center;

                otherPosition.y = 0f;

                Vector3 relative =
                    otherPosition - start;

                float forward =
                    Vector3.Dot(
                        relative,
                        fallDirection
                    );

                float sideways =
                    Mathf.Abs(
                        Vector3.Dot(
                            relative,
                            sideDirection
                        )
                    );

                Bounds otherBounds =
                    otherCollider.bounds;

                float otherHalfWidth =
                    Mathf.Min(
                        otherBounds.extents.x,
                        otherBounds.extents.z
                    );

                // -----------------------------------------
                // FALL AREA
                // -----------------------------------------

                float allowedSideways =
                    sideSafety +
                    otherHalfWidth +
                    minimumDominoClearance;

                // IMPORTANT:
                // only check objects IN FRONT of the domino.
                //
                // This stops the validator from rejecting
                // nearby dominoes beside/behind the standing
                // domino at the beginning of the fall.
                float forwardStart =
                    fallingHalfWidth;

                float forwardEnd =
                    fallReach +
                    otherHalfWidth +
                    endFallPadding;

                bool insideForwardArea =
                    forward >= forwardStart &&
                    forward <= forwardEnd;

                bool insideSideArea =
                    sideways <= allowedSideways;

                if (insideForwardArea &&
                    insideSideArea)
                {
                    Debug.LogWarning(
                        $"REAL FALL COLLISION: " +
                        $"{fallingLine.name}/" +
                        $"{fallingDomino.name} -> " +
                        $"{otherLine.name}/" +
                        $"{otherDomino.name} | " +
                        $"Forward={forward:F3} " +
                        $"Side={sideways:F3} | " +
                        $"MaxForward={forwardEnd:F3} " +
                        $"MaxSide={allowedSideways:F3}"
                    );

                    return true;
                }
            }
        }

        return false;
    }

    private Vector3 GetRuntimeCurvedFallDirection(
    CurvedPath path,
    int index)
    {
        if (path == null ||
            path.positions == null ||
            path.positions.Count < 2 ||
            index < 0 ||
            index >= path.positions.Count)
        {
            return Vector3.zero;
        }

        Vector3 direction;

        // First domino is started manually.
        // Runtime StartChain() makes it fall toward domino 1.
        if (index == 0)
        {
            direction =
                path.positions[1] -
                path.positions[0];
        }
        else
        {
            // IMPORTANT:
            // Every later domino receives the direction from
            // the PREVIOUS domino when ActivateFromPrevious()
            // is called.
            direction =
                path.positions[index] -
                path.positions[index - 1];
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return direction.normalized;
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