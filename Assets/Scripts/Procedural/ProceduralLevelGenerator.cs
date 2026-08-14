using System.Collections.Generic;
using UnityEngine;

public class ProceduralLevelGenerator : MonoBehaviour
{
    // =========================================================
    // LEVEL
    // =========================================================

    [Header("Level")]
    [Min(1)]
    public int levelNumber = 1;

    [Tooltip("Same level + same seed produces the same puzzle.")]
    public int baseSeed = 12345;


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


    // =========================================================
    // BOARD
    // =========================================================

    [Header("Board")]


    [Tooltip("Distance between neighboring domino grid positions.")]
    [Min(0.01f)]
    public float cellSize = 0.25f;

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
        ClearLevel();

        if (!ValidateSettings())
            return;


        // -----------------------------------------------------
        // CALCULATE LEVEL SIZE
        // -----------------------------------------------------

        Vector2 groundSize =
            CalculateGroundSize();


        // -----------------------------------------------------
        // RESIZE GROUND
        // -----------------------------------------------------

        ResizeGround(
            groundSize
        );


        // -----------------------------------------------------
        // SEED
        // -----------------------------------------------------

        int seed =
            baseSeed +
            levelNumber * 7919;

        Random.InitState(
            seed
        );


        // -----------------------------------------------------
        // GRID SIZE
        // -----------------------------------------------------

        int columns =
            CalculateColumns();

        int rows =
            CalculateRows();


        // -----------------------------------------------------
        // BOARD CENTER
        // -----------------------------------------------------

        Vector3 boardCenter =
            groundRenderer != null
                ? groundRenderer.bounds.center
                : transform.position;

        // Grid itself lives at ground level.
        boardCenter.y =
            transform.position.y;


        // -----------------------------------------------------
        // CREATE GRID
        // -----------------------------------------------------

        board =
            new GridBoard(
                columns,
                rows,
                cellSize,
                boardCenter
            );


        // -----------------------------------------------------
        // CREATE GENERATED ROOT
        // -----------------------------------------------------

        CreateGeneratedRoot();


        generatedDominoCount = 0;
        generatedLineCount = 0;

        generatedLines.Clear();


        // -----------------------------------------------------
        // CREATE NON-CROSSING COVERAGE
        // -----------------------------------------------------

        GenerateSegmentedCoverage();


        // -----------------------------------------------------
        // CREATE GUARANTEED-SOLVABLE DEPENDENCIES
        // -----------------------------------------------------

        GenerateDependencies();


        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        Debug.Log(
            $"Generated Level {levelNumber} | " +
            $"Ground {groundSize.x:F2} x {groundSize.y:F2} | " +
            $"Grid {columns} x {rows} | " +
            $"Lines {generatedLineCount} | " +
            $"Dominoes {generatedDominoCount} | " +
            $"Seed {seed}"
        );
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


        for (
            int y = 0;
            y < board.Height;
            y += rowSpacing)
        {
            // Random direction per row.
            bool leftToRight =
                Random.value >= 0.5f;


            if (leftToRight)
            {
                GenerateRowLeftToRight(
                    y,
                    ref lineIndex
                );
            }
            else
            {
                GenerateRowRightToLeft(
                    y,
                    ref lineIndex
                );
            }
        }
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