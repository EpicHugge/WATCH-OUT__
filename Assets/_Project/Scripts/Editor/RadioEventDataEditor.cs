using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RadioEventData))]
public sealed class RadioEventDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RadioEventData radioEvent = (RadioEventData)target;
        List<string> messages = radioEvent.GetValidationMessages();
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
