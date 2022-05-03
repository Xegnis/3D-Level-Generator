using System.Linq;
using StixGames.TileComposer.Solvers.WFCPlugins;
using UnityEditor;

namespace StixGames.TileComposer.WFCPlugins
{
    [CustomEditor(typeof(Mirror))]
    public class MirrorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var mirror = target as Mirror;
            var tileComposer = mirror.GetComponent<TileComposer>();
            var tileCollection = tileComposer != null ? tileComposer.TileCollection : null;
            
            if (tileCollection == null)
            {
                EditorGUILayout.LabelField("Component must be on the same object as a Tile Composer with a selected Tile Collection.");
                return;
            }

            var grid = tileCollection.DefaultGrid;

            var axis = serializedObject.FindProperty(nameof(Mirror.Axis));
            axis.intValue = EditorGUILayout.Popup(axis.displayName, axis.intValue, grid.AxisNames);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Mirror.MirrorTile)));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}