using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StixGames.TileComposer
{
    [CustomEditor(typeof(TileCollection))]
    [CanEditMultipleObjects]
    public class TileCollectionEditor : Editor
    {
        private bool showEditor = true;
        private bool showTileCollectionSettings = true;

        private TileSetterMode editorMode;
        private int assignmentIndex;
        private ConnectorEditMode connectorEditMode;

        private static bool useDarkGUI;

        private static float circleSize;
        private static float circleOffset;

        private bool requestSanitize;

        private void OnEnable()
        {
            useDarkGUI = EditorPrefs.GetBool("StixGames.TileComposer.UseDarkGui", false);
            circleSize = EditorPrefs.GetFloat("StixGames.TileComposer.CircleSize", 0.2f);
            circleOffset = EditorPrefs.GetFloat("StixGames.TileComposer.CircleOffset", 0);
        }

        public override void OnInspectorGUI()
        {
            bool clearNeighbors = false;
            bool clearConnectors = false;
            bool autoSetNeighborsFromMesh = false;
            bool autoSetNeighborsFromExamples = false;

            requestSanitize = false;
            using (var fullChangeCheck = new EditorGUI.ChangeCheckScope())
            {
                // Grid editor
                showEditor =
                    EditorGUILayout.Foldout(showEditor, new GUIContent("Tile Collection Editor"));
                if (showEditor)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        using (var change = new EditorGUI.ChangeCheckScope())
                        {
                            useDarkGUI = EditorGUILayout.Toggle(new GUIContent("Use dark GUI",
                                    "If enabled, the GUI is drawn in black instead of white, which can increase visibility."),
                                useDarkGUI);

                            circleSize = Mathf.Max(0, EditorGUILayout.FloatField(
                                new GUIContent("Circle Size", "Change the size of the toggle control circles."),
                                circleSize));

                            circleOffset = EditorGUILayout.FloatField(
                                new GUIContent("Circle Offset", "Change the offset of the toggle control circles."),
                                circleOffset);

                            if (change.changed)
                            {
                                EditorPrefs.SetBool("StixGames.TileComposer.UseDarkGui", useDarkGUI);
                                EditorPrefs.SetFloat("StixGames.TileComposer.CircleSize", circleSize);
                                EditorPrefs.SetFloat("StixGames.TileComposer.CircleOffset", circleOffset);
                            }
                        }

                        EditorGUILayout.LabelField(new GUIContent("Automatic Neighbors"), EditorStyles.boldLabel);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(new GUIContent("Clear neighbors:"), GUILayout.Width(180));
                            clearNeighbors = GUILayout.Button(new GUIContent("Execute"), GUILayout.Width(80),
                                GUILayout.ExpandWidth(true));
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(new GUIContent("Clear connectors:"), GUILayout.Width(180));
                            clearConnectors = GUILayout.Button(new GUIContent("Execute"), GUILayout.Width(80),
                                GUILayout.ExpandWidth(true));
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(new GUIContent("Auto-set neighbors from mesh:"),
                                GUILayout.Width(180));
                            autoSetNeighborsFromMesh = GUILayout.Button(new GUIContent("Execute"), GUILayout.Width(80),
                                GUILayout.ExpandWidth(true));
                        }

                        EditorGUILayout.Space();

                        EditorGUILayout.LabelField("Experimental", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(
                            serializedObject.FindProperty(nameof(TileCollection.ExampleModels)),
                            true);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(new GUIContent("Auto-set neighbors from examples:"),
                                GUILayout.Width(180));
                            autoSetNeighborsFromExamples = GUILayout.Button(new GUIContent("Execute"),
                                GUILayout.Width(80),
                                GUILayout.ExpandWidth(true));
                        }

                        EditorGUILayout.Space();

                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            AssignmentEditor();

                            if (check.changed)
                            {
                                EditorApplication.QueuePlayerLoopUpdate();
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                // Grid Settings
                showTileCollectionSettings =
                    EditorGUILayout.Foldout(showTileCollectionSettings,
                        new GUIContent("Tile Collection Settings"));

                if (showTileCollectionSettings)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        // Grid settings
                        var gridType = serializedObject.FindProperty(nameof(TileCollection.GridType));
                        EditorGUILayout.PropertyField(gridType);

                        var gridScale = serializedObject.FindProperty(nameof(TileCollection.GridScale));
                        if (gridType.hasMultipleDifferentValues)
                        {
                            using (new EditorGUI.DisabledGroupScope(true))
                            {
                                EditorGUI.showMixedValue = true;
                                EditorGUILayout.PropertyField(gridScale, true);
                            }
                        }
                        else
                        {
                            var grid = TileCollection.GetDefaultGrid((GridType) gridType.enumValueIndex);
                            StixGamesEditorExtensions.FixedSizeArray(gridScale, grid.AxisNames.Length, grid.AxisNames);
                        }

                        // Grid Specifics
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Grid Specifics", EditorStyles.boldLabel);
                        using (new EditorGUI.DisabledScope(gridType.hasMultipleDifferentValues ||
                                                           gridType.enumValueIndex != (int) GridType.Hexagon))
                        {
                            EditorGUILayout.PropertyField(
                                serializedObject.FindProperty(nameof(TileCollection.HexagonNormalizeInnerRadius)));
                        }

                        // Tile settings
                        EmptyTiles();
                        Connectors();

                        // Custom properties
                        CustomProperties();

                        serializedObject.ApplyModifiedProperties();
                    }
                }

                requestSanitize |= fullChangeCheck.changed;
            }

            if (clearNeighbors)
            {
                foreach (var tileCollection in targets.Cast<TileCollection>())
                {
                    Undo.RecordObjects(tileCollection.GetTiles(true).Cast<Object>().ToArray(), "Clear neighbors");
                    tileCollection.ClearNeighbors();
                }
            }

            if (clearConnectors)
            {
                foreach (var tileCollection in targets.Cast<TileCollection>())
                {
                    Undo.RecordObjects(tileCollection.GetTiles(true).Cast<Object>().ToArray(), "Clear connectors");
                    tileCollection.ClearConnectors();
                }
            }

            if (autoSetNeighborsFromMesh)
            {
                foreach (var tileCollection in targets.Cast<TileCollection>())
                {
                    Undo.RecordObjects(tileCollection.GetTiles(true).Cast<Object>().ToArray(),
                        "Set neighbors from mesh");
                    tileCollection.CalcNeighborCompatibilityFromMesh();
                }
            }

            if (autoSetNeighborsFromExamples)
            {
                foreach (var tileCollection in targets.Cast<TileCollection>())
                {
                    Undo.RecordObjects(tileCollection.GetTiles(true).Cast<Object>().ToArray(),
                        "Set neighbors from example");
                    tileCollection.CalcNeighborCompatibilityFromExamples();
                }
            }

            // Sanitize tile collections
            if (requestSanitize)
            {
                foreach (var collection in targets.Cast<TileCollection>())
                {
                    DataSanitizer.SanitizeTileCollection(collection);
                }
            }
        }

        private void AssignmentEditor()
        {
            EditorGUILayout.LabelField(new GUIContent("Empty Tile Assignment"), EditorStyles.boldLabel);
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.LabelField(
                    new GUIContent("Multi editing of tile collection assignments is not yet supported."));
            }
            else
            {
                var emptyTiles = serializedObject.FindProperty(nameof(TileCollection.EmptyTiles));

                StixGamesEditorExtensions.CustomArrayProperty(emptyTiles, (property, i) =>
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var isCurrentlySelected = editorMode == TileSetterMode.Empty && assignmentIndex == i;

                            var name = property.FindPropertyRelative(nameof(EmptyTile.Name)).stringValue;
                            EditorGUILayout.LabelField(new GUIContent(name), GUILayout.Width(180));

                            var prevColor = GUI.color;
                            if (isCurrentlySelected)
                            {
                                GUI.color = Color.green;
                            }

                            var result = GUILayout.Button(new GUIContent("Assign"), GUILayout.Width(80),
                                GUILayout.ExpandWidth(true));
                            GUI.color = prevColor;

                            if (result)
                            {
                                if (isCurrentlySelected)
                                {
                                    editorMode = TileSetterMode.None;
                                }
                                else
                                {
                                    editorMode = TileSetterMode.Empty;
                                    assignmentIndex = i;
                                }
                            }
                        }
                    },
                    (p, i) => { },
                    true, true, false, true);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(new GUIContent("Connector Assignment"), EditorStyles.boldLabel);
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.LabelField(
                    new GUIContent("Multi editing of tile collection assignments is not yet supported."));
            }
            else
            {
                var connectors = serializedObject.FindProperty(nameof(TileCollection.Connectors));

                StixGamesEditorExtensions.CustomArrayProperty(connectors, (property, i) =>
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var isCurrentlySelected = false;
                            var wasSelected = false;

                            var name = property.FindPropertyRelative(nameof(ConnectorType.Name)).stringValue;
                            var bidirectional = property.FindPropertyRelative(nameof(ConnectorType.IsBidirectional))
                                .boolValue;

                            EditorGUILayout.LabelField(new GUIContent(name), GUILayout.Width(180));

                            var prevColor = GUI.color;

                            bool result = false;
                            ConnectorEditMode type = ConnectorEditMode.In;

                            if (bidirectional)
                            {
                                isCurrentlySelected = editorMode == TileSetterMode.Connector && assignmentIndex == i &&
                                                      connectorEditMode == ConnectorEditMode.Bidirectional;

                                if (isCurrentlySelected)
                                {
                                    GUI.color = Color.green;
                                }

                                result = GUILayout.Button(new GUIContent("Assign"), GUILayout.Width(80),
                                    GUILayout.ExpandWidth(true));
                                type = ConnectorEditMode.Bidirectional;
                                wasSelected = isCurrentlySelected;

                                GUI.color = prevColor;
                            }
                            else
                            {
                                isCurrentlySelected = editorMode == TileSetterMode.Connector && assignmentIndex == i &&
                                                      connectorEditMode == ConnectorEditMode.In;
                                if (isCurrentlySelected)
                                {
                                    GUI.color = Color.green;
                                }

                                if (GUILayout.Button(new GUIContent("Set In"), GUILayout.Width(50),
                                        GUILayout.ExpandWidth(true)))
                                {
                                    result = true;
                                    type = ConnectorEditMode.In;
                                    wasSelected = isCurrentlySelected;
                                }

                                GUI.color = prevColor;

                                isCurrentlySelected = editorMode == TileSetterMode.Connector && assignmentIndex == i &&
                                                      connectorEditMode == ConnectorEditMode.Out;
                                if (isCurrentlySelected)
                                {
                                    GUI.color = Color.green;
                                }

                                if (GUILayout.Button(new GUIContent("Set Out"), GUILayout.Width(50),
                                        GUILayout.ExpandWidth(true)))
                                {
                                    result = true;
                                    type = ConnectorEditMode.Out;
                                    wasSelected = isCurrentlySelected;
                                }

                                GUI.color = prevColor;
                            }

                            if (result)
                            {
                                if (wasSelected)
                                {
                                    editorMode = TileSetterMode.None;
                                }
                                else
                                {
                                    editorMode = TileSetterMode.Connector;
                                    assignmentIndex = i;
                                    connectorEditMode = type;
                                }
                            }
                        }
                    },
                    (p, i) => { },
                    true, true, false, true);
            }
        }

        private void EmptyTiles()
        {
            var emptyTypes = serializedObject.FindProperty(nameof(TileCollection.EmptyTiles));

            using (new EditorGUI.DisabledGroupScope(emptyTypes.hasMultipleDifferentValues))
            {
                if (emptyTypes.hasMultipleDifferentValues)
                {
                    EditorGUILayout.PropertyField(emptyTypes);
                }
                else
                {
                    var tileCollections = targets.Cast<TileCollection>().ToArray();

                    StixGamesEditorExtensions.CustomArrayProperty(emptyTypes,
                        (p, i) =>
                        {
                            p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, p.displayName);
                            if (p.isExpanded)
                            {
                                var nameProperty = p.FindPropertyRelative(nameof(EmptyTile.Name));
                                var prevValue = nameProperty.stringValue;

                                EditorGUILayout.PropertyField(nameProperty);
                                EditorGUILayout.PropertyField(p.FindPropertyRelative(nameof(EmptyTile.Weight)));
                                EditorGUILayout.PropertyField(p.FindPropertyRelative(nameof(EmptyTile.IsCompressible)));

                                EnsureUniqueName(emptyTypes, p, i, nameof(EmptyTile.Name));

                                if (prevValue != nameProperty.stringValue)
                                {
                                    foreach (var tileCollection in tileCollections)
                                    {
                                        tileCollection.UpdateTileTypes(prevValue, nameProperty.stringValue);
                                    }
                                }
                            }
                        },
                        (p, i) => { EnsureUniqueName(emptyTypes, p, i, nameof(EmptyTile.Name)); },
                        false, true, true);
                }
            }
        }

        private void Connectors()
        {
            var connectors = serializedObject.FindProperty(nameof(TileCollection.Connectors));

            using (new EditorGUI.DisabledGroupScope(connectors.hasMultipleDifferentValues))
            {
                if (connectors.hasMultipleDifferentValues)
                {
                    EditorGUILayout.PropertyField(connectors);
                }
                else
                {
                    var tileCollections = targets.Cast<TileCollection>().ToArray();

                    StixGamesEditorExtensions.CustomArrayProperty(connectors,
                        (p, i) =>
                        {
                            p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, p.displayName);
                            if (p.isExpanded)
                            {
                                var nameProperty = p.FindPropertyRelative(nameof(ConnectorType.Name));
                                var prevValue = nameProperty.stringValue;

                                EditorGUILayout.PropertyField(nameProperty);
                                EditorGUILayout.PropertyField(
                                    p.FindPropertyRelative(nameof(ConnectorType.IsBidirectional)));

                                EnsureUniqueName(connectors, p, i, nameof(ConnectorType.Name));

                                if (prevValue != nameProperty.stringValue)
                                {
                                    foreach (var tileCollection in tileCollections)
                                    {
                                        tileCollection.UpdateConnector(prevValue, nameProperty.stringValue);
                                    }
                                }
                            }
                        },
                        (p, i) => { EnsureUniqueName(connectors, p, i, nameof(ConnectorType.Name)); },
                        false, true, true);
                }
            }
        }

        private void CustomProperties()
        {
            var properties = serializedObject.FindProperty(nameof(TileCollection.CustomValues));

            using (new EditorGUI.DisabledGroupScope(properties.hasMultipleDifferentValues))
            {
                if (properties.hasMultipleDifferentValues)
                {
                    EditorGUILayout.PropertyField(properties);
                }
                else
                {
                    var tileCollections = targets.Cast<TileCollection>().ToArray();

                    StixGamesEditorExtensions.CustomArrayProperty(properties,
                        (p, i) =>
                        {
                            var prevValue = p.stringValue;

                            EditorGUILayout.PropertyField(p);
                            EnsureUniqueName(properties, p, i);

                            // Propagate the change to all children
                            if (prevValue != p.stringValue)
                            {
                                foreach (var tileCollection in tileCollections)
                                {
                                    tileCollection.UpdateCustomProperty(prevValue, p.stringValue);
                                }
                            }
                        }, (p, i) => { EnsureUniqueName(properties, p, i); },
                        false, true, true);
                }
            }
        }

        private static void EnsureUniqueName(SerializedProperty emptyTypes, SerializedProperty p, int index,
            string nameField)
        {
            var emptyNames = new List<string>();
            for (var i = 0; i < emptyTypes.arraySize; i++)
            {
                if (index != i)
                {
                    emptyNames.Add(emptyTypes.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameField).stringValue);
                }
            }

            var nameProperty = p.FindPropertyRelative(nameField);

            nameProperty.stringValue = DataSanitizer.UniqueName(emptyNames, nameProperty.stringValue);
        }

        private static void EnsureUniqueName(SerializedProperty emptyTypes, SerializedProperty p, int index)
        {
            var emptyNames = new List<string>();
            for (var i = 0; i < emptyTypes.arraySize; i++)
            {
                if (index != i)
                {
                    emptyNames.Add(emptyTypes.GetArrayElementAtIndex(i).stringValue);
                }
            }

            p.stringValue = DataSanitizer.UniqueName(emptyNames, p.stringValue);
        }

        private void OnSceneGUI()
        {
            if (editorMode == TileSetterMode.None)
            {
                return;
            }

            var collection = (TileCollection) target;
            var grid = collection.DefaultGrid;

            // Draw tile frames
            var list = new List<Vector3>();
            var frame = grid.GetTileFrame();
            foreach (var tile in collection.GetTiles())
            {
                list.AddRange(frame.Select(x => tile.transform.TransformPoint(x)).ToArray());
            }

            Handles.color = useDarkGUI ? Color.black : Color.white;
            Handles.DrawDottedLines(list.ToArray(), 3);

            if (editorMode == TileSetterMode.Empty)
            {
                // Abort when selected index is out of range
                if (collection.EmptyTiles.Length <= assignmentIndex)
                {
                    editorMode = TileSetterMode.None;
                    return;
                }

                var tileType = collection.EmptyTiles[assignmentIndex].Name;

                // TODO: Add non-root tiles too
                foreach (var tile in collection.GetTiles().Where(x => x.BaseTile == null))
                {
                    var neighbor = tile.Neighbors.First(x => x.TileType == tileType);

                    for (int side = 0; side < grid.Sides; side++)
                    {
                        var isEnabled = neighbor.Sides[side] != 0;
                        var toggle = SideToggle(grid, tile, side, isEnabled);

                        if (toggle)
                        {
                            Undo.RecordObject(tile, "Toggle empty type");
                            if (isEnabled)
                            {
                                neighbor.Sides[side] = 0;
                            }
                            else
                            {
                                neighbor.Sides[side] = ~0;
                            }
                        }
                    }
                }
            }

            if (editorMode == TileSetterMode.Connector)
            {
                // Abort when selected index is out of range
                if (collection.Connectors.Length <= assignmentIndex)
                {
                    editorMode = TileSetterMode.None;
                    return;
                }

                // Abort when selected mode is impossible with the current selection, bidirectional != bidirectional edit mode
                if (collection.Connectors[assignmentIndex].IsBidirectional !=
                    (connectorEditMode == ConnectorEditMode.Bidirectional))
                {
                    editorMode = TileSetterMode.None;
                    return;
                }

                var connectorName = collection.Connectors[assignmentIndex].Name;

                // TODO: Add non-root tiles too
                foreach (var tile in collection.GetTiles().Where(x => x.BaseTile == null))
                {
                    // Check tile connector sides
                    if (tile.Connectors == null)
                    {
                        tile.Connectors = new ConnectorSide[grid.Sides];
                    }

                    if (tile.Connectors.Length != grid.Sides)
                    {
                        Array.Resize(ref tile.Connectors, grid.Sides);
                    }

                    for (int side = 0; side < grid.Sides; side++)
                    {
                        var connectorSide = tile.Connectors[side];

                        // Check tile connectors on side
                        if (connectorSide.Connectors == null)
                        {
                            connectorSide.Connectors = new ConnectorAssignment[0];
                        }


                        var connectors = connectorSide.Connectors.Where(x => x.Name == connectorName).ToArray();

                        // If there are multiple connectors with the same type, combine them into one
                        if (connectors.Length > 1)
                        {
                            var allowIn = connectors.Any(x => x.ConnectionType.HasFlag(ConnectionType.In));
                            var allowOut = connectors.Any(x => x.ConnectionType.HasFlag(ConnectionType.Out));
                            var connectionType = allowIn ? ConnectionType.In : 0;
                            connectionType |= allowOut ? ConnectionType.Out : 0;

                            var combinedConnector = new ConnectorAssignment(connectorName, connectionType);
                            connectorSide.Connectors = connectorSide.Connectors
                                .Where(x => x.Name != connectorName)
                                .Append(combinedConnector)
                                .ToArray();

                            connectors = new[] {combinedConnector};
                        }

                        var connector = connectors.Length == 1 ? connectors[0] : null;

                        bool isEnabled = connector != null;
                        var toggle = SideToggle(grid, tile, side, isEnabled,
                            connector?.ConnectionType ?? ConnectionType.In);

                        if (toggle)
                        {
                            Undo.RecordObject(tile, "Toggle connector");

                            if (connector == null)
                            {
                                connector = new ConnectorAssignment(connectorName, 0);
                                connectorSide.Connectors = connectorSide.Connectors
                                    .Append(connector)
                                    .ToArray();
                            }

                            switch (connectorEditMode)
                            {
                                case ConnectorEditMode.In:
                                    connector.ConnectionType ^= ConnectionType.In;
                                    break;
                                case ConnectorEditMode.Out:
                                    connector.ConnectionType ^= ConnectionType.Out;
                                    break;
                                case ConnectorEditMode.Bidirectional:
                                    connector.ConnectionType = connector.ConnectionType == ConnectionType.Bidirectional
                                        ? 0
                                        : ConnectionType.Bidirectional;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            connectorSide.Connectors = connectorSide.Connectors
                                .Where(x => x.ConnectionType != 0)
                                .ToArray();
                        }
                    }
                }
            }
        }

        private static bool SideToggle(IGrid grid, Tile tile, int side, bool isEnabled,
            ConnectionType connectionType = ConnectionType.In)
        {
            var controlId = GUIUtility.GetControlID($"TileType {tile.TileType} Side {side}".GetHashCode(),
                FocusType.Passive);

            var border = tile.transform.TransformRay(grid.GetBorderRay(side));

            Vector3 localViewDir = tile.transform.InverseTransformPoint(Camera.current.transform.position).normalized;
            var backgroundColor = GetHandleColor(isEnabled, grid.IsSideVisible(side, localViewDir), connectionType);

            return SideToggleHandle(controlId, border, backgroundColor, circleSize);
        }

        private static Color GetHandleColor(bool isEnabled, bool isVisible, ConnectionType connectionType)
        {
            if (isEnabled)
            {
                if (isVisible)
                {
                    switch (connectionType)
                    {
                        case ConnectionType.In:
                            return new Color(0, 1, 0, 0.5f);
                        case ConnectionType.Out:
                            return new Color(1, 0, 0, 0.5f);
                        case ConnectionType.Bidirectional:
                            return new Color(1, 1, 0, 0.5f);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(connectionType), connectionType, null);
                    }
                }
                else
                {
                    switch (connectionType)
                    {
                        case ConnectionType.In:
                            return new Color(0, 0.6f, 0, 0.3f);
                        case ConnectionType.Out:
                            return new Color(0.6f, 0, 0, 0.3f);
                        case ConnectionType.Bidirectional:
                            return new Color(0.5f, 0.5f, 0, 0.3f);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(connectionType), connectionType, null);
                    }
                }
            }
            else
            {
                if (isVisible)
                {
                    return useDarkGUI
                        ? new Color(0.8f, 0.8f, 0.8f, 0.5f)
                        : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                else
                {
                    return useDarkGUI
                        ? new Color(0.3f, 0.3f, 0.3f, 0.3f)
                        : new Color(0.1f, 0.1f, 0.1f, 0.3f);
                }
            }
        }

        private static bool SideToggleHandle(int controlID, Ray border, Color backgroundColor, float radius = 0.2f)
        {
            var borderColor = useDarkGUI ? Color.black : Color.white;
            borderColor.a = backgroundColor.a + 0.2f;

            var viewDir = (Camera.current.transform.position - border.origin).normalized;

            // Abort early if control is behind camera
            var viewportPoint = Camera.current.WorldToViewportPoint(border.origin);
            var isInView = 0 <= viewportPoint.x && viewportPoint.x <= 1 && 0 <= viewportPoint.y &&
                           viewportPoint.y <= 1.0f && viewportPoint.z > 0;
            if (!isInView)
            {
                return false;
            }

            var wasToggled = false;
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseUp:
                    if (HandleUtility.nearestControl == controlID && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                        wasToggled = true;
                    }

                    break;

                case EventType.Repaint:
                    StixGamesHandles.DrawConnectionCircle(controlID, border.origin, viewDir, radius, borderColor,
                        backgroundColor);
                    break;

                case EventType.Layout:
                    Handles.CircleHandleCap(controlID, border.origin, Quaternion.LookRotation(viewDir),
                        radius, EventType.Layout);
                    break;
            }

            return wasToggled && HandleUtility.nearestControl == controlID;
        }

        private enum TileSetterMode
        {
            None,
            Empty,
            Connector
        }

        public enum ConnectorEditMode
        {
            In,
            Out,
            Bidirectional,
        }
    }
}