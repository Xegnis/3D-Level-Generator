using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid2D<T>
    where T : new()
{
    public T[,] objects { get; }
    public int xSize { get; }
    public int ySize { get; }
    public float tileSize { get; }

    public float[,] zOffset { get; set; }
    public bool[,] isEmpty { get; set; }

    public Vector3 origin { get; set; }

    protected const float TILE_SIZE = 1f;

    public Grid2D(int xSize = 64, int ySize = 64, float tileSize = TILE_SIZE)
    {
        objects = new T[xSize, ySize];
        this.xSize = xSize;
        this.ySize = ySize;
        this.tileSize = tileSize;
        zOffset = new float[xSize, ySize];
        isEmpty = new bool[xSize, ySize];
        origin = Vector3.zero;
    }

    public void Clear()
    {
        for (int r = 0; r < xSize; r++)
        {
            for (int c = 0; c < ySize; c++)
            {
                objects[r, c] = default(T);
                isEmpty[r, c] = true;
            }
        }
    }

    public void SetAt(T o, int x, int y)
    {
        objects[x, y] = o;
        isEmpty[x, y] = false;
    }

    public void RemoveAt(int x, int y)
    {
        objects[x, y] = default(T);
        isEmpty[x, y] = true;
    }

    public T GetAt(int x, int y)
    {
        if (!isEmpty[x, y])
        {
            return objects[x, y];
        }
        else
        {
            return default(T);
        }
    }

    public bool IsEmptyAt (int x, int y)
    {
        return isEmpty[x, y];
    }

    public void Initialize()
    {
        for (int r = 0; r < xSize; r++)
        {
            for (int c = 0; c < ySize; c++)
            {
                objects[r, c] = new T();
                isEmpty[r, c] = false;
            }
        }
    }

    public Vector3 GridToWorld(int x, int y)
    {
        float xPos = origin.x + x * tileSize;
        float yPos = origin.z + y * tileSize;
        return new Vector3(xPos, zOffset[x, y], yPos);
    }

    public Vector2Int WorldToGrid(float x, float y)
    {
        int xIndex = Mathf.FloorToInt((x - origin.x) / tileSize);
        int yIndex = Mathf.FloorToInt((y - origin.z) / tileSize);
        return new Vector2Int(Mathf.Max(xIndex, 0), Mathf.Max(yIndex, 0));
    }

    public void Import (Grid2D<T> newGrid, int x = 0, int y = 0)
    {
        for (int i = 0; i < newGrid.xSize; i++)
        {
            for (int c = 0; c < newGrid.ySize; c++)
            {
                if (x + i > xSize - 1 || y + c > ySize - 1)
                    continue;
                SetAt(newGrid.GetAt(i, c), x + i, y + c);
                zOffset[x + i, y + c] = newGrid.zOffset[i, c];
                isEmpty[x + i, y + c] = newGrid.isEmpty[i, c];
            }
        }
    }
}