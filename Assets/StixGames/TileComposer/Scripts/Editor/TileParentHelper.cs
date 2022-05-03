using System.Collections.Generic;
using StixGames.TileComposer;
using UnityEditor;
using UnityEngine;

public class TileParentHelper : Editor
{
    //[MenuItem("Tools/Wave Function Collapse/Create empty parent for each selected")]
    public static void ParentEach()
    {
        if (Selection.transforms == null)
        {
            return;
        }

        Undo.SetCurrentGroupName("Create empty parent for each selected");

        foreach (var o in Selection.transforms)
        {
            var originalParent = o.parent;

            CreateTileParent(o, originalParent, null, false);
        }
    }

    [MenuItem("Tools/Stix Games | Tile Composer/Create tile component on selected")]
    public static void AddTileComponent()
    {
        if (Selection.transforms == null)
        {
            return;
        }

        Undo.SetCurrentGroupName("Create tile component on selected");

        foreach (var obj in Selection.transforms)
        {
            var tile = Undo.AddComponent<Tile>(obj.gameObject);
            tile.TileType = obj.name;
        }
    }
    
    [MenuItem("Tools/Stix Games | Tile Composer/Create tile hierarchy from selected")]
    public static void ParentHierarchy()
    {
        if (Selection.transforms == null)
        {
            return;
        }

        Undo.SetCurrentGroupName("Create tile hierarchy from selected");

        foreach (var o in Selection.transforms)
        {
            var originalParent = o.parent;

            CreateTileParent(o, originalParent, null, true);
        }
    }

    [MenuItem("Tools/Stix Games | Tile Composer/Create tile variation")]
    public static void CreateTileVariation()
    {
        if (Selection.transforms == null)
        {
            return;
        }
        
        Undo.SetCurrentGroupName("Create tile hierarchy from selected");

        for (var i = 0; i < Selection.transforms.Length; i++)
        {
            var selection = Selection.transforms[i];
            var newInstance = Instantiate(selection, selection.parent);
            newInstance.name = $"{selection.name} Variation ({i+1})";

            Undo.RegisterCreatedObjectUndo(newInstance.gameObject, "Create tile hierarchy from selected");
            
            var tile = newInstance.GetComponent<Tile>();
            if (tile != null)
            {
                tile.BaseTile = selection.GetComponent<Tile>();
            }
        }
    }
    
    [MenuItem("Tools/Stix Games | Tile Composer/Create tile replacement")]
    public static void CreateTileModification()
    {
        if (Selection.transforms == null)
        {
            return;
        }
        
        Undo.SetCurrentGroupName("Create tile hierarchy from selected");

        var newSelection = new List<Object>();
        foreach (var selection in Selection.transforms)
        {
            var newInstance = Instantiate(selection, selection.parent);
            newInstance.name = $"{selection.name} Replacement";

            Undo.RegisterCreatedObjectUndo(newInstance.gameObject, "Create tile hierarchy from selected");
            
            var tile = newInstance.GetComponent<Tile>();
            if (tile != null)
            {
                tile.BaseTile = selection.GetComponent<Tile>();
            }
            
            Undo.RecordObject(selection.gameObject, "Set old object inactive");
            selection.gameObject.SetActive(false);
            
            newSelection.Add(tile.gameObject);
        }

        Selection.objects = newSelection.ToArray();
    }
    
    private static void CreateTileParent(Transform target, Transform parent, Tile baseTile, bool isRecursive)
    {
        // You can't change the transforms while iterating, so I'm saving the necessary operations in a list
        // and execute them when everything else is finished
        var opList = new List<(Transform, Transform)>();
        
        CreateTileParent(opList, target, baseTile, isRecursive);

        foreach (var (t, p) in opList)
        {
            Undo.SetTransformParent(t, p, "Set new parent");
            Undo.SetTransformParent(p, parent, "Set new parent");
        }
    }
    
    private static void CreateTileParent(List<(Transform, Transform)> operations, Transform target, Tile baseTile, bool isRecursive)
    {
        var newParent = new GameObject(target.name).transform;
        Undo.RegisterCreatedObjectUndo(newParent.gameObject, "Create new parent");
        
        newParent.position = target.position;

        var tile = Undo.AddComponent<Tile>(newParent.gameObject);

        if (baseTile == null)
        {
            tile.TileType = target.name;
        }
        else
        {
            tile.TileType = baseTile.TileType;
            tile.BaseTile = baseTile;
        }
        
        operations.Add((target, newParent));

        if (!isRecursive)
        {
            return;
        }

        foreach (Transform child in target)
        {
            CreateTileParent(operations, child, tile, true);
        }
    }
}