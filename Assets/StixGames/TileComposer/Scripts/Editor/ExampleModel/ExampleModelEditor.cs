using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    [CustomEditor(typeof(ExampleModel))]
    [CanEditMultipleObjects]
    public class ExampleModelEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Editor tools", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Update tile models",
                "If you've changed some of the tiles in the tile collection, " +
                "you can press this button to update all example tiles to the new model."))
            )
            {
                foreach (var model in targets.Cast<ExampleModel>())
                {
                    var baseTiles = model.TileCollection.GetTiles(true).Where(x => x.BaseTile == null).ToArray();

                    foreach (var tile in model.GetComponentsInChildren<ExampleTile>())
                    {
                        Undo.RecordObject(tile, "Update tile models");
                        if (tile.CurrentTile != null)
                        {
                            Undo.RecordObject(tile.CurrentTile, "Update tile models");
                        }
                        
                        tile.CloneTarget(baseTiles);
                        
                        Undo.RegisterCreatedObjectUndo(tile.CurrentTile, "Update tile models");
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

            var tileCollection = serializedObject.FindProperty(nameof(ExampleModel.TileCollection));
            EditorGUILayout.PropertyField(tileCollection, true);

            var gridSize = serializedObject.FindProperty(nameof(ExampleModel.GridSize));
            if (tileCollection.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(gridSize, true);
            }
            else
            {
                var grid = ((TileCollection) tileCollection.objectReferenceValue)?.DefaultGrid;
                StixGamesEditorExtensions.FixedSizeArray(gridSize, grid?.Axes ?? 0, grid?.AxisNames ?? new string[0]);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var model = target as ExampleModel;

            var t = model.transform;
            var collection = model.TileCollection;

            if (collection == null)
            {
                return;
            }
            
            var grid = collection.GetGrid(model.GridSize);

            // Create grid
            var lines = grid.GetModelFrame().Select(x => t.TransformPoint(x)).ToArray();
            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawDottedLines(lines, 10);
        }
    }
}