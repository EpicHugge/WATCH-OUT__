using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CassetteShelf))]
public sealed class CassetteShelfEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space();

        CassetteShelf shelf = (CassetteShelf)target;
        EditorGUILayout.HelpBox(
            $"Grid Capacity: {shelf.Capacity}\nConfigured Slots: {shelf.Slots.Count}",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Match Slots To Grid"))
            {
                Undo.RecordObject(shelf, "Match Cassette Shelf Slots");
                shelf.SyncSlotsToGrid();
                EditorUtility.SetDirty(shelf);
            }

            if (GUILayout.Button("Rebuild Shelf"))
            {
                shelf.RebuildShelf();
            }
        }

        if (GUILayout.Button("Clear Generated"))
        {
            shelf.ClearGeneratedChildren();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
