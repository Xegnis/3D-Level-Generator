using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    [CustomEditor(typeof(ExampleTile))]
    [CanEditMultipleObjects]
    public class ExampleTileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var exampleModelTileCollections =
                targets
                    .Cast<ExampleTile>()
                    .Select(x => x.GetComponentInParent<ExampleModel>()?.TileCollection)
                    .Where(x => x != null)
                    .Distinct()
                    .ToArray();

            var tileCollectionsCount = exampleModelTileCollections.Length;

            if (tileCollectionsCount == 0)
            {
                EditorGUILayout.LabelField(
                    "The selected example tile is not part of an example model, or the model doesn't have a tile collection assigned.");
                return;
            }

            if (tileCollectionsCount > 1)
            {
                EditorGUILayout.LabelField("Can't edit example tiles from multiple tile collections at the same time.");
                return;
            }

            EditorGUILayout.LabelField("Editor tools", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Change GameObject name to tile type",
                "Changes the GameObjects name to be the same as the selected tile type"))
            )
            {
                var tiles = targets.Cast<ExampleTile>().Where(x => !string.IsNullOrWhiteSpace(x.TileType)).ToArray();

                Undo.RecordObjects(tiles.Select(x => (Object) x.gameObject).ToArray(),
                    "Change GameObject name to tile type");

                foreach (var tile in tiles)
                {
                    tile.gameObject.name = tile.TileType;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

            var collection = exampleModelTileCollections[0];

            var tileType = serializedObject.FindProperty(nameof(ExampleTile.TileType));

            bool tileTypeChanged;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                TileComposerEditorUtility.TileTypeSelector(collection, tileType, false, false);

                tileTypeChanged = check.changed;
            }

            serializedObject.ApplyModifiedProperties();
            
            // After all editor code finished, update the cloned tile
            if (tileTypeChanged)
            {
                var baseTiles = collection.GetTiles(true).Where(x => x.BaseTile == null).ToArray();

                foreach (var tile in targets.Cast<ExampleTile>())
                {
                    Undo.RecordObject(tile, "Update tile models");
                    if (tile.CurrentTile != null)
                    {
                        Undo.RecordObject(tile.CurrentTile, "Update tile models");
                    }

                    tile.CloneTarget(baseTiles);

                    if (tile.CurrentTile != null)
                    {
                        Undo.RegisterCreatedObjectUndo(tile.CurrentTile, "Update tile models");
                    }
                }
                    
                serializedObject.Update();
            }
        }
        
        private void OnSceneGUI()
        {
            var tile = (target as ExampleTile);
            var model = tile?.GetComponentInParent<ExampleModel>();

            if (model == null)
            {
                return;
            }
            
            var modelTransform = model.transform;
            var collection = model.TileCollection;

            if (collection == null)
            {
                return;
            }
            
            var grid = collection.GetGrid(model.GridSize);

            // Create grid
            var lines = grid.GetModelFrame().Select(x => modelTransform.TransformPoint(x)).ToArray();
            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawDottedLines(lines, 10);
            
            // Make sure the tile is always moved to grid positions
            var t = tile.transform;
            var index = model.GetIndex(t.localPosition);
            var pos = grid.GetPosition(index);

            t.transform.localPosition = pos;
            
            // Snap to rotation
            var (axis, steps) = model.GetRotation(index, t.localRotation);
            var rotation = grid.GetTileRotation(index) * grid.GetRotation(axis, steps);
            
            t.localRotation = rotation;
        }
    }
}