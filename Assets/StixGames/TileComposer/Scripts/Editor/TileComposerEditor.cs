using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    [CustomEditor(typeof(TileComposer))]
    [CanEditMultipleObjects]
    public class TileComposerEditor : Editor
    {
        private bool showWFCSettings = true;
        private bool showZ3Settings = true;

        private TileRestrictionType editedTileRestrictionType;
        private int editedTileRestriction = -1;

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(new GUIContent("Generate model in editor",
                    "Generates the model in the editor mode. This will always use fast generation.")))
            {
                foreach (var tileComposer in targets.Cast<TileComposer>())
                {
                    var slowGen = tileComposer.DoSlowGeneration;
                    tileComposer.DoSlowGeneration = false;

                    try
                    {
                        tileComposer.Generate(true);
                    }
                    finally
                    {
                        tileComposer.DoSlowGeneration = slowGen;
                    }
                }
            }

            var tileCollection = serializedObject.FindProperty(nameof(TileComposer.TileCollection));
            EditorGUILayout.PropertyField(tileCollection, true);

            var seed = serializedObject.FindProperty(nameof(TileComposer.Seed));
            EditorGUILayout.PropertyField(seed, true);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(TileComposer.MaxTries)), true);

            var solverType = serializedObject.FindProperty(nameof(TileComposer.SolverTypeSelection));
            EditorGUILayout.PropertyField(solverType, true);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(TileComposer.ParallelInstances)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TileComposer.Timeout)),
                true);

            // Behaviour 
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(TileComposer.GenerateAsynchronously)),
                true);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(TileComposer.GenerateOnStart)),
                true);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(TileComposer.DestroyAfterUse)),
                true);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(TileComposer.InstantiateModelWhenGenerated)),
                true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(TileComposer.OnModelGenerated)),
                true);

            // Grid settings
            var gridSize = serializedObject.FindProperty(nameof(TileComposer.GridSize));
            var blockedTiles = serializedObject.FindProperty(nameof(TileComposer.BlockedTiles));
            var fixedTiles = serializedObject.FindProperty(nameof(TileComposer.FixedTiles));
            if (tileCollection.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.PropertyField(gridSize, true);
                    EditorGUILayout.PropertyField(blockedTiles, true);
                    EditorGUILayout.PropertyField(fixedTiles, true);
                }
            }
            else
            {
                var grid = ((TileCollection) tileCollection.objectReferenceValue)?.DefaultGrid;
                StixGamesEditorExtensions.FixedSizeArray(gridSize, grid?.Axes ?? 0, grid?.AxisNames ?? new string[0]);

                DrawTileRestrictions(tileCollection, blockedTiles, grid, TileRestrictionType.Blocked);
                DrawTileRestrictions(tileCollection, fixedTiles, grid, TileRestrictionType.Fixed);
            }

            EditorGUILayout.Space();
            DrawWaveFunctionCollapseSettings(solverType);

            EditorGUILayout.Space();
            Z3SolverSettings(tileCollection, solverType);

            DebugProperties(tileCollection);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTileRestrictions(SerializedProperty tileCollection, SerializedProperty tileInitializers,
            IGrid grid, TileRestrictionType restrictionType)
        {
            StixGamesEditorExtensions.CustomArrayProperty(tileInitializers,
                (prop, index) => TileRestrictions(tileCollection, tileInitializers, prop, index, grid, restrictionType),
                (p, a) => { }, // Don't do anything here, Unity's copy behaviour can be quite useful
                false, true);

            GUIContent guiContent;
            switch (restrictionType)
            {
                case TileRestrictionType.Blocked:
                    guiContent = new GUIContent("Add blocked tiles");
                    break;
                case TileRestrictionType.Fixed:
                    guiContent = new GUIContent("Add fixed tiles");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(restrictionType), restrictionType, null);
            }

            var newElem = ArrayAddButton(guiContent, tileInitializers);

            // Auto select the new element for editing
            if (newElem > 0)
            {
                editedTileRestrictionType = restrictionType;
                editedTileRestriction = tileInitializers.arraySize - 1;
            }
        }

        private void TileRestrictions(SerializedProperty tileCollection, SerializedProperty arrayProperty,
            SerializedProperty property, int index,
            IGrid grid, TileRestrictionType restrictionType)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var foldoutRect = EditorGUILayout.GetControlRect(GUILayout.Width(20));
                    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, new GUIContent(""), true);

                    var isEditing = editedTileRestrictionType == restrictionType && index == editedTileRestriction;
                    if (GUILayout.Button(isEditing ? new GUIContent("Stop") : new GUIContent("Edit"),
                            GUILayout.Width(40)))
                    {
                        editedTileRestrictionType = restrictionType;
                        editedTileRestriction = isEditing ? -1 : index;
                        EditorApplication.QueuePlayerLoopUpdate();
                    }

                    EditorGUILayout.LabelField(property.displayName);

                    if (ArrayDeleteButton(arrayProperty, index))
                    {
                        // Remove the selection, if the current element was deleted
                        if (editedTileRestriction == index)
                        {
                            editedTileRestrictionType = restrictionType;
                            editedTileRestriction = -1;
                        }

                        return;
                    }
                }

                if (property.isExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.Space();

                        TileComposerEditorUtility.TileTypeSelector(tileCollection,
                            property.FindPropertyRelative(nameof(TileSlice.TileType)));

                        EditorGUILayout.Space();

                        var slices = property.FindPropertyRelative(nameof(TileSlice.Dimensions));
                        StixGamesEditorExtensions.FixedSizeArray(slices, grid.Axes, grid.AxisNames, true);

                        EditorGUILayout.Space();
                    }
                }
            }
        }

        private void Z3SolverSettings(SerializedProperty tileCollection, SerializedProperty solverType)
        {
            showZ3Settings = EditorGUILayout.Foldout(showZ3Settings,
                new GUIContent("Z3 Solver Settings"));
            EditorGUI.indentLevel++;

            if (showZ3Settings)
            {
                using (new EditorGUI.DisabledGroupScope(!solverType.hasMultipleDifferentValues
                                                        && ((TileComposer.SolverType) solverType
                                                            .enumValueIndex) !=
                                                        TileComposer.SolverType.Z3Solver))
                {
                    DrawSatSettings(serializedObject.FindProperty(nameof(TileComposer.SATSettings)),
                        tileCollection);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSatSettings(SerializedProperty property, SerializedProperty tileCollection)
        {
            EditorGUILayout.PropertyField(
                property.FindPropertyRelative(nameof(SATSettings.RandomizeConstraintOrder)),
                true);

            EditorGUILayout.PropertyField(
                property.FindPropertyRelative(nameof(SATSettings.WeightPriority)),
                true);

            var percentageConstraints =
                property.FindPropertyRelative(nameof(SATSettings.PercentageConstraints));
            DrawConstraints(tileCollection, percentageConstraints, ConstraintType.TileType);

            var absoluteConstraints =
                property.FindPropertyRelative(nameof(SATSettings.AbsoluteConstraints));
            DrawConstraints(tileCollection, absoluteConstraints, ConstraintType.TileType);


            using (new EditorGUI.DisabledScope(tileCollection.hasMultipleDifferentValues ||
                                               (tileCollection.objectReferenceValue as TileCollection)?.CustomValues
                                               ?.Length == 0))
            {
                var customPropertyConstraints =
                    property.FindPropertyRelative(nameof(SATSettings.CustomPropertyConstraints));
                DrawConstraints(tileCollection, customPropertyConstraints, ConstraintType.CustomProperty);
            }
        }

        private void DrawConstraints(SerializedProperty tileCollection,
            SerializedProperty percentageConstraints, ConstraintType constraintType)
        {
            StixGamesEditorExtensions.CustomArrayProperty(percentageConstraints,
                (prop, index) =>
                    DrawCountConstraint(tileCollection, percentageConstraints, prop, index, constraintType),
                (p, i) => { }, // Once again, copying the previous entry is fine in this case.
                false, true);

            ArrayAddButton(new GUIContent("Add Constraint"), percentageConstraints);
        }

        private void DrawCountConstraint(SerializedProperty tileCollection, SerializedProperty arrayProperty,
            SerializedProperty property, int index, ConstraintType constraintType)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Select the constraints target type
                    switch (constraintType)
                    {
                        case ConstraintType.TileType:
                            TileComposerEditorUtility.TileTypeSelector(tileCollection,
                                property.FindPropertyRelative(nameof(PercentageConstraint.Name)), true, true,
                                GUILayout.Width(100));
                            break;
                        case ConstraintType.CustomProperty:
                            CustomPropertySelector(tileCollection,
                                property.FindPropertyRelative(nameof(AbsoluteConstraint.Name)), true,
                                GUILayout.Width(100));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(constraintType), constraintType, null);
                    }

                    // Select greater than / less than
                    var comparison = property.FindPropertyRelative(nameof(PercentageConstraint.Comparison));

                    GUIContent content;
                    if (comparison.hasMultipleDifferentValues)
                    {
                        content = new GUIContent("-");
                    }
                    else
                    {
                        switch ((Comparison) comparison.enumValueIndex)
                        {
                            case Comparison.Less:
                                content = new GUIContent("<");
                                break;
                            case Comparison.Equal:
                                content = new GUIContent("=");
                                break;
                            case Comparison.Greater:
                                content = new GUIContent(">");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (GUILayout.Button(content, GUILayout.Width(20)))
                    {
                        if (comparison.hasMultipleDifferentValues)
                        {
                            comparison.enumValueIndex = 0;
                        }
                        else
                        {
                            comparison.enumValueIndex = (comparison.enumValueIndex + 1) % 3;
                        }
                    }

                    // Select the percent
                    EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(PercentageConstraint.Value)),
                        GUIContent.none);

                    // Delete button
                    if (ArrayDeleteButton(arrayProperty, index))
                    {
                        return;
                    }
                }
            }
        }

        private void DrawWaveFunctionCollapseSettings(SerializedProperty solverType)
        {
            showWFCSettings = EditorGUILayout.Foldout(showWFCSettings,
                new GUIContent("Wave Function Collapse Settings"));
            EditorGUI.indentLevel++;

            if (showWFCSettings)
            {
                using (new EditorGUI.DisabledGroupScope(!solverType.hasMultipleDifferentValues
                                                        && ((TileComposer.SolverType) solverType
                                                            .enumValueIndex) !=
                                                        TileComposer.SolverType.WaveFunctionCollapse))
                {
                    DrawWFCSettings(serializedObject.FindProperty(nameof(TileComposer.WFCSettings)));
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawWFCSettings(SerializedProperty property)
        {
            var failureRecovery =
                property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.UseFailureRecovery));
            EditorGUILayout.PropertyField(failureRecovery);

            using (new EditorGUI.DisabledScope(!failureRecovery.boolValue))
            {
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.FailureResetRadius)), true);
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.RadiusSizeMultiplier)), true);
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.BacktrackSteps)),
                    true);
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.BacktrackStepsMultiplier)),
                    true);
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.FailureDecay)),
                    true);
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.FailureAccelerationMultiplier)),
                    true);
                EditorGUILayout.PropertyField(
                    property.FindPropertyRelative(nameof(WaveFunctionCollapseSettings.FailureAccelerationDecay)),
                    true);
            }
        }

        private void DebugProperties(SerializedProperty tileCollection)
        {
            var doSlowGeneration = serializedObject.FindProperty(nameof(TileComposer.DoSlowGeneration));
            EditorGUILayout.PropertyField(doSlowGeneration, true);
            using (new EditorGUI.DisabledScope(!doSlowGeneration.boolValue))
            {
                EditorGUILayout.Space();

                var isPaused = serializedObject.FindProperty(nameof(TileComposer.IsPaused));

                GUILayout.Label("Slow Generation Controls", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (isPaused.boolValue)
                    {
                        if (GUILayout.Button(new GUIContent("▶", "Resume slow generation")))
                        {
                            isPaused.boolValue = false;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(new GUIContent("||", "Pause slow generation")))
                        {
                            isPaused.boolValue = true;
                        }
                    }

                    if (GUILayout.Button(new GUIContent(">", "Make a single step")))
                    {
                        var doStep = serializedObject.FindProperty(nameof(TileComposer.DoStep));
                        doStep.boolValue = true;
                    }
                }

                EditorGUILayout.Space();

                var timeStep = serializedObject.FindProperty(nameof(TileComposer.TimeStep));
                EditorGUILayout.PropertyField(timeStep, true);

                var solverStepsPerStep =
                    serializedObject.FindProperty(nameof(TileComposer.SolverStepsPerStep));
                EditorGUILayout.PropertyField(solverStepsPerStep, true);

                var useFastDebugMode = serializedObject.FindProperty(nameof(TileComposer.UseFastDebugMode));
                EditorGUILayout.PropertyField(useFastDebugMode, true);

                var emptyDebugObjects =
                    serializedObject.FindProperty(nameof(TileComposer.EmptyDebugObjects));
                if (tileCollection.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.PropertyField(emptyDebugObjects, true);
                    }
                }
                else
                {
                    var collection = (TileCollection) tileCollection.objectReferenceValue;
                    StixGamesEditorExtensions.FixedSizeArray(emptyDebugObjects, collection?.EmptyTiles.Length ?? 0,
                        collection?.EmptyTiles.Select(x => x.Name).ToArray() ?? new string[0]);
                }

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(nameof(TileComposer.SlowGenerationMaterial)), true);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(nameof(TileComposer.Transparency)), true);
            }
        }

        private void OnSceneGUI()
        {
            var model = (TileComposer) target;
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

            DrawTileRestriction(model, grid, t);
        }

        private void DrawTileRestriction(TileComposer model, IGrid grid, Transform t)
        {
            TileSlice[] restrictionList;
            switch (editedTileRestrictionType)
            {
                case TileRestrictionType.Blocked:
                    restrictionList = model.BlockedTiles;
                    break;
                case TileRestrictionType.Fixed:
                    restrictionList = model.FixedTiles;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Abort if no initializer is edited
            if (editedTileRestriction < 0 || editedTileRestriction >= restrictionList.Length)
            {
                return;
            }

            // Draw initializer outline
            var initializer = restrictionList[editedTileRestriction];
            var initializerArea = grid.GetSliceFrame(initializer.Dimensions)
                .Select(x => model.transform.TransformPoint(x)).ToArray();

            Handles.color = Color.green;
            Handles.DrawLines(initializerArea);

            var center = t.TransformPoint(grid.GetSliceCenter(initializer.Dimensions));

            // Create handle for initializer position
            var newPos = Handles.PositionHandle(center, t.rotation);
            var localDir = t.InverseTransformVector(newPos - center);
            var indexOffset = grid.CalculateIndexOffset(center, localDir);

            // Update the values
            if (indexOffset.Any(x => x != 0))
            {
                Undo.RecordObject(model, "Move grid initializer");

                for (int i = 0; i < grid.Axes; i++)
                {
                    var slice = initializer.Dimensions[i];

                    // Normalize grid before applying clamps
                    if (slice.Start < 0 || slice.End < 0)
                    {
                        slice.Start = MathExtensions.Modulo(slice.Start, model.GridSize[i]);
                        slice.End = MathExtensions.Modulo(slice.End, model.GridSize[i]);
                    }

                    // Add offset
                    var start = Mathf.Clamp(slice.Start + indexOffset[i], 0, model.GridSize[i] - 1);
                    var end = Mathf.Clamp(slice.End + indexOffset[i], 0, model.GridSize[i] - 1);

                    slice.Start = Mathf.Min(start, end);
                    slice.End = Mathf.Max(start, end);
                }
            }

            // Create handles for initializer size
            Handles.color = Color.white;
            for (int direction = 0; direction < grid.Axes * 2; direction++)
            {
                var axis = direction / 2;
                var isPositive = direction % 2 == 0;

                var sideCenter =
                    t.TransformPoint(grid.GetSliceBorderCenter(initializer.Dimensions, axis, isPositive, true));
                var sideNormal =
                    t.TransformDirection(grid.GetSliceBorderNormal(initializer.Dimensions, axis, isPositive, true));

                var scaleOffset = Handles.Slider(sideCenter, sideNormal,
                    HandleUtility.GetHandleSize(sideCenter) * 0.1f, Handles.CubeHandleCap, 0.1f);

                // Offset: (axis, start/end, index-offset)
                var offset = grid.CalculateSliceOffset(axis, isPositive, sideCenter,
                    t.InverseTransformVector(scaleOffset - sideCenter));

                // Update the slice value
                if (offset != 0)
                {
                    Undo.RecordObject(model, "Resize grid initializer");

                    var slice = initializer.Dimensions[axis];
                    if (isPositive)
                    {
                        slice.End = Mathf.Clamp(slice.End + offset, 0, model.GridSize[axis] - 1);
                    }
                    else
                    {
                        slice.Start = Mathf.Clamp(slice.Start + offset, 0, model.GridSize[axis] - 1);
                    }
                }
            }
        }

        private static void CustomPropertySelector(SerializedProperty tileCollection, SerializedProperty property,
            bool hideLabel, params GUILayoutOption[] options)
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
                var properties = collection != null
                    ? collection.CustomValues.OrderBy(x => x).ToArray()
                    : new string[0];
                var labels = properties.Select(x => new GUIContent(x)).ToArray();
                var current = Array.IndexOf(properties, property.stringValue);
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
                if (collection.CustomValues.Length > 0)
                {
                    // Set the value, if there is a valid value to set
                    property.stringValue = properties[current];
                }
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
                    EditorApplication.QueuePlayerLoopUpdate();

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
                EditorApplication.QueuePlayerLoopUpdate();

                return true;
            }

            return false;
        }

        private enum TileRestrictionType
        {
            Blocked,
            Fixed
        }

        private enum ConstraintType
        {
            TileType,
            CustomProperty
        }
    }
}