using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(DialogueConversation))]
public sealed class DialogueConversationEditor : Editor
{
    private const string TagReferenceText =
        "TMP color: <color=#FF0000>text</color>\n" +
        "Inline effects: <wave>text</wave>  <shake>text</shake>  <glitch>text</glitch>\n" +
        "Pause: [pause=0.3]\n" +
        "Line / preset effects: Whisper, Shout";

    private SerializedProperty overrideRevealSpeedProperty;
    private SerializedProperty charactersPerSecondProperty;
    private SerializedProperty defaultVoiceProfileProperty;
    private SerializedProperty speakerVoicesProperty;
    private SerializedProperty linesProperty;

    private ReorderableList linesList;

    private void OnEnable()
    {
        overrideRevealSpeedProperty = serializedObject.FindProperty("overrideRevealSpeed");
        charactersPerSecondProperty = serializedObject.FindProperty("charactersPerSecond");
        defaultVoiceProfileProperty = serializedObject.FindProperty("defaultVoiceProfile");
        speakerVoicesProperty = serializedObject.FindProperty("speakerVoices");
        linesProperty = serializedObject.FindProperty("lines");

        linesList = new ReorderableList(serializedObject, linesProperty, true, true, true, true);
        linesList.drawHeaderCallback = DrawLinesHeader;
        linesList.drawElementCallback = DrawLineProperty;
        linesList.elementHeightCallback = GetLineElementHeight;
        linesList.onAddCallback = AddLine;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTagReferenceBox();
        EditorGUILayout.Space();

        DrawDefaultsSection();
        EditorGUILayout.Space();

        linesList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTagReferenceBox()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Inline Tag Reference", EditorStyles.boldLabel);
            float height = EditorStyles.textArea.CalcHeight(new GUIContent(TagReferenceText), EditorGUIUtility.currentViewWidth - 52f);
            EditorGUILayout.SelectableLabel(TagReferenceText, EditorStyles.textArea, GUILayout.MinHeight(height + 6f));
        }
    }

    private void DrawDefaultsSection()
    {
        EditorGUILayout.LabelField("Conversation Defaults", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(overrideRevealSpeedProperty, new GUIContent("Override Reveal Speed"));
        if (overrideRevealSpeedProperty.boolValue)
        {
            EditorGUILayout.PropertyField(charactersPerSecondProperty, new GUIContent("Characters Per Second"));
        }

        EditorGUILayout.PropertyField(defaultVoiceProfileProperty, new GUIContent("Default Voice Profile"));
        EditorGUILayout.PropertyField(speakerVoicesProperty, new GUIContent("Speaker Voice Mapping"), true);
    }

    private void DrawLinesHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Lines");
    }

    private void AddLine(ReorderableList list)
    {
        int newIndex = linesProperty.arraySize;
        linesProperty.arraySize++;
        SerializedProperty newLineProperty = linesProperty.GetArrayElementAtIndex(newIndex);
        ResetLine(newLineProperty);
        newLineProperty.isExpanded = false;
        serializedObject.ApplyModifiedProperties();
    }

    private float GetLineElementHeight(int index)
    {
        SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(index);
        return EditorGUI.GetPropertyHeight(lineProperty, true) + 6f;
    }

    private void DrawLineProperty(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(index);
        rect.y += 2f;
        rect.height = EditorGUI.GetPropertyHeight(lineProperty, true);
        EditorGUI.PropertyField(rect, lineProperty, GUIContent.none, true);
    }

    private static void ResetLine(SerializedProperty lineProperty)
    {
        lineProperty.FindPropertyRelative("speakerPreset").objectReferenceValue = null;
        lineProperty.FindPropertyRelative("speakerName").stringValue = string.Empty;
        lineProperty.FindPropertyRelative("dialogueText").stringValue = string.Empty;
        lineProperty.FindPropertyRelative("overrideTextEffect").boolValue = false;
        lineProperty.FindPropertyRelative("textEffect").enumValueIndex = 0;
        lineProperty.FindPropertyRelative("overrideSpeakerNameColor").boolValue = false;
        lineProperty.FindPropertyRelative("speakerNameColor").colorValue = Color.white;
        lineProperty.FindPropertyRelative("overrideDialogueTextColor").boolValue = false;
        lineProperty.FindPropertyRelative("dialogueTextColor").colorValue = Color.white;
        lineProperty.FindPropertyRelative("overrideBorderColor").boolValue = false;
        lineProperty.FindPropertyRelative("borderColor").colorValue = new Color(0.57f, 0.67f, 0.69f, 0.82f);
        lineProperty.FindPropertyRelative("overrideRevealSpeed").boolValue = false;
        lineProperty.FindPropertyRelative("charactersPerSecond").floatValue = 60f;
        lineProperty.FindPropertyRelative("voiceProfileOverride").objectReferenceValue = null;
        lineProperty.FindPropertyRelative("autoAdvance").boolValue = false;
        lineProperty.FindPropertyRelative("autoAdvanceDelay").floatValue = 0.15f;

        SerializedProperty lineEventProperty = lineProperty.FindPropertyRelative("lineEvent");
        lineEventProperty.FindPropertyRelative("eventType").enumValueIndex = 0;
        lineEventProperty.FindPropertyRelative("triggerTiming").enumValueIndex = 0;
        lineEventProperty.FindPropertyRelative("duration").floatValue = 0.2f;
        lineEventProperty.FindPropertyRelative("magnitude").floatValue = 0.08f;
        lineEventProperty.FindPropertyRelative("frequency").floatValue = 24f;
    }
}
