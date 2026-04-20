using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueLine))]
public sealed class DialogueLineDrawer : PropertyDrawer
{
    private const float MinTextHeight = 52f;
    private const float MaxTextHeight = 110f;
    private const float OuterPadding = 8f;
    private const float InnerIndent = 10f;
    private const float SectionSpacing = 4f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float width = Mathf.Max(180f, EditorGUIUtility.currentViewWidth - 140f);
        float height = OuterPadding;

        height += EditorGUIUtility.singleLineHeight + spacing;
        height += GetTextAreaHeight(property.FindPropertyRelative("dialogueText"), width) + spacing;

        if (property.isExpanded)
        {
            height += GetSectionHeight(
                property.FindPropertyRelative("speakerPreset"),
                property.FindPropertyRelative("speakerName"));

            height += GetSectionHeight(
                property.FindPropertyRelative("overrideSpeakerNameColor"),
                property.FindPropertyRelative("overrideSpeakerNameColor").boolValue ? property.FindPropertyRelative("speakerNameColor") : null,
                property.FindPropertyRelative("overrideDialogueTextColor"),
                property.FindPropertyRelative("overrideDialogueTextColor").boolValue ? property.FindPropertyRelative("dialogueTextColor") : null,
                property.FindPropertyRelative("overrideBorderColor"),
                property.FindPropertyRelative("overrideBorderColor").boolValue ? property.FindPropertyRelative("borderColor") : null);

            height += GetSectionHeight(
                property.FindPropertyRelative("overrideTextEffect"),
                property.FindPropertyRelative("overrideTextEffect").boolValue ? property.FindPropertyRelative("textEffect") : null);

            height += GetSectionHeight(property.FindPropertyRelative("voiceProfileOverride"));

            height += GetSectionHeight(
                property.FindPropertyRelative("overrideRevealSpeed"),
                property.FindPropertyRelative("overrideRevealSpeed").boolValue ? property.FindPropertyRelative("charactersPerSecond") : null,
                property.FindPropertyRelative("autoAdvance"),
                property.FindPropertyRelative("autoAdvance").boolValue ? property.FindPropertyRelative("autoAdvanceDelay") : null);

            SerializedProperty eventProperty = property.FindPropertyRelative("lineEvent");
            height += GetSectionHeight(
                eventProperty.FindPropertyRelative("eventType"),
                eventProperty.FindPropertyRelative("eventType").enumValueIndex != 0 ? eventProperty.FindPropertyRelative("triggerTiming") : null,
                eventProperty.FindPropertyRelative("eventType").enumValueIndex != 0 ? eventProperty.FindPropertyRelative("duration") : null,
                eventProperty.FindPropertyRelative("eventType").enumValueIndex != 0 ? eventProperty.FindPropertyRelative("magnitude") : null,
                eventProperty.FindPropertyRelative("eventType").enumValueIndex != 0 ? eventProperty.FindPropertyRelative("frequency") : null);
        }

        height += OuterPadding;
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

        Rect contentRect = new Rect(
            position.x + OuterPadding,
            position.y + OuterPadding,
            position.width - (OuterPadding * 2f),
            position.height - (OuterPadding * 2f));

        float y = contentRect.y;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect foldoutRect = new Rect(contentRect.x, y, 16f, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

        Rect summaryRect = new Rect(contentRect.x + 18f, y, contentRect.width - 18f, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(summaryRect, BuildSummary(property), GetSummaryStyle());
        y += EditorGUIUtility.singleLineHeight + spacing;

        SerializedProperty dialogueTextProperty = property.FindPropertyRelative("dialogueText");
        float textHeight = GetTextAreaHeight(dialogueTextProperty, contentRect.width);
        Rect textRect = new Rect(contentRect.x, y, contentRect.width, textHeight);
        string updatedText = EditorGUI.TextArea(textRect, dialogueTextProperty.stringValue ?? string.Empty, GetTextAreaStyle());
        if (updatedText != dialogueTextProperty.stringValue)
        {
            dialogueTextProperty.stringValue = updatedText;
        }

        y += textHeight + spacing;

        if (property.isExpanded)
        {
            float sectionX = contentRect.x + InnerIndent;
            float sectionWidth = contentRect.width - InnerIndent;

            DrawSpeakerSection(property, sectionX, sectionWidth, ref y);
            DrawColorsSection(property, sectionX, sectionWidth, ref y);
            DrawEffectsSection(property, sectionX, sectionWidth, ref y);
            DrawAudioSection(property, sectionX, sectionWidth, ref y);
            DrawTimingSection(property, sectionX, sectionWidth, ref y);
            DrawEventsSection(property, sectionX, sectionWidth, ref y);
        }

        EditorGUI.EndProperty();
    }

    private static float GetSectionHeight(params SerializedProperty[] properties)
    {
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float height = EditorGUIUtility.singleLineHeight + spacing;

        for (int i = 0; i < properties.Length; i++)
        {
            SerializedProperty property = properties[i];
            if (property == null)
            {
                continue;
            }

            height += EditorGUI.GetPropertyHeight(property, true) + spacing;
        }

        return height + SectionSpacing;
    }

    private static void DrawSpeakerSection(SerializedProperty property, float x, float width, ref float y)
    {
        DrawSectionHeader("Speaker", x, width, ref y);
        DrawProperty(property.FindPropertyRelative("speakerPreset"), "Speaker Preset", x, width, ref y);
        DrawProperty(property.FindPropertyRelative("speakerName"), "Speaker Name Override", x, width, ref y);
    }

    private static void DrawColorsSection(SerializedProperty property, float x, float width, ref float y)
    {
        DrawSectionHeader("Colors And Overrides", x, width, ref y);
        DrawOverrideColorField(property.FindPropertyRelative("overrideSpeakerNameColor"), property.FindPropertyRelative("speakerNameColor"), "Speaker Name Color", x, width, ref y);
        DrawOverrideColorField(property.FindPropertyRelative("overrideDialogueTextColor"), property.FindPropertyRelative("dialogueTextColor"), "Dialogue Text Color", x, width, ref y);
        DrawOverrideColorField(property.FindPropertyRelative("overrideBorderColor"), property.FindPropertyRelative("borderColor"), "Border Color", x, width, ref y);
    }

    private static void DrawEffectsSection(SerializedProperty property, float x, float width, ref float y)
    {
        DrawSectionHeader("Effects", x, width, ref y);
        DrawOverrideField(property.FindPropertyRelative("overrideTextEffect"), property.FindPropertyRelative("textEffect"), "Text Effect Override", x, width, ref y);
    }

    private static void DrawAudioSection(SerializedProperty property, float x, float width, ref float y)
    {
        DrawSectionHeader("Audio", x, width, ref y);
        DrawProperty(property.FindPropertyRelative("voiceProfileOverride"), "Voice Profile Override", x, width, ref y);
    }

    private static void DrawTimingSection(SerializedProperty property, float x, float width, ref float y)
    {
        DrawSectionHeader("Timing And Auto-Advance", x, width, ref y);
        DrawOverrideField(property.FindPropertyRelative("overrideRevealSpeed"), property.FindPropertyRelative("charactersPerSecond"), "Reveal Speed Override", x, width, ref y, "Characters Per Second");
        DrawProperty(property.FindPropertyRelative("autoAdvance"), "Auto Advance", x, width, ref y);
        if (property.FindPropertyRelative("autoAdvance").boolValue)
        {
            DrawProperty(property.FindPropertyRelative("autoAdvanceDelay"), "Auto Advance Delay", x, width, ref y);
        }
    }

    private static void DrawEventsSection(SerializedProperty property, float x, float width, ref float y)
    {
        DrawSectionHeader("Events", x, width, ref y);

        SerializedProperty eventProperty = property.FindPropertyRelative("lineEvent");
        DrawProperty(eventProperty.FindPropertyRelative("eventType"), "Event", x, width, ref y);
        if (eventProperty.FindPropertyRelative("eventType").enumValueIndex == 0)
        {
            return;
        }

        DrawProperty(eventProperty.FindPropertyRelative("triggerTiming"), "Trigger Timing", x, width, ref y);
        DrawProperty(eventProperty.FindPropertyRelative("duration"), "Duration", x, width, ref y);
        DrawProperty(eventProperty.FindPropertyRelative("magnitude"), "Magnitude", x, width, ref y);
        DrawProperty(eventProperty.FindPropertyRelative("frequency"), "Frequency", x, width, ref y);
    }

    private static void DrawSectionHeader(string title, float x, float width, ref float y)
    {
        Rect rect = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, title, EditorStyles.boldLabel);
        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    private static void DrawProperty(SerializedProperty property, string label, float x, float width, ref float y)
    {
        float height = EditorGUI.GetPropertyHeight(property, true);
        Rect rect = new Rect(x, y, width, height);
        EditorGUI.PropertyField(rect, property, new GUIContent(label), true);
        y += height + EditorGUIUtility.standardVerticalSpacing;
    }

    private static void DrawOverrideField(SerializedProperty overrideProperty, SerializedProperty valueProperty, string toggleLabel, float x, float width, ref float y, string valueLabel = null)
    {
        DrawProperty(overrideProperty, toggleLabel, x, width, ref y);
        if (!overrideProperty.boolValue)
        {
            return;
        }

        DrawProperty(valueProperty, valueLabel ?? ObjectNames.NicifyVariableName(valueProperty.name), x, width, ref y);
    }

    private static void DrawOverrideColorField(SerializedProperty overrideProperty, SerializedProperty colorProperty, string label, float x, float width, ref float y)
    {
        DrawProperty(overrideProperty, $"{label} Override", x, width, ref y);
        if (!overrideProperty.boolValue)
        {
            return;
        }

        DrawProperty(colorProperty, label, x, width, ref y);
    }

    private static float GetTextAreaHeight(SerializedProperty textProperty, float width)
    {
        float height = GetTextAreaStyle().CalcHeight(new GUIContent(textProperty.stringValue ?? string.Empty), Mathf.Max(160f, width));
        return Mathf.Clamp(height, MinTextHeight, MaxTextHeight);
    }

    private static string BuildSummary(SerializedProperty property)
    {
        int lineIndex = GetLineIndex(property) + 1;
        string speakerName = property.FindPropertyRelative("speakerName").stringValue;
        SerializedProperty presetProperty = property.FindPropertyRelative("speakerPreset");
        if (string.IsNullOrWhiteSpace(speakerName) && presetProperty.objectReferenceValue is SpeakerPreset speakerPreset)
        {
            speakerName = !string.IsNullOrWhiteSpace(speakerPreset.SpeakerDisplayName)
                ? speakerPreset.SpeakerDisplayName
                : speakerPreset.name;
        }

        if (string.IsNullOrWhiteSpace(speakerName))
        {
            speakerName = "No Speaker";
        }

        string summary = $"Line {lineIndex} - {speakerName.Trim()}";
        return summary.Length <= 36 ? summary : $"{summary.Substring(0, 33)}...";
    }

    private static int GetLineIndex(SerializedProperty property)
    {
        string path = property.propertyPath;
        int openBracketIndex = path.LastIndexOf('[');
        int closeBracketIndex = path.LastIndexOf(']');
        if (openBracketIndex < 0 || closeBracketIndex <= openBracketIndex)
        {
            return 0;
        }

        string indexText = path.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
        return int.TryParse(indexText, out int index) ? index : 0;
    }

    private static GUIStyle GetSummaryStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            clipping = TextClipping.Clip
        };

        return style;
    }

    private static GUIStyle GetTextAreaStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true
        };

        return style;
    }
}
