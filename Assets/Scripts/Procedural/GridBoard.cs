using System.Collections.Generic;
using UnityEngine;

public class GridBoard
{
    private readonly int width;
    private readonly int height;

    private readonly float cellSize;

    private readonly Vector3 origin;

    private readonly bool[,] occupied;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    public GridBoard(
        int width,
        int height,
        float cellSize,
        Vector3 center)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        occupied = new bool[width, height];

        float worldWidth =
            (width - 1) * cellSize;

        float worldHeight =
            (height - 1) * cellSize;

        origin =
            center -
            new Vector3(
                worldWidth * 0.5f,
                0f,
                worldHeight * 0.5f
            );
    }

    // ---------------------------------------------------------
    // VALIDATION
    // ---------------------------------------------------------

    public bool IsInside(Vector2Int cell)
    {
        return
            cell.x >= 0 &&
            cell.x < width &&
            cell.y >= 0 &&
            cell.y < height;
    }

    public bool IsOccupied(Vector2Int cell)
    {
        if (!IsInside(cell))
            return true;

        return occupied[cell.x, cell.y];
    }

    public bool CanOccupy(Vector2Int cell)
    {
        if (!IsInside(cell))
            return false;

        return !occupied[cell.x, cell.y];
    }

    // ---------------------------------------------------------
    // OCCUPANCY
    // ---------------------------------------------------------

    public void Occupy(Vector2Int cell)
    {
        if (!IsInside(cell))
            return;

        occupied[cell.x, cell.y] = true;
    }

    public void Free(Vector2Int cell)
    {
        if (!IsInside(cell))
            return;

        occupied[cell.x, cell.y] = false;
    }

    // ---------------------------------------------------------
    // WORLD POSITION
    // ---------------------------------------------------------

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return origin +
               new Vector3(
                   cell.x * cellSize,
                   0f,
                   cell.y * cellSize
               );
    }

    // ---------------------------------------------------------
    // DEBUG
    // ---------------------------------------------------------

    public List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> result =
            new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (occupied[x, y])
                {
                    result.Add(
                        new Vector2Int(x, y)
                    );
                }
            }
        }

        return result;
    }
}