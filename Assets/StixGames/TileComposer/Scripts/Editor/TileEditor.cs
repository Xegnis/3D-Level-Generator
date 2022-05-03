using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StixGames.TileComposer
{
    [CustomEditor(typeof(Tile))]
    [CanEditMultipleObjects]
    public class TileEditor : Editor
    {
        private bool canEditNeighbors;
        private bool isEditingSingleBaseTile;
        private bool onlyRootTiles;

        private bool isDrawingNewLine;
        private int connectionStartSide;
        private string connectionEndTileType;
        private int connectionEndSide;
        private bool createNewNeighborConnection;
        private bool showTileNeighborsEditor = true;
        private bool showConnectorsEditor = true;
        private bool showCustomPropertiesEditor = true;

        private readonly List<Action> endActions = new List<Action>();

        private static bool isInitialized;
        private static bool useDarkGUI;
        private static bool showConnectionNames;
        private static bool useConnectionLines;
        private static bool showConnectionLines;
        private static int sideMask;

        private static float circleSize;
        private static float circleOffset;

        private bool requestSanitize;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var tileTargets = targets.Cast<Tile>();
            var tileCollections = tileTargets.SelectMany(x => x.GetComponentsInParent<TileCollection>(true))
                .Distinct().ToList();

            if (tileCollections.Count > 1)
            {
                EditorGUILayout.LabelField("Can't edit tiles from multiple collections at the same time.");
                return;
            }

            if (tileCollections.Count == 0)
            {
                EditorGUILayout.LabelField("Your tile has to be the child object of a TileCollection.");
                return;
            }

            var collection = tileCollections.Single();
            var grid = collection.DefaultGrid;

            bool wasChanged;
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                EditorSettings(grid);

                GeneralSettings(collection);
                RotationalAxes(grid);
                Connectors(collection, grid);
                CustomProperties();
                Neighbors(collection, grid);

                serializedObject.ApplyModifiedProperties();

                wasChanged = changeCheck.changed;
            }

            // Apply all actions that were registered during the editor run
            if (Event.current.type == EventType.Repaint)
            {
                // Only update during the repaint stage, so layouting works as expected
                // Record undo for all sub objects
                Undo.RecordObjects(tileCollections.Cast<Object>().ToArray(),
                    "Record potential tile collection changes");
                Undo.RecordObjects(tileCollections.SelectMany(x => x.GetTiles(true)).Cast<Object>().ToArray(),
                    "Record potential tile changes");

                // Execute actions
                foreach (var endAction in endActions)
                {
                    endAction();
                }

                endActions.Clear();

                if (wasChanged)
                {
                    foreach (var tileTarget in tileTargets)
                    {
                        DataSanitizer.SanitizeTile(collection, tileTarget);
                    }
                }
            }
        }

        private void EditorSettings(IGrid grid)
        {
            if (!isInitialized)
            {
                useDarkGUI = EditorPrefs.GetBool("StixGames.TileComposer.UseDarkGui", false);
                showConnectionNames = EditorPrefs.GetBool("StixGames.TileComposer.ShowConnectionNames", true);
                useConnectionLines = EditorPrefs.GetBool("StixGames.TileComposer.UseConnectionLines", true);
                showConnectionLines = EditorPrefs.GetBool("StixGames.TileComposer.ShowConnectionLines", false);
                sideMask = EditorPrefs.GetInt("StixGames.TileComposer.SideMask", -1);
                circleSize = EditorPrefs.GetFloat("StixGames.TileComposer.CircleSize", 0.2f);
                circleOffset = EditorPrefs.GetFloat("StixGames.TileComposer.CircleOffset", 0);
                isInitialized = true;
            }

            EditorGUILayout.LabelField(new GUIContent("Editor Settings"), EditorStyles.boldLabel);
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                useDarkGUI = EditorGUILayout.Toggle(new GUIContent("Use dark GUI",
                        "If enabled, the GUI is drawn in black instead of white, which can increase visibility."),
                    useDarkGUI);
                showConnectionNames = EditorGUILayout.Toggle(new GUIContent("Show Connection Names",
                    "Shows the names of allowed neighbor tiles on the side of selected tiles."), showConnectionNames);
                useConnectionLines = EditorGUILayout.Toggle(new GUIContent("Use Connection Lines",
                    "Use drag and drop lines to connect tiles."), useConnectionLines);
                showConnectionLines = EditorGUILayout.Toggle(new GUIContent("Show Connection Lines",
                    "Shows all lines to connected neighbors from the currently selected tiles."), showConnectionLines);

                sideMask = EditorGUILayout.MaskField(
                    new GUIContent("Side Mask",
                        "Mask out connection lines per tile side. " +
                        "Use this in case there are too many lines to distinguish them individually."),
                    sideMask, grid.SideNames);

                circleSize = Mathf.Max(0, EditorGUILayout.FloatField(
                    new GUIContent("Circle Size", "Change the size of the toggle control circles."), circleSize));

                circleOffset = EditorGUILayout.FloatField(
                    new GUIContent("Circle Offset", "Change the offset of the toggle control circles."), circleOffset);

                if (change.changed)
                {
                    EditorPrefs.SetBool("StixGames.TileComposer.UseDarkGui", useDarkGUI);
                    EditorPrefs.SetBool("StixGames.TileComposer.ShowConnectionNames", showConnectionNames);
                    EditorPrefs.SetBool("StixGames.TileComposer.UseConnectionLines", useConnectionLines);
                    EditorPrefs.SetBool("StixGames.TileComposer.ShowConnectionLines", showConnectionLines);
                    EditorPrefs.SetInt("StixGames.TileComposer.SideMask", sideMask);
                    EditorPrefs.SetFloat("StixGames.TileComposer.CircleSize", circleSize);
                    EditorPrefs.SetFloat("StixGames.TileComposer.CircleOffset", circleOffset);

                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }

        private void GeneralSettings(TileCollection collection)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(new GUIContent("General Settings"), EditorStyles.boldLabel);

            var baseTile = serializedObject.FindProperty(nameof(Tile.BaseTile));
            EditorGUILayout.PropertyField(baseTile);

            // Save some values that will be used by other settings
            onlyRootTiles = !baseTile.hasMultipleDifferentValues && baseTile.objectReferenceValue == null;
            canEditNeighbors = onlyRootTiles ||
                               !onlyRootTiles && !baseTile.hasMultipleDifferentValues;

            var tileType = serializedObject.FindProperty(nameof(Tile.TileType));
            TileTypeTextField(tileType, collection);

            var weight = serializedObject.FindProperty(nameof(Tile.BaseWeight));
            EditorGUILayout.PropertyField(weight);

            var canNeighborSelf = serializedObject.FindProperty(nameof(Tile.CanNeighborSelf));
            EditorGUILayout.PropertyField(canNeighborSelf);
        }

        private void TileTypeTextField(SerializedProperty tileType, TileCollection collection)
        {
            var disabled = !onlyRootTiles || serializedObject.isEditingMultipleObjects;
            using (new EditorGUI.DisabledGroupScope(disabled))
            {
                if (serializedObject.isEditingMultipleObjects || !onlyRootTiles)
                {
                    // Multiple values or single non-root tile
                    EditorGUILayout.PropertyField(tileType);
                }
                else
                {
                    // Single root tile
                    var original = tileType.stringValue;
                    var newValue = EditorGUILayout.DelayedTextField(tileType.GetGUIContent(), tileType.stringValue);

                    if (original != newValue)
                    {
                        newValue = DataSanitizer.UniqueTileType(collection, newValue);

                        // Apply the change
                        tileType.stringValue = newValue;
                        endActions.Add(() => collection.UpdateTileTypes(original, newValue));
                    }
                }
            }
        }

        private void RotationalAxes(IGrid grid)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(new GUIContent("Variations"), EditorStyles.boldLabel);
            var rotationAxis = serializedObject.FindProperty(nameof(Tile.RotationAxes));

            var axisTypes = Enumerable.Range(0, grid.RotationAxes.Length).ToArray();
            var axisNames = grid.RotationAxes.Select(x => new GUIContent(x)).ToArray();
            StixGamesEditorExtensions.CustomArrayProperty(rotationAxis,
                (prop, index) => EditorGUILayout.IntPopup(prop, axisNames, axisTypes),
                (p, i) => p.enumValueIndex = 0);
        }

        private void Connectors(TileCollection collection, IGrid grid)
        {
            EditorGUILayout.Space();

            var connectors = serializedObject.FindProperty(nameof(Tile.Connectors));
            showConnectorsEditor =
                EditorGUILayout.Foldout(showConnectorsEditor, connectors.GetGUIContent());
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledGroupScope(!canEditNeighbors))
                {
                    if (showConnectorsEditor)
                    {
                        // Disable when no connectors are set up
                        using (new EditorGUI.DisabledScope(collection.Connectors.Length == 0))
                        {
                            StixGamesEditorExtensions.FixedSizeArray(connectors, grid.Sides, grid.SideNames, true,
                                (prop, content) => DrawConnectors(collection, prop, content));
                        }
                    }
                }
            }
        }

        private void CustomProperties()
        {
            EditorGUILayout.Space();

            var properties = serializedObject.FindProperty(nameof(Tile.CustomProperties));
            showCustomPropertiesEditor =
                EditorGUILayout.Foldout(showCustomPropertiesEditor, properties.GetGUIContent());
            using (new EditorGUI.IndentLevelScope())
            {
                if (showCustomPropertiesEditor)
                {
                    for (int i = 0; i < properties.arraySize; i++)
                    {
                        // TODO: Use a property drawer for this instead
                        var property = properties.GetArrayElementAtIndex(i);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(property.FindPropertyRelative(nameof(CustomTileProperty.Name))
                            .stringValue);
                        EditorGUILayout.PropertyField(
                            property.FindPropertyRelative(nameof(CustomTileProperty.IntValue)),
                            GUIContent.none);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        private void DrawConnectors(TileCollection collection, SerializedProperty property, GUIContent content)
        {
            var sideConnectors = property.FindPropertyRelative(nameof(ConnectorSide.Connectors));

            sideConnectors.isExpanded = EditorGUILayout.Foldout(sideConnectors.isExpanded, content, true);

            using (new EditorGUI.IndentLevelScope())
            {
                var firstConnector = collection.Connectors.Length == 0 ? "" : collection.Connectors[0].Name;
                StixGamesEditorExtensions.CustomArrayProperty(sideConnectors,
                    (prop, index) => ConnectorSelector(collection, prop, index),
                    (p, i) =>
                    {
                        var nameProperty = p.FindPropertyRelative(nameof(ConnectorAssignment.Name));
                        nameProperty.stringValue = firstConnector;

                        var connectionTypeProperty = p.FindPropertyRelative(nameof(ConnectorAssignment.ConnectionType));
                        connectionTypeProperty.enumValueIndex = ConnectionType.Bidirectional.GetIndex();
                    },
                    true, false, true);
            }
        }

        private void ConnectorSelector(TileCollection collection, SerializedProperty property, int index)
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.LabelField("Multi object editing not yet supported.");
                return;
            }

            // TODO: Support multi object editing 
            using (new EditorGUILayout.HorizontalScope())
            {
                // Type selector
                var connectorTypes = collection.Connectors.Select(x => x.Name).ToList();
                var nameDisplayedOptions = connectorTypes.Select(x => new GUIContent(x)).ToArray();

                var nameProperty = property.FindPropertyRelative(nameof(ConnectorAssignment.Name));

                int nameIndex = connectorTypes.IndexOf(nameProperty.stringValue);
                if (nameIndex < 0)
                {
                    nameIndex = 0;
                }

                EditorGUI.showMixedValue = nameProperty.hasMultipleDifferentValues;
                var prevIndent = EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;
                nameIndex = EditorGUILayout.Popup(nameIndex, nameDisplayedOptions);
                EditorGUI.indentLevel = prevIndent;

                // If there are no connectors set up, this may be empty
                var isBidirectional = false;
                if (0 <= nameIndex && nameIndex < collection.Connectors.Length)
                {
                    isBidirectional = collection.Connectors[nameIndex].IsBidirectional;
                    nameProperty.stringValue = connectorTypes[nameIndex];
                }

                var connectionTypeProperty = property.FindPropertyRelative(nameof(ConnectorAssignment.ConnectionType));
                if (isBidirectional)
                {
                    connectionTypeProperty.enumValueIndex = ConnectionType.Bidirectional.GetIndex();
                    return;
                }

                // Select in or out for unidirectional connections
                var connectionTypes = new[] {"In", "Out", "Both"};
                var connectionTypeDisplayedOptions = connectionTypes.Select(x => new GUIContent(x)).ToArray();

                EditorGUI.indentLevel = 0;
                connectionTypeProperty.enumValueIndex = EditorGUILayout.Popup(GUIContent.none,
                    connectionTypeProperty.enumValueIndex, connectionTypeDisplayedOptions, GUILayout.Width(40));
                EditorGUI.indentLevel = prevIndent;
            }
        }

        private void Neighbors(TileCollection collection, IGrid grid)
        {
            EditorGUILayout.Space();

            var neighbors = serializedObject.FindProperty(nameof(Tile.Neighbors));
            showTileNeighborsEditor =
                EditorGUILayout.Foldout(showTileNeighborsEditor, neighbors.GetGUIContent());
            
            using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledGroupScope(!canEditNeighbors))
                {
                    var emptyTypes = new HashSet<string>(collection.EmptyTiles.Select(x => x.Name));

                    var sideNames = grid.SideNames.Select(x => new GUIContent(x)).ToArray();
                    var sizeDiscrepancy = neighbors.arraySize != collection.GetAllTypes(true).Length;

                    if (showTileNeighborsEditor)
                    {
                        // Empty types are handled differently, because their orientation isn't relevant
                        sizeDiscrepancy = EmptyTypeNeighbors(collection, grid, neighbors, emptyTypes, sizeDiscrepancy,
                            sideNames);

                        // Draw editors for all non-empty tile types
                        sizeDiscrepancy =
                            TileNeighbors(collection, grid, neighbors, emptyTypes, sizeDiscrepancy, sideNames);
                    }

                    if (sizeDiscrepancy)
                    {
                        endActions.Add(collection.UpdateTileTypeCount);
                    }
                }
        }

        private bool EmptyTypeNeighbors(TileCollection collection, IGrid grid, SerializedProperty neighbors,
            HashSet<string> emptyTypes,
            bool sizeDiscrepancy, GUIContent[] sideNames)
        {
            EditorGUILayout.LabelField(new GUIContent("Empty Neighbors"), EditorStyles.boldLabel);
            for (int neighborIndex = 0; neighborIndex < neighbors.arraySize; neighborIndex++)
            {
                var neighbor = neighbors.GetArrayElementAtIndex(neighborIndex);
                var tileTypeName = neighbor.FindPropertyRelative(nameof(NeighborCompatibiltyMatrix.TileType))
                    .stringValue;
                var typeName = new GUIContent(tileTypeName);

                // Skip non-empty types
                if (!emptyTypes.Contains(tileTypeName))
                {
                    continue;
                }

                EditorGUILayout.PropertyField(neighbor, typeName, false);

                if (!neighbor.isExpanded)
                {
                    continue;
                }

                var neighborSides = neighbor.FindPropertyRelative(nameof(NeighborCompatibiltyMatrix.Sides));
                var neighborSideOverrides =
                    neighbor.FindPropertyRelative(nameof(NeighborCompatibiltyMatrix.Overrides));

                sizeDiscrepancy |= neighborSides.arraySize != grid.Sides ||
                                   neighborSideOverrides.arraySize != grid.Sides;

                if (onlyRootTiles)
                {
                    //using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int sideIndex = 0;
                             sideIndex < neighborSides.arraySize && sideIndex < grid.Sides;
                             sideIndex++)
                        {
                            var side = neighborSides.GetArrayElementAtIndex(sideIndex);
                            EditorGUI.showMixedValue = !canEditNeighbors && !onlyRootTiles;
                            // Interface for root tiles
                            using (var changeCheck = new EditorGUI.ChangeCheckScope())
                            {
                                var isSet = side.intValue == ~0;
                                isSet = EditorGUILayout.ToggleLeft(sideNames[sideIndex], isSet, GUILayout.MinWidth(40));

                                if (changeCheck.changed && canEditNeighbors && onlyRootTiles)
                                {
                                    side.intValue = isSet ? ~0 : 0;

                                    // Update neighbor tiles, to fit the current change
                                    foreach (var tile in targets.Cast<Tile>())
                                    {
                                        endActions.Add(() => collection.UpdateNeighborCompatibility(tile, grid));
                                    }
                                }
                                else if (side.intValue != 0 && side.intValue != ~0)
                                {
                                    // If the user didn't change anything, check if the variable has a valid value
                                    // if the value isn't valid, override it with "IsSet"
                                    side.intValue = ~0;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int sideIndex = 0; sideIndex < neighborSides.arraySize; sideIndex++)
                    {
                        var side = neighborSides.GetArrayElementAtIndex(sideIndex);
                        var sideOverride = neighborSideOverrides.GetArrayElementAtIndex(sideIndex);
                        EditorGUI.showMixedValue = !canEditNeighbors && !onlyRootTiles;
                        // Interface for sub tiles, with override toggle
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(sideOverride, GUIContent.none, GUILayout.Width(20));

                            // TODO: Copy non-overriden values from base tile, maybe in the tile collection?

                            var canEditNeighborValue =
                                !sideOverride.hasMultipleDifferentValues && sideOverride.boolValue;
                            using (new EditorGUI.DisabledGroupScope(!canEditNeighborValue))
                            {
                                var isSet = side.intValue == ~0;
                                isSet = EditorGUILayout.ToggleLeft(sideNames[sideIndex], isSet, GUILayout.MinWidth(40));

                                if (canEditNeighborValue)
                                {
                                    side.intValue = isSet ? ~0 : 0;
                                }
                                else if (side.intValue != 0 && side.intValue != ~0)
                                {
                                    // If the user didn't change anything, check if the variable has a valid value
                                    // if the value isn't valid, override it with "IsSet"
                                    side.intValue = ~0;
                                }
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            return sizeDiscrepancy;
        }

        private bool TileNeighbors(TileCollection collection, IGrid grid, SerializedProperty neighbors, HashSet<string>
                emptyTypes,
            bool sizeDiscrepancy, GUIContent[] sideNames)
        {
            EditorGUILayout.LabelField(new GUIContent("Tile Neighbors"), EditorStyles.boldLabel);
            for (int neighborIndex = 0;
                 neighborIndex < neighbors.arraySize;
                 neighborIndex++)
            {
                var neighbor = neighbors.GetArrayElementAtIndex(neighborIndex);
                var tileTypeName = neighbor.FindPropertyRelative(nameof(NeighborCompatibiltyMatrix.TileType))
                    .stringValue;
                var typeName = new GUIContent(tileTypeName);

                // Skip empty types
                if (emptyTypes.Contains(tileTypeName))
                {
                    continue;
                }

                EditorGUILayout.PropertyField(neighbor, typeName, false);

                if (!neighbor.isExpanded)
                {
                    continue;
                }

                var neighborSides = neighbor.FindPropertyRelative(nameof(NeighborCompatibiltyMatrix.Sides));
                var neighborSideOverrides =
                    neighbor.FindPropertyRelative(nameof(NeighborCompatibiltyMatrix.Overrides));

                sizeDiscrepancy |= neighborSides.arraySize != grid.Sides ||
                                   neighborSideOverrides.arraySize != grid.Sides;

                for (int sideIndex = 0; sideIndex < neighborSides.arraySize && sideIndex < grid.Sides; sideIndex++)
                {
                    var side = neighborSides.GetArrayElementAtIndex(sideIndex);
                    var sideOverride = neighborSideOverrides.GetArrayElementAtIndex(sideIndex);
                    EditorGUI.showMixedValue = !canEditNeighbors && !onlyRootTiles;

                    if (onlyRootTiles)
                    {
                        // Interface for root tiles
                        using (var changeCheck = new EditorGUI.ChangeCheckScope())
                        {
                            PropertyMaskField(sideNames[sideIndex], side, grid.SideNames);

                            if (changeCheck.changed && canEditNeighbors && onlyRootTiles)
                            {
                                // Update neighbor tiles, to fit the current change
                                foreach (var tile in targets.Cast<Tile>())
                                {
                                    endActions.Add(() => collection.UpdateNeighborCompatibility(tile, grid));
                                }
                            }
                        }
                    }
                    else
                    {
                        // Interface for sub tiles, with override toggle
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(sideOverride, GUIContent.none, GUILayout.Width(20));

                            // TODO: Copy non-overriden values from base tile, maybe in the tile collection?

                            var canEditNeighborValue =
                                !sideOverride.hasMultipleDifferentValues && sideOverride.boolValue;
                            using (new EditorGUI.DisabledGroupScope(!canEditNeighborValue))
                            {
                                PropertyMaskField(sideNames[sideIndex], side, grid.SideNames);
                            }
                        }
                    }
                }

                EditorGUILayout.Space();
            }

            return sizeDiscrepancy;
        }

        private void PropertyMaskField(GUIContent label, SerializedProperty property, string[] entryLabels)
        {
            EditorGUI.showMixedValue = EditorGUI.showMixedValue || property.hasMultipleDifferentValues;
            int originalValue = property.hasMultipleDifferentValues ? 0 : property.intValue;

            var value = EditorGUILayout.MaskField(label, originalValue, entryLabels);
            if (value != originalValue)
            {
                property.intValue = value;
            }
        }

        private void OnSceneGUI()
        {
            var tile = (Tile) target;

            var tileCollections = tile.GetComponentsInParent<TileCollection>();
            if (tileCollections.Length == 0 || tileCollections.Length > 1)
            {
                return;
            }

            if (tile.Neighbors == null)
            {
                return;
            }

            var tileCollection = tileCollections[0];
            var grid = tileCollection.DefaultGrid;

            // Tile border
            var frame = grid.GetTileFrame().Select(x => tile.transform.TransformPoint(x))
                .ToArray();

            Handles.color = useDarkGUI ? Color.black : Color.white;
            Handles.DrawDottedLines(frame, 3);

            // Labels
            TileSideLabels(tile, grid, tileCollection);

            // Connection Lines
            DrawConnectionLines(tile, grid, tileCollection);
            UseConnectionLines(tile, grid, tileCollection);
        }

        private static void TileSideLabels(Tile tile, IGrid grid, TileCollection collection)
        {
            if (!showConnectionNames)
            {
                return;
            }

            //Don't draw the names for multiple selected, or the whole screen will become chaotic
            if (Selection.gameObjects.Length > 1)
            {
                return;
            }

            var viewDir = tile.transform.InverseTransformPoint(Camera.current.transform.position).normalized;

            // Iterate through all sides of the current tile
            var emptyTypes = collection.EmptyTiles.Select(x => x.Name).ToList();
            var overridenNeighbors = tile.GetOverridenNeighborSides();
            for (int side = 0; side < grid.Sides; side++)
            {
                var tileBorder = tile.transform.TransformRay(grid.GetBorderRay(side));

                if (!grid.IsSideVisible(side, viewDir))
                {
                    continue;
                }

                // Iterate all neighbors
                var builder = new StringBuilder();
                var name = $"{grid.SideNames[side]} Side";
                var separator = "-------";
                builder.AppendLine(name);

                var combinedConnectors = tile.GetCombinedConnectors(side);
                if (combinedConnectors.Length > 0)
                {
                    builder.AppendLine(separator);

                    foreach (var assignment in combinedConnectors)
                    {
                        builder.AppendLine(assignment.ToString());
                    }
                }

                builder.AppendLine(separator);

                for (var neighborIndex = 0; neighborIndex < tile.Neighbors.Length; neighborIndex++)
                {
                    var neighbor = overridenNeighbors[neighborIndex];
                    var sides = neighbor.GetSideArray(side, grid.Sides);

                    if (!sides.Any(x => x))
                    {
                        continue;
                    }

                    if (emptyTypes.Contains(neighbor.TileType))
                    {
                        // Empty types
                        builder.AppendLine(neighbor.TileType);
                    }
                    else
                    {
                        // Regular tiles
                        builder.Append(neighbor.TileType);
                        builder.Append(": ");

                        // Iterate all sides of the current neighbor
                        bool isFirst = true;
                        for (var neighborSide = 0; neighborSide < sides.Length; neighborSide++)
                        {
                            var neighborAllowed = sides[neighborSide];
                            if (neighborAllowed)
                            {
                                if (!isFirst)
                                {
                                    builder.Append(", ");
                                }

                                builder.Append(grid.SideNames[neighborSide]);
                                isFirst = false;
                            }
                        }

                        builder.AppendLine();
                    }
                }

                Handles.Label(tileBorder.origin, builder.ToString(),
                    useDarkGUI ? EditorStyles.label : EditorStyles.whiteLabel);
            }
        }

        private void DrawConnectionLines(Tile tile, IGrid grid, TileCollection collection)
        {
            if (!showConnectionLines)
            {
                return;
            }

            var tileLookup = collection.GetTiles()
                .ToLookup(x => x.GetOverridenTileType(), x => x);

            var overridenNeighbors = tile.GetOverridenNeighborSides();

            // Iterate through all neighbors for that side
            for (var neighborIndex = 0; neighborIndex < tile.Neighbors.Length; neighborIndex++)
            {
                var neighbor = overridenNeighbors[neighborIndex];

                // Don't draw anything if the tile type isn't found, e.g. for an empty type
                if (!tileLookup.Contains(neighbor.TileType))
                {
                    continue;
                }

                var neighborTiles = tileLookup[neighbor.TileType];

                foreach (var neighborTile in neighborTiles)
                {
                    var otherOverridenNeighbors = neighborTile.GetOverridenNeighborSides();

                    // Check if the neighbor has the current tile as neighbor
                    var otherNeighbor =
                        otherOverridenNeighbors.FirstOrDefault(x => x.TileType == tile.GetOverridenTileType());

                    if (otherNeighbor == null)
                    {
                        continue;
                    }

                    // Iterate through all sides
                    for (int side = 0;
                         side < grid.Sides;
                         side++)
                    {
                        // Check if the current side is selected in the side mask
                        if ((sideMask & (1 << side)) == 0)
                        {
                            continue;
                        }

                        var startBorder = tile.transform.TransformRay(grid.GetBorderRay(side));
                        for (int otherSide = 0; otherSide < grid.Sides; otherSide++)
                        {
                            if (!neighbor.SupportsNeighborSide(side, otherSide))
                            {
                                continue;
                            }

                            if (!otherNeighbor.SupportsNeighborSide(otherSide, side))
                            {
                                continue;
                            }

                            var endBorder = neighborTile.transform.TransformRay(grid.GetBorderRay(otherSide));

                            if (tile.GetOverridenTileType() == neighbor.TileType && side == otherSide)
                            {
                                Handles.color = useDarkGUI ? Color.black : Color.white;
                                DrawIdentityConnection(startBorder, 1.0f);
                            }
                            else
                            {
                                Handles.color = useDarkGUI ? Color.black : Color.white;
                                Handles.DrawDottedLine(startBorder.origin, endBorder.origin, 1.0f);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawIdentityConnection(Ray startBorder, float screenSpaceSize)
        {
            var start = startBorder.origin;
            var forward = startBorder.direction;
            var right = Vector3.Cross(Vector3.up + Vector3.forward * 0.001f, forward);

            var scale = HandleUtility.GetHandleSize(start) * 0.7f;
            var width = 0.3f;

            var lineSegments = new List<Vector3>
            {
                startBorder.origin,
                start + forward * scale + scale * width * right
            };

            for (int i = 0; i < 10; i++)
            {
                var s = (i / 10.0f) * Mathf.PI;
                var e = ((i + 1) / 10.0f) * Mathf.PI;
                lineSegments.Add(start + forward * scale + Mathf.Sin(s) * scale * width * forward +
                                 Mathf.Cos(s) * scale * width * right);
                lineSegments.Add(start + forward * scale + Mathf.Sin(e) * scale * width * forward +
                                 Mathf.Cos(e) * scale * width * right);
            }

            lineSegments.Add(start + forward * scale - scale * width * right);
            lineSegments.Add(startBorder.origin);

            Handles.DrawDottedLines(lineSegments.ToArray(), screenSpaceSize);
        }

        private void UseConnectionLines(Tile tile, IGrid grid, TileCollection tileCollection)
        {
            if (!useConnectionLines)
            {
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                connectionEndTileType = null;
            }

            Vector3 localViewDir = tile.transform.InverseTransformPoint(Camera.current.transform.position).normalized;

            var tileDictionary = tileCollection.GetTiles()
                .ToLookup(x => x.GetOverridenTileType(), x => x);

            var overridenNeighbors = tile.GetOverridenNeighborSides();

            int selfNeighborIndex = -1;
            for (int i = 0;
                 i < overridenNeighbors.Length;
                 i++)
            {
                if (overridenNeighbors[i].TileType == tile.GetOverridenTileType())
                {
                    selfNeighborIndex = i;
                }
            }

            // When the tile type was just changed, it can happen that it can't find itself in the list of types
            if (selfNeighborIndex < 0)
            {
                return;
            }

            var neighbor = overridenNeighbors[selfNeighborIndex];
            for (int side = 0;
                 side < grid.Sides;
                 side++)
            {
                bool isOnBackSide = !grid.IsSideVisible(side, localViewDir);

                var border = tile.transform.TransformRay(grid.GetBorderRay(side));

                bool isClosest = ConnectionCircleHandle(tile, side, isOnBackSide, border,
                    neighbor.SupportsNeighborSide(connectionStartSide, side));

                if (isClosest)
                {
                    connectionEndTileType = neighbor.TileType;
                    connectionEndSide = side;
                }
            }

            // Draw all circles when a new connection is being created
            if (isDrawingNewLine)
            {
                // Draw new connection line
                var newLineStart = tile.transform.TransformRay(grid.GetBorderRay(connectionStartSide)).origin;
                Handles.color = useDarkGUI ? Color.black : Color.white;
                Handles.DrawLine(newLineStart, HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin);

                // Draw all circles
                for (var neighborIndex = 0; neighborIndex < overridenNeighbors.Length; neighborIndex++)
                {
                    if (neighborIndex == selfNeighborIndex)
                    {
                        continue;
                    }

                    neighbor = overridenNeighbors[neighborIndex];

                    if (!tileDictionary.Contains(neighbor.TileType))
                    {
                        continue;
                    }

                    var otherTiles = tileDictionary[neighbor.TileType];

                    foreach (var otherTile in otherTiles)
                    {
                        localViewDir = otherTile.transform.InverseTransformPoint(Camera.current.transform.position)
                            .normalized;

                        for (int otherSide = 0; otherSide < grid.Sides; otherSide++)
                        {
                            bool isOnBackSide = !grid.IsSideVisible(otherSide, localViewDir);
                            var border = otherTile.transform.TransformRay(grid.GetBorderRay(otherSide));

                            bool isClosest = ConnectionCircleHandle(otherTile, otherSide, isOnBackSide,
                                border, neighbor.SupportsNeighborSide(connectionStartSide, otherSide));

                            if (isClosest)
                            {
                                connectionEndTileType = neighbor.TileType;
                                connectionEndSide = otherSide;
                            }
                        }
                    }
                }

                EditorApplication.QueuePlayerLoopUpdate();
            }

            // Add the new neighbor connection / disconnect
            if (Event.current.type == EventType.Repaint && createNewNeighborConnection)
            {
                createNewNeighborConnection = false;
                isDrawingNewLine = false;

                if (connectionEndTileType == null)
                {
                    return;
                }

                int targetNeighborIndex = -1;
                for (int i = 0; i < tile.Neighbors.Length; i++)
                {
                    if (tile.Neighbors[i].TileType == connectionEndTileType)
                    {
                        targetNeighborIndex = i;
                    }
                }

                var changedObjects = tileCollection.GetTiles(true)
                    .Where(x => x.BaseTile == null && x.TileType == connectionEndTileType)
                    .Append(tile).ToArray();
                Undo.RecordObjects(changedObjects, "Change tile connections");

                // If override was false before, overwrite the neighbor values with the overriden neighbors
                if (!tile.Neighbors[targetNeighborIndex].Overrides[connectionStartSide])
                {
                    tile.Neighbors[targetNeighborIndex].Sides[connectionStartSide] =
                        overridenNeighbors[targetNeighborIndex].Sides[connectionStartSide];
                }

                // Set the override to true
                tile.Neighbors[targetNeighborIndex].Overrides[connectionStartSide] = true;

                // Apply the opposing value from the overriden neighbor array
                var allowedNeighbor = !overridenNeighbors[targetNeighborIndex]
                    .SupportsNeighborSide(connectionStartSide, connectionEndSide);
                tile.Neighbors[targetNeighborIndex]
                    .SetNeighborSideSupport(connectionStartSide, connectionEndSide, allowedNeighbor);

                // If this operation sets a value on the same tile, make sure the change doesn't get overwritten
                if (targetNeighborIndex == selfNeighborIndex)
                {
                    tile.Neighbors[selfNeighborIndex].Overrides[connectionEndSide] = true;
                    tile.Neighbors[selfNeighborIndex]
                        .SetNeighborSideSupport(connectionEndSide, connectionStartSide, allowedNeighbor);
                }

                if (tile.BaseTile == null)
                {
                    tileCollection.UpdateNeighborCompatibility(tile, grid);
                }

                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private bool ConnectionCircleHandle(Tile tile, int side, bool isOnBackSide, Ray border, bool isConnected)
        {
            var controlId =
                GUIUtility.GetControlID($"TileType {tile.TileType} Side {side}".GetHashCode(), FocusType.Passive);

            var viewDir = (Camera.current.transform.position - border.origin).normalized;

            var e = Event.current;
            var circlePos = border.origin + border.direction * circleOffset;

            // Abort early if control is behind camera
            var viewportPoint = Camera.current.WorldToViewportPoint(circlePos);
            var isInView = 0 <= viewportPoint.x && viewportPoint.x <= 1 && 0 <= viewportPoint.y &&
                           viewportPoint.y <= 1.0f && viewportPoint.z > 0;
            if (!isInView)
            {
                return false;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlId && e.button == 0)
                    {
                        isDrawingNewLine = true;
                        connectionStartSide = side;
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();

                        // Change the neighbor compatibility
                        createNewNeighborConnection = true;
                    }

                    break;

                case EventType.MouseDrag:
                    // if i'm controlled, move the point
                    break;

                case EventType.Repaint:
                    ConnectionState state;
                    if (isDrawingNewLine && HandleUtility.nearestControl == controlId)
                    {
                        state = isConnected ? ConnectionState.Disconnect : ConnectionState.Connect;
                    }
                    else
                    {
                        state = isOnBackSide ? ConnectionState.Back : ConnectionState.Front;
                    }

                    var borderColor = useDarkGUI ? Color.black : Color.white;
                    var backgroundColor = GetConnectionCircleColor(state);
                    StixGamesHandles.DrawConnectionCircle(controlId, circlePos, viewDir, circleSize, borderColor,
                        backgroundColor);
                    break;

                case EventType.Layout:
                    Handles.CircleHandleCap(controlId, circlePos, Quaternion.LookRotation(viewDir),
                        circleSize, EventType.Layout);
                    break;
            }

            return HandleUtility.nearestControl == controlId;
        }

        private enum ConnectionState
        {
            Front,
            Back,
            Connect,
            Disconnect
        }

        private static Color GetConnectionCircleColor(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Front:
                    return useDarkGUI
                        ? new Color(0.8f, 0.8f, 0.8f, 0.3f)
                        : new Color(0.3f, 0.3f, 0.3f, 0.3f);
                case ConnectionState.Back:
                    return useDarkGUI
                        ? new Color(0.3f, 0.3f, 0.3f, 0.3f)
                        : new Color(0.1f, 0.1f, 0.1f, 0.3f);
                case ConnectionState.Connect:
                    return Color.green;
                case ConnectionState.Disconnect:
                    return Color.red;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}