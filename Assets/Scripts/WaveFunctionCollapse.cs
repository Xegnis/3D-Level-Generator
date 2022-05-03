using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapse : Generator
{

    public WFCBlock[,,] blocks;
    public int minHeight, maxHeight;
    public GameObject blankBlock;

    public override void Generate()
    {
        
    }

    public void Iterate ()
    {
        for (int x = 0; x < blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(1); y++)
            {
                for (int z = 0; z < blocks.GetLength(2); z++)
                {
                    if (blocks[x, y, z] != null)
                        continue;
                    if (x == 0 || x == blocks.GetLength(0) - 1 ||
                        y == 0 || y == blocks.GetLength(1) - 1 ||
                        z == blocks.GetLength(2) - 1)
                    {

                    }
                }
            }
        }
    }
    /*
    public List<GameObject> GetLegalBlock (int x, int y, int z)
    {
        List<GameObject> list = new List<GameObject>();
        if (x == 0 || x == blocks.GetLength(0) - 1 ||
            y == 0 || y == blocks.GetLength(1) - 1 ||
            z == blocks.GetLength(2) - 1)
        {
            list.Add(blankBlock);
        }
        else
        {
            bool done = false;
            if (!done && blocks[x - 1, y, z] != null)
            {
                done = true;
                foreach (GameObject block in blocks[x - 1, y, z].neighborsRight)
                {
                    if (CheckValidBlock(block, x, y, z))
                    {
                        list.Add(block);
                    }
                }
            }
            if (!done && blocks[x + 1, y, z] != null)
            {
                done = true;
                foreach (GameObject block in blocks[x + 1, y, z].neighborsLeft)
                {
                    if (CheckValidBlock(block, x, y, z))
                    {
                        list.Add(block);
                    }
                }
            }

        }
    }

    public bool CheckValidBlock (GameObject block, int x, int y, int z)
    {
        if (x > 0 && blocks[x - 1, y, z] != null)
        {
            if (!blocks[x - 1, y, z].neighborsRight.Contains(block))
                return false;
        }
        if (x < blocks.GetLength(0) - 1 && blocks[x + 1, y, z] != null)
        {
            if (!blocks[x + 1, y, z].neighborsLeft.Contains(block))
                return false;
        }
        if (y > 0 && blocks[x, y - 1, z] != null)
        {
            if (!blocks[x, y - 1, z].neighborsBack.Contains(block))
                return false;
        }
        if (y < blocks.GetLength(1) - 1 && blocks[x, y + 1, z] != null)
        {
            if (!blocks[x, y + 1, z].neighborsFront.Contains(block))
                return false;
        }
        if (z > 0 && blocks[x, y, z - 1] != null)
        {
            if (!blocks[x, y, z - 1].neighborsUp.Contains(block))
                return false;
        }
        if (z < blocks.GetLength(2) - 1 && blocks[x, y, z + 1] != null)
        {
            if (!blocks[x, y, z + 1].neighborsDown.Contains(block))
                return false;
        }
        return true;
    }*/
}
