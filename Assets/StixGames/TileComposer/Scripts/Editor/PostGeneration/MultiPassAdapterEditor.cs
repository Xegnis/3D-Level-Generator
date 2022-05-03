using StixGames.TileComposer;
using UnityEditor;

[CustomEditor(typeof(MultiPassAdapter))]
public class MultiPassAdapterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var origin = serializedObject.FindProperty(nameof(MultiPassAdapter.Origin));
        EditorGUILayout.PropertyField(origin, true);

        var target = serializedObject.FindProperty(nameof(MultiPassAdapter.Target));
        EditorGUILayout.PropertyField(target, true);

        EditorGUILayout.Space();

        var originTileCollection = origin.objectReferenceValue as TileCollection;
        var targetTileCollection = (target.objectReferenceValue as TileComposer)?.TileCollection;

        var tileCollectionsNull = originTileCollection == null || targetTileCollection == null;
        if (tileCollectionsNull)
        {
            EditorGUILayout.LabelField("Origin or Target.TileCollection is null!");
            EditorGUILayout.Space();
        }

        using (new EditorGUI.DisabledScope(tileCollectionsNull))
        {
            // Blocked conversions
            var blockedTiles = serializedObject.FindProperty(nameof(MultiPassAdapter.BlockedTileConversions));
            StixGamesEditorExtensions.CustomArrayProperty(blockedTiles,
                (property, index) => BlockedTileConversionDrawer(originTileCollection, targetTileCollection, property, index),
                (p, i) => { }, false, true, true, false);
            
            // Fixed conversions
            var fixedTiles = serializedObject.FindProperty(nameof(MultiPassAdapter.FixedTileConversions));
            StixGamesEditorExtensions.CustomArrayProperty(fixedTiles,
                (property, index) => FixedTileConversionDrawer(originTileCollection, targetTileCollection, property, index),
                (p, i) => { }, false, true, true, false);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void BlockedTileConversionDrawer(TileCollection origin, TileCollection target, SerializedProperty property, int index)
    {
        if (origin == null || target == null)
        {
            EditorGUILayout.PropertyField(property);
            return;
        }
        
        var source = property.FindPropertyRelative(nameof(BlockedTileConversion.SourceTile));
        TileComposerEditorUtility.TileTypeSelector(origin, source, true, false);
        
        var blocked = property.FindPropertyRelative(nameof(BlockedTileConversion.BlockedTiles));
        StixGamesEditorExtensions.CustomArrayProperty(blocked, (p, i) =>
        {
            TileComposerEditorUtility.TileTypeSelector(target, p, true, false);
        }, (p, i) => {}, false, true, true);
    }
    
    private void FixedTileConversionDrawer(TileCollection origin, TileCollection target, SerializedProperty property, int index)
    {
        if (origin == null || target == null)
        {
            EditorGUILayout.PropertyField(property);
            return;
        }
        
        var source = property.FindPropertyRelative(nameof(FixedTileConversion.SourceTile));
        TileComposerEditorUtility.TileTypeSelector(origin, source, true, false);
        
        var targetTile = property.FindPropertyRelative(nameof(FixedTileConversion.TargetTile));
        TileComposerEditorUtility.TileTypeSelector(target, targetTile, true, false);
    }
}