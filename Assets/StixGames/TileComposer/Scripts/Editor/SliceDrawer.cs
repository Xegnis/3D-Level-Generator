using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    [CustomPropertyDrawer(typeof(Slice))]
    public class SliceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PrefixLabel(position, label);

            position.xMin += EditorGUIUtility.labelWidth;

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var halfSize = (int) (position.width * 0.5f);
            var splitterHalfSize = 10;
            var splitterRealHalfSize = 4;
            var startRect = new Rect(position.x, position.y, halfSize - splitterHalfSize, position.height);
            var splitterRect = new Rect(position.x + halfSize - splitterRealHalfSize, position.y, splitterRealHalfSize * 2, position.height);
            var endRect = new Rect(position.x + halfSize + splitterHalfSize, position.y, halfSize - splitterHalfSize, position.height);

            EditorGUI.PropertyField(startRect, property.FindPropertyRelative(nameof(Slice.Start)), GUIContent.none);
            EditorGUI.LabelField(splitterRect, new GUIContent(":"));
            EditorGUI.PropertyField(endRect, property.FindPropertyRelative(nameof(Slice.End)), GUIContent.none);

            EditorGUI.indentLevel = indent;
        }
    }
}
