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

    [Tooltip("Same level + same seed produces the same layout.")]
    public int baseSeed = 12345;


    // =========================================================
    // PREFABS
    // =========================================================

    [Header("Prefabs")]
    public Domino dominoPrefab;

    [Tooltip(
        "Prefab containing a DominoLine component. " +
        "It should NOT contain domino children."
    )]
    public DominoLine linePrefab;


    // =========================================================
    // BOARD
    // =========================================================

    [Header("Board")]

    [Tooltip("Grid width for early levels.")]
    public int baseColumns = 18;

    [Tooltip("Grid height for early levels.")]
    public int baseRows = 12;

    [Tooltip("Maximum width of later levels.")]
    public int maxColumns = 30;

    [Tooltip("Maximum height of later levels.")]
    public int maxRows = 20;

    [Tooltip(
        "Keep this equal to approximately your normal domino spacing."
    )]
    public float cellSize = 0.25f;

    [Tooltip("Height of dominoes above ground.")]
    public float groundOffset = 0.5f;


    // =========================================================
    // COVERAGE
    // =========================================================

    [Header("Coverage")]

    [Tooltip(
        "1 = line on every grid row. " +
        "2 = line every second row."
    )]
    [Min(1)]
    public int rowSpacing = 2;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Domino Rotation")]

    [Tooltip(
        "Use the same rotation offset that worked in DominoSplineBuilder."
    )]
    public Vector3 rotationOffset =
        new Vector3(0f, 90f, 0f);


    // =========================================================
    // GENERATED DATA
    // =========================================================

    [Header("Runtime Debug")]

    [SerializeField]
    private int generatedDominoCount;

    [SerializeField]
    private int generatedLineCount;

    private GridBoard board;

    private Transform generatedRoot;


    // =========================================================
    // PUBLIC GENERATION
    // =========================================================

    public void GenerateLevel()
    {
        ClearLevel();

        if (!ValidateSettings())
            return;

        int seed =
            baseSeed +
            levelNumber * 7919;

        Random.InitState(seed);

        int columns =
            CalculateColumns();

        int rows =
            CalculateRows();

        board =
            new GridBoard(
                columns,
                rows,
                cellSize,
                transform.position
            );

        CreateGeneratedRoot();

        generatedDominoCount = 0;
        generatedLineCount = 0;

        GenerateHorizontalCoverageLines();

        Debug.Log(
            $"Generated Level {levelNumber}. " +
            $"Lines: {generatedLineCount}, " +
            $"Dominoes: {generatedDominoCount}, " +
            $"Grid: {columns} x {rows}, " +
            $"Seed: {seed}"
        );
    }


    // =========================================================
    // BOARD DIFFICULTY
    // =========================================================

    private int CalculateColumns()
    {
        // Every 5 levels make the board slightly wider.

        int increases =
            (levelNumber - 1) / 5;

        int columns =
            baseColumns +
            increases * 2;

        return Mathf.Clamp(
            columns,
            baseColumns,
            maxColumns
        );
    }

    private int CalculateRows()
    {
        // Every 10 levels add more board depth.

        int increases =
            (levelNumber - 1) / 10;

        int rows =
            baseRows +
            increases * 2;

        return Mathf.Clamp(
            rows,
            baseRows,
            maxRows
        );
    }


    // =========================================================
    // GENERATION
    // =========================================================

    private void GenerateHorizontalCoverageLines()
    {
        int lineIndex = 0;

        for (
            int y = 0;
            y < board.Height;
            y += rowSpacing)
        {
            List<Vector2Int> path =
                new List<Vector2Int>();


            // Alternate direction:
            //
            // line 0:  >>>>>>>>
            // line 1:  <<<<<<<<
            // line 2:  >>>>>>>>

            bool leftToRight =
                lineIndex % 2 == 0;


            if (leftToRight)
            {
                for (
                    int x = 0;
                    x < board.Width;
                    x++)
                {
                    Vector2Int cell =
                        new Vector2Int(x, y);

                    if (!board.CanOccupy(cell))
                        continue;

                    path.Add(cell);
                }
            }
            else
            {
                for (
                    int x = board.Width - 1;
                    x >= 0;
                    x--)
                {
                    Vector2Int cell =
                        new Vector2Int(x, y);

                    if (!board.CanOccupy(cell))
                        continue;

                    path.Add(cell);
                }
            }


            if (path.Count >= 2)
            {
                CreateLine(
                    path,
                    lineIndex
                );

                lineIndex++;
            }
        }
    }


    // =========================================================
    // CREATE LINE
    // =========================================================

    private void CreateLine(
        List<Vector2Int> path,
        int lineIndex)
    {
        DominoLine line =
            Instantiate(
                linePrefab,
                generatedRoot
            );

        line.name =
            $"Generated_Line_{lineIndex:00}";


        for (
            int i = 0;
            i < path.Count;
            i++)
        {
            Vector2Int cell =
                path[i];


            // Mark occupied BEFORE another line can use it.

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


            // Determine path direction.

            Vector3 direction =
                GetPathDirection(
                    path,
                    i
                );


            if (direction.sqrMagnitude > 0.001f)
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


        // Your existing DominoLine code now connects
        // domino 0 -> domino 1 -> domino 2 etc.

        line.AutoConnect();

        generatedLineCount++;
    }


    // =========================================================
    // PATH DIRECTION
    // =========================================================

    private Vector3 GetPathDirection(
        List<Vector2Int> path,
        int index)
    {
        Vector2Int from;
        Vector2Int to;


        if (index < path.Count - 1)
        {
            from = path[index];
            to = path[index + 1];
        }
        else
        {
            from = path[index - 1];
            to = path[index];
        }


        Vector3 fromWorld =
            board.CellToWorld(from);

        Vector3 toWorld =
            board.CellToWorld(to);


        Vector3 direction =
            toWorld -
            fromWorld;

        direction.y = 0f;

        return direction.normalized;
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

        if (existing == null)
            return;


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

        generatedRoot = null;

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
                "ProceduralLevelGenerator: " +
                "Domino Prefab is missing."
            );

            return false;
        }


        if (linePrefab == null)
        {
            Debug.LogError(
                "ProceduralLevelGenerator: " +
                "Line Prefab is missing."
            );

            return false;
        }


        if (baseColumns < 2)
        {
            Debug.LogError(
                "Base Columns must be at least 2."
            );

            return false;
        }


        if (baseRows < 1)
        {
            Debug.LogError(
                "Base Rows must be at least 1."
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


        return true;
    }


    // =========================================================
    // DEBUG GRID
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        int columns =
            Application.isPlaying &&
            board != null
                ? board.Width
                : CalculateColumns();

        int rows =
            Application.isPlaying &&
            board != null
                ? board.Height
                : CalculateRows();


        if (columns <= 0 ||
            rows <= 0 ||
            cellSize <= 0f)
        {
            return;
        }


        float width =
            (columns - 1) *
            cellSize;

        float height =
            (rows - 1) *
            cellSize;


        Vector3 bottomLeft =
            transform.position -
            new Vector3(
                width * 0.5f,
                0f,
                height * 0.5f
            );


        // Vertical grid lines

        for (
            int x = 0;
            x < columns;
            x++)
        {
            Vector3 start =
                bottomLeft +
                new Vector3(
                    x * cellSize,
                    0.02f,
                    0f
                );

            Vector3 end =
                start +
                Vector3.forward *
                height;

            Gizmos.DrawLine(
                start,
                end
            );
        }


        // Horizontal grid lines

        for (
            int y = 0;
            y < rows;
            y++)
        {
            Vector3 start =
                bottomLeft +
                new Vector3(
                    0f,
                    0.02f,
                    y * cellSize
                );

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