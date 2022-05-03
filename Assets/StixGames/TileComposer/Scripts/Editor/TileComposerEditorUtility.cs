using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    public class TileComposerEditorUtility
    {
        public static void TileTypeSelector(SerializedProperty tileCollection, SerializedProperty property,
            params GUILayoutOption[] options)
        {
            TileTypeSelector(tileCollection, property, true, false, options);
        }

        public static void TileTypeSelector(SerializedProperty tileCollection, SerializedProperty property,
            bool includeEmpty, bool hideLabel, params GUILayoutOption[] options)
        {
            if (tileCollection.hasMultipleDifferentValues)
            {
                if (hideLabel)
                {
                    EditorGUILayout.LabelField(property.stringValue, options);
                }
                else
                {
                    EditorGUILayout.LabelField(property.GetGUIContent(), property.stringValue, options);
                }
            }
            else
            {
                // Get the array of tile types
                var collection = (TileCollection) tileCollection.objectReferenceValue;
                TileTypeSelector(collection, property, includeEmpty, hideLabel, options);
            }
        }

        public static void TileTypeSelector(TileCollection tileCollection, SerializedProperty property,
            params GUILayoutOption[] options)
        {
            TileTypeSelector(tileCollection, property, true, false, options);
        }
        public static void TileTypeSelector(TileCollection tileCollection, SerializedProperty property, bool includeEmpty, bool hideLabel,
            params GUILayoutOption[] options)
        {
            var tileTypes = (includeEmpty ? tileCollection.GetAllTypes() : tileCollection.GetTileTypes()).OrderBy(x => x).ToList();
            var labels = tileTypes.Select(x => new GUIContent(x)).ToArray();
            var current = tileTypes.IndexOf(property.stringValue);
            if (current < 0)
            {
                current = 0;
            }

            // Create a dropdown menu to select the wanted tile type
            if (hideLabel)
            {
                current = EditorGUILayout.Popup(GUIContent.none, current, labels, options);
            }
            else
            {
                current = EditorGUILayout.Popup(property.GetGUIContent(), current, labels, options);
            }

            // Set the string value of the new type
            property.stringValue = tileTypes[current];
        }
    }
}