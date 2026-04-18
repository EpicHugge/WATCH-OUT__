using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DayData))]
public sealed class DayDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DayData dayData = (DayData)target;
        List<string> messages = dayData.GetValidationMessages();
        if (messages.Count <= 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        for (int i = 0; i < messages.Count; i++)
        {
            EditorGUILayout.HelpBox(messages[i], MessageType.Warning);
        }
    }
}
