using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    public static class SerializedPropertyExtensions
    {
        public static GUIContent GetGUIContent(this SerializedProperty property)
        {
            return new GUIContent(property.displayName, property.tooltip);
        }
    }
}
