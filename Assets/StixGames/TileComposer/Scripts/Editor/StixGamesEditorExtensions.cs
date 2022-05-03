using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    public static class StixGamesEditorExtensions
    {
        public static void FixedSizeArray(SerializedProperty property, int elements, string[] elementNames,
            bool inlineArray = false)
        {
            FixedSizeArray(property, elements, elementNames, inlineArray,
                (prop, label) => EditorGUILayout.PropertyField(prop, label, true));
        }

        public static void FixedSizeArray(SerializedProperty property, int elements, string[] elementNames,
            bool inlineArray, Action<SerializedProperty, GUIContent> propertyDrawer)
        {
            ResizeArrayProperty(property, elements);

            if (!inlineArray)
            {
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName);
            }

            if (property.isExpanded || inlineArray)
            {
                using (new EditorGUI.IndentLevelScope(inlineArray ? 0 : 1))
                {
                    var elemGUIContent = elementNames.Select(x => new GUIContent(x)).ToList();
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var elem = property.GetArrayElementAtIndex(i);

                        propertyDrawer(elem, elemGUIContent[i]);
                    }
                }
            }
        }

        public static void CustomArrayProperty(SerializedProperty property,
            Action<SerializedProperty, int> propertyDrawer, Action<SerializedProperty, int> newElementAction,
            bool inlineArray = false, bool hideArraySize = false, bool drawAddDeleteButtons = false, bool alwaysExpanded = false)
        {
            if (!inlineArray)
            {
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName);
            }

            if (!alwaysExpanded && !property.isExpanded)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope(inlineArray ? 0 : 1))
            {
                if (!hideArraySize)
                {
                    var arrayLength = property.FindPropertyRelative("Array.size");
                    EditorGUILayout.PropertyField(arrayLength);
                }

                for (int i = 0; i < property.arraySize; i++)
                {
                    if (drawAddDeleteButtons)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.BeginVertical();
                    }

                    var elem = property.GetArrayElementAtIndex(i);
                    propertyDrawer(elem, i);

                    if (drawAddDeleteButtons)
                    {
                        EditorGUILayout.EndVertical();

                        if (ArrayDeleteButton(property, i))
                        {
                            // Don't continue drawing on delete
                            return;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (drawAddDeleteButtons)
                {
                    var newElem = ArrayAddButton(new GUIContent("Add element"), property);

                    if (newElem >= 0)
                    {
                        newElementAction(property.GetArrayElementAtIndex(newElem), newElem);
                    }
                }
            }
        }

        public static void ResizeArrayProperty(SerializedProperty property, int size)
        {
            if (property.FindPropertyRelative("Array.size").hasMultipleDifferentValues)
            {
                property.arraySize = size;
            }

            while (property.arraySize > size)
            {
                property.DeleteArrayElementAtIndex(property.arraySize - 1);
            }

            while (property.arraySize < size)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
            }
        }

        /// <summary>
        /// Creates a button to add a new element to the array property. Returns -1 if no new element was created, or the index of the new element.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        private static int ArrayAddButton(GUIContent content, SerializedProperty property)
        {
            var returnValue = -1;

            if (property.isExpanded)
            {
                if (GUILayout.Button(content))
                {
                    property.InsertArrayElementAtIndex(property.arraySize);

                    returnValue = property.arraySize - 1;
                }

                EditorGUILayout.Space();
            }

            return returnValue;
        }

        /// <summary>
        /// Deletes the selected element from the array property.
        /// </summary>
        /// <param name="arrayProperty"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static bool ArrayDeleteButton(SerializedProperty arrayProperty, int index)
        {
            if (GUILayout.Button(new GUIContent("X"), GUILayout.Width(20)))
            {
                arrayProperty.DeleteArrayElementAtIndex(index);

                return true;
            }

            return false;
        }
    }
}