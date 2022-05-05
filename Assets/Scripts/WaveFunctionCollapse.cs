using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StixGames.TileComposer;

public class WaveFunctionCollapse : Generator
{
    public TileComposer tileComposer;
    public int[] dimensions;
    public TileSlice[] fixedTiles;

    public override void Generate()
    {
        TileComposer tc = Instantiate(tileComposer, transform.position, Quaternion.identity);
        tc.transform.SetParent(transform);
        tc.GridSize = dimensions;
        GenerateFixedTiles();
        tc.FixedTiles = fixedTiles;
        tc.GenerateOnStart = true;
    }

    void Start()
    {
        //Generate();
    }

    void GenerateFixedTiles ()
    {
        fixedTiles[0].Dimensions[1].End = dimensions[1] - 1;
        fixedTiles[0].Dimensions[2].End = dimensions[2] - 1;

        fixedTiles[1].Dimensions[0].End = dimensions[0] - 1;
        fixedTiles[1].Dimensions[1].End = dimensions[1] - 1;

        fixedTiles[2].Dimensions[0].Start = dimensions[0] - 1;
        fixedTiles[2].Dimensions[0].End = dimensions[0] - 1;
        fixedTiles[2].Dimensions[1].End = dimensions[1] - 1;
        fixedTiles[2].Dimensions[2].End = dimensions[2] - 1;

        fixedTiles[3].Dimensions[0].End = dimensions[0] - 1;
        fixedTiles[3].Dimensions[1].End = dimensions[1] - 1;
        fixedTiles[3].Dimensions[2].Start = dimensions[2] - 1;
        fixedTiles[3].Dimensions[2].End = dimensions[2] - 1;

        fixedTiles[4].Dimensions[0].End = dimensions[0] - 1;
        fixedTiles[4].Dimensions[1].Start = dimensions[1] - 1;
        fixedTiles[4].Dimensions[1].End = dimensions[1] - 1;
        fixedTiles[4].Dimensions[2].End = dimensions[2] - 1;
    }
}
