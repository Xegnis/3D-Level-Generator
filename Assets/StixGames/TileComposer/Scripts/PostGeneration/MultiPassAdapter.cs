using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StixGames.TileComposer;
using UnityEngine;

public class MultiPassAdapter : MonoBehaviour
{
    public TileCollection Origin;
    public TileComposer Target;

    public BlockedTileConversion[] BlockedTileConversions;

    public FixedTileConversion[] FixedTileConversions;

    public void NextPass(ModelGeneratedEventData data)
    {
        if (data.InstantiatedParent != null)
        {
            Debug.LogWarning(
                "When using multiple TileComposer passes, you should disable instantiation on the first TileComposer, or you will create multiple instantiations!",
                this);
        }

        // Create a copy of the target TileComposer
        var target = Instantiate(Target);
        target.DestroyAfterUse = true;
        target.GenerateOnStart = false;

        var blockedTiles = new List<TileSlice>(target.BlockedTiles);
        var fixedTiles = new List<TileSlice>(target.FixedTiles);

        var blockedDict = BlockedTileConversions.ToDictionary(x => x.SourceTile, x => x);
        var fixedDict = FixedTileConversions.ToDictionary(x => x.SourceTile, x => x);

        for (var i = 0; i < data.Model.Length; i++)
        {
            var tileType = data.Model[i].TileTypeName;
            var slice = data.Grid.IndexToCoordinates(i).Select(x => new Slice(x, x)).ToArray();

            if (blockedDict.TryGetValue(tileType, out var block))
            {
                foreach (var blockedType in block.BlockedTiles)
                {
                    blockedTiles.Add(new TileSlice(blockedType, slice));
                }
            }
            
            if (fixedDict.TryGetValue(tileType, out var fixedTile))
            {
                fixedTiles.Add(new TileSlice(fixedTile.TargetTile, slice));
            }
        }

        target.BlockedTiles = blockedTiles.ToArray();
        target.FixedTiles = fixedTiles.ToArray();

        Debug.Log("Generating next pass");
        if (target.GenerateAsynchronously)
        {
#pragma warning disable 4014
            target.GenerateAsync(data.RegisterUndo);
#pragma warning restore 4014
        }
        else
        {
            target.Generate(data.RegisterUndo);
        }
    }
}

[Serializable]
public class BlockedTileConversion
{
    public string SourceTile;
    public string[] BlockedTiles;
}

[Serializable]
public class FixedTileConversion
{
    public string SourceTile;
    public string TargetTile;
}