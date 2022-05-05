using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StixGames.TileComposer;

public class WaveFunctionCollapseIndustrial : WaveFunctionCollapse
{
    /*public override void Generate()
    {
        TileComposer tc = Instantiate(tileComposer, transform.position, Quaternion.identity);
        tc.transform.SetParent(transform);
        tc.GridSize = new int[] { (int)Mathf.Min(14, dimensions[0]), (int)Mathf.Min(12, dimensions[1]), (int)Mathf.Min(14, dimensions[2]) };
        GenerateFixedTiles();
        tc.FixedTiles = fixedTiles;
        tc.GenerateOnStart = true;
    }*/

    public override void GenerateFixedTiles()
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

       
        fixedTiles[7].Dimensions[0].Start = Random.Range(dimensions[0] / 3, dimensions[0] * 2 / 3);
        fixedTiles[7].Dimensions[0].End = fixedTiles[7].Dimensions[0].Start;
        fixedTiles[7].Dimensions[1].Start = dimensions[1] / 2;
        fixedTiles[7].Dimensions[1].End = fixedTiles[7].Dimensions[1].Start;
        fixedTiles[7].Dimensions[2].Start = Random.Range(dimensions[2] / 3, dimensions[2] * 2 / 3);
        fixedTiles[7].Dimensions[2].End = fixedTiles[7].Dimensions[2].Start;

        fixedTiles[6].Dimensions[0].End = dimensions[0] - 3;
        //fixedTiles[6].Dimensions[0].End = fixedTiles[7].Dimensions[1].Start;
        fixedTiles[6].Dimensions[2].End = dimensions[2] - 3;

    }
}
