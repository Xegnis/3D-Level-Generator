using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResidentialGenerator : Generator
{ 
    [Header("Prefabs")]
    public GameObject smallHousePrefab;
    public GameObject mediumHouseHPrefab;
    public GameObject mediumHouseVPrefab;
    public GameObject largeHousePrefab;
    public GameObject parkPrefab;

    [Header("Chances")]
    public float mediumBuildingChance;
    public float largeBuildingChance;
    public float parkChance;

    public override void Generate ()
    {
        grid = new Grid2D<Node>(xSize, ySize);
        grid.Initialize();
        grid.origin = transform.position;
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                if (grid.GetAt(x, y).completed)
                {
                    continue;
                }
                if (CanBuildPark(x, y))
                {
                    BuildPark(x, y);
                }
                else if (CanBuildLarge(x, y))
                {
                    BuildLarge(x, y);
                }
                else if (CanBuildMediumH(x, y))
                {
                    BuildMediumH(x, y);
                }
                else if (CanBuildMediumV(x, y))
                {
                    BuildMediumV(x, y);
                }
                else
                {
                    BuildSmall(x, y);
                }
            }
        }
    }

    bool CanBuildSmall (int x, int y)
    {
        if (x > grid.xSize - 16 || y > grid.ySize - 16)
            return false;
        for (int a = 0; a < 16; a++)
        {
            for (int b = 0; b < 16; b++)
            {
                if (grid.GetAt(x + a, y + b).completed)
                    return false;
            }
        }
        return true;
    }

    bool CanBuildMediumH(int x, int y)
    {
        if (x > grid.xSize - 32 || y > grid.ySize - 16)
            return false;
        for (int a = 0; a < 32; a++)
        {
            for (int b = 0; b < 16; b++)
            {
                if (grid.GetAt(x + a, y + b).completed)
                    return false;
            }
        }
        if (Random.value < mediumBuildingChance)
            return true;
        return false;
    }

    bool CanBuildMediumV(int x, int y)
    {
        if (x > grid.xSize - 16 || y > grid.ySize - 32)
            return false;
        for (int a = 0; a < 16; a++)
        {
            for (int b = 0; b < 32; b++)
            {
                if (grid.GetAt(x + a, y + b).completed)
                    return false;
            }
        }
        if (Random.value < mediumBuildingChance)
            return true;
        return false;
    }

    bool CanBuildLarge(int x, int y)
    {
        if (x > grid.xSize - 32 || y > grid.ySize - 32)
            return false;
        for (int a = 0; a < 32; a++)
        {
            for (int b = 0; b < 32; b++)
            {
                if (grid.GetAt(x + a, y + b).completed)
                    return false;
            }
        }
        if (Random.value < largeBuildingChance)
            return true;
        return false;
    }

    bool CanBuildPark (int x, int y)
    {
        if (x > grid.xSize - 48 || y > grid.ySize - 48)
            return false;
        for (int a = 0; a < 48; a++)
        {
            for (int b = 0; b < 48; b++)
            {
                if (grid.GetAt(x + a, y + b).completed)
                    return false;
            }
        }
        if (Random.value < parkChance)
            return true;
        return false;
    }

    void BuildSmall (int x, int y)
    {
        for (int a = 0; a < 16; a++)
        {
            for (int b = 0; b < 16; b++)
            {
                grid.GetAt(x + a, y + b).completed = true;
            }
        }
        SpawnBlock(smallHousePrefab, x, y);
    }

    void BuildMediumH (int x, int y)
    {
        for (int a = 0; a < 32; a++)
        {
            for (int b = 0; b < 16; b++)
            {
                grid.GetAt(x + a, y + b).completed = true;
            }
        }
        SpawnBlock(mediumHouseHPrefab, x, y);
    }

    void BuildMediumV(int x, int y)
    {
        for (int a = 0; a < 16; a++)
        {
            for (int b = 0; b < 32; b++)
            {
                grid.GetAt(x + a, y + b).completed = true;
            }
        }
        SpawnBlock(mediumHouseVPrefab, x, y);
    }

    void BuildLarge (int x, int y)
    {
        for (int a = 0; a < 32; a++)
        {
            for (int b = 0; b < 32; b++)
            {
                grid.GetAt(x + a, y + b).completed = true;
            }
        }
        SpawnBlock(largeHousePrefab, x, y);
    }

    void BuildPark (int x, int y)
    {
        for (int a = 0; a < 48; a++)
        {
            for (int b = 0; b < 48; b++)
            {
                grid.GetAt(x + a, y + b).completed = true;
            }
        }
        SpawnBlock(parkPrefab, x, y);
    }

    
}
