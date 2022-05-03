using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StixGames;

public class Generator : MonoBehaviour
{
    public Grid2D<Node> grid;
    public Vector3 origin;
    public int xSize, ySize;

    public virtual void Generate ()
    {
        
    }

    public void SpawnBlock(GameObject prefab, int x, int y)
    {
        GameObject block = Instantiate(prefab, grid.GridToWorld(x, y), Quaternion.identity);
        block.transform.SetParent(gameObject.transform);
    }
}
