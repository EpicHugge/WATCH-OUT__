using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProgressionManager))]
public sealed class ProgressionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ProgressionManager manager = (ProgressionManager)target;
        DrawRuntimeStatus(manager);

        EditorGUILayout.Space();
        DrawDefaultInspector();

        DrawConfigurationWarnings(manager);
        DrawPlayModeControls(manager);

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawRuntimeStatus(ProgressionManager manager)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Day", manager.CurrentDay.ToString());
            EditorGUILayout.LabelField("Phase", manager.CurrentPhase.ToString());
            EditorGUILayout.LabelField("Step", manager.CurrentObjectiveStep.ToString());
            EditorGUILayout.LabelField("Objective", manager.CurrentObjectiveText);
            EditorGUILayout.LabelField("Generator Started", manager.GeneratorStartedToday ? "Yes" : "No");
            EditorGUILayout.LabelField("Power Out", manager.PowerOutTriggeredToday ? "Yes" : "No");
            EditorGUILayout.ObjectField("Day Data", manager.CurrentDayData, typeof(DayData), false);
            EditorGUILayout.ObjectField("Selected Cassette", manager.SelectedCassetteToday, typeof(CassetteData), false);
            EditorGUILayout.LabelField("Active Radio Events", BuildEventList(manager.ActiveRadioEventsToday));
            EditorGUILayout.LabelField("Resolved Radio Events", BuildEventList(manager.ResolvedRadioEventsToday));
        }
    }

    private static void DrawConfigurationWarnings(ProgressionManager manager)
    {
        List<string> warnings = manager.GetConfigurationWarnings();
        if (warnings.Count <= 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration Warnings", EditorStyles.boldLabel);

        for (int i = 0; i < warnings.Count; i++)
        {
            EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
        }
    }

    private static void DrawPlayModeControls(ProgressionManager manager)
    {
        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Quick Test Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use the progression test buttons.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Start Day"))
            {
                manager.StartDay();
            }

            if (GUILayout.Button("Complete Wake Up"))
            {
                manager.CompleteWakeUp();
            }

            if (GUILayout.Button("Advance To Next Day"))
            {
                manager.AdvanceToNextDay();
            }

            manager.DebugSetDay = EditorGUILayout.IntField("Set Current Day", manager.DebugSetDay);
            if (GUILayout.Button("Apply Current Day"))
            {
                manager.SetCurrentDay(manager.DebugSetDay);
            }

            manager.DebugSetPhase = (DayPhase)EditorGUILayout.EnumPopup("Set Phase", manager.DebugSetPhase);
            if (GUILayout.Button("Apply Phase"))
            {
                manager.SetPhase(manager.DebugSetPhase);
            }

            if (GUILayout.Button("Start Generator"))
            {
                manager.StartGenerator();
            }

            manager.DebugTestCassette = (CassetteData)EditorGUILayout.ObjectField(
                "Test Cassette",
                manager.DebugTestCassette,
                typeof(CassetteData),
                false);

            if (GUILayout.Button("Select Test Cassette"))
            {
                manager.SelectCassette(manager.DebugTestCassette);
            }

            if (GUILayout.Button("Begin Radio Scan"))
            {
                manager.BeginRadioScan();
            }

            manager.DebugTestFrequency = EditorGUILayout.FloatField("Test Frequency", manager.DebugTestFrequency);
            if (GUILayout.Button("Check Test Frequency"))
            {
                if (manager.TryGetMatchingRadioEvent(manager.DebugTestFrequency, out RadioEventData matchingEvent))
                {
                    Debug.Log(
                        $"ProgressionManager matched '{matchingEvent.EventName}' at {matchingEvent.TargetFrequency:F1} FM.",
                        manager);
                }
                else
                {
                    Debug.Log($"ProgressionManager found no event at {manager.DebugTestFrequency:F1} FM.", manager);
                }
            }

            if (GUILayout.Button("Trigger Power Out"))
            {
                manager.TriggerPowerOut();
            }

            if (GUILayout.Button("Print Current State"))
            {
                manager.PrintCurrentState();
            }
        }
    }

    private static string BuildEventList(IReadOnlyList<RadioEventData> radioEvents)
    {
        if (radioEvents == null || radioEvents.Count == 0)
        {
            return "None";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        for (int i = 0; i < radioEvents.Count; i++)
        {
            RadioEventData radioEvent = radioEvents[i];
            if (radioEvent == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(radioEvent.EventName);
        }

        return builder.Length > 0 ? builder.ToString() : "None";
    }
}
