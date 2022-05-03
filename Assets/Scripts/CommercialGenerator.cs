using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommercialGenerator : Generator
{
    public GameObject diggerPrefab;

    public override void Generate()
    {
        grid = new Grid2D<Node>(xSize, ySize);
        grid.Initialize();
        grid.origin = transform.position;
        Vector3 pos = grid.origin;
        Digger digger = Instantiate(diggerPrefab, pos, Quaternion.identity).GetComponent<Digger>();
        digger.grid = grid;
        digger.commercialGenerator = gameObject;
        for (int i = 0; i < 100; i++)
        {
            digger.DigOnce();
        }
    }
}
