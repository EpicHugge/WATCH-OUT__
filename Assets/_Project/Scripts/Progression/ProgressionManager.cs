using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProgressionManager : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private List<DayData> days = new List<DayData>();
    [SerializeField] [Min(1)] private int startingDay = 1;
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private bool completeWakeUpImmediatelyOnSceneStart = true;

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private bool playDayStartDialogueAutomatically = true;
    [SerializeField] private bool playDayEndDialogueOnPowerOut;

    [Header("Power Out")]
    [SerializeField] private PowerOutMode powerOutMode = PowerOutMode.Manual;
    [SerializeField] [Min(1)] private int radioEventsNeededBeforePowerOut = 1;
    [SerializeField] [Min(0f)] private float radioScanSecondsBeforePowerOut = 120f;
    [SerializeField] [Min(0.001f)] private float frequencyMatchTolerance = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool allowRadioDebugWithoutCassette = true;
    [SerializeField] private bool repeatSameRadioLoopEveryNightInTestBuild = true;
    [SerializeField] private bool powerDownGeneratorAndRadioAfterDialogueInTestBuild = true;
    [SerializeField] [Min(1)] private int debugSetDay = 1;
    [SerializeField] private DayPhase debugSetPhase = DayPhase.NeedGenerator;
    [SerializeField] private CassetteData debugTestCassette;
    [SerializeField] [Min(0f)] private float debugTestFrequency = 94.3f;

    private readonly List<RadioEventData> activeRadioEventsToday = new List<RadioEventData>();
    private readonly List<RadioEventData> resolvedRadioEventsToday = new List<RadioEventData>();
    private readonly List<RadioEventData> completedOneTimeRadioEvents = new List<RadioEventData>();

    private int currentDay;
    private DayPhase currentPhase = DayPhase.WakingUp;
    private bool generatorStartedToday;
    private bool powerOutTriggeredToday;
    private CassetteData selectedCassetteToday;
    private float radioScanElapsedSeconds;

    public event Action StateChanged;

    public int CurrentDay => currentDay;
    public DayPhase CurrentPhase => currentPhase;
    public bool GeneratorStartedToday => generatorStartedToday;
    public bool PowerOutTriggeredToday => powerOutTriggeredToday;
    public CassetteData SelectedCassetteToday => selectedCassetteToday;
    public DayData CurrentDayData => GetDayData(currentDay);
    public float RadioScanElapsedSeconds => radioScanElapsedSeconds;
    public IReadOnlyList<RadioEventData> ActiveRadioEventsToday => activeRadioEventsToday;
    public IReadOnlyList<RadioEventData> ResolvedRadioEventsToday => resolvedRadioEventsToday;
    public IReadOnlyList<RadioEventData> CompletedOneTimeRadioEvents => completedOneTimeRadioEvents;
    public bool RepeatSameRadioLoopEveryNightInTestBuild => repeatSameRadioLoopEveryNightInTestBuild;
    public bool PowerDownGeneratorAndRadioAfterDialogueInTestBuild => powerDownGeneratorAndRadioAfterDialogueInTestBuild;
    public bool DeferPowerOutUntilDialogueEndsInTestBuild => powerDownGeneratorAndRadioAfterDialogueInTestBuild;
    public int DebugSetDay { get => debugSetDay; set => debugSetDay = Mathf.Max(1, value); }
    public DayPhase DebugSetPhase { get => debugSetPhase; set => debugSetPhase = value; }
    public CassetteData DebugTestCassette { get => debugTestCassette; set => debugTestCassette = value; }
    public float DebugTestFrequency { get => debugTestFrequency; set => debugTestFrequency = Mathf.Max(0f, value); }

    private void Awake()
    {
        currentDay = Mathf.Max(1, startingDay);
        debugSetDay = Mathf.Max(debugSetDay, startingDay);
        ResolveReferences();
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartDay();

            if (completeWakeUpImmediatelyOnSceneStart)
            {
                CompleteWakeUp();
            }
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || powerOutTriggeredToday || currentPhase != DayPhase.ScanningRadio)
        {
            return;
        }

        if (DeferPowerOutUntilDialogueEndsInTestBuild)
        {
            return;
        }

        if (powerOutMode != PowerOutMode.TimeBased)
        {
            return;
        }

        radioScanElapsedSeconds += Time.deltaTime;

        if (radioScanElapsedSeconds >= radioScanSecondsBeforePowerOut)
        {
            TriggerPowerOut();
        }
    }

    public void StartDay()
    {
        ResolveReferences();

        currentDay = Mathf.Max(1, currentDay <= 0 ? startingDay : currentDay);
        generatorStartedToday = false;
        powerOutTriggeredToday = false;
        selectedCassetteToday = null;
        radioScanElapsedSeconds = 0f;
        activeRadioEventsToday.Clear();
        resolvedRadioEventsToday.Clear();

        if (repeatSameRadioLoopEveryNightInTestBuild)
        {
            completedOneTimeRadioEvents.Clear();
        }

        SetPhaseInternal(DayPhase.WakingUp);

        if (playDayStartDialogueAutomatically)
        {
            TryPlayDialogue(CurrentDayData != null ? CurrentDayData.DayStartDialogue : null);
        }

        NotifyStateChanged();
    }

    public void CompleteWakeUp()
    {
        if (currentPhase == DayPhase.WakingUp)
        {
            SetPhaseInternal(DayPhase.NeedGenerator);
        }
    }

    public bool StartGenerator()
    {
        if (generatorStartedToday)
        {
            return false;
        }

        if (currentPhase == DayPhase.WakingUp)
        {
            CompleteWakeUp();
        }

        generatorStartedToday = true;
        SetPhaseInternal(DayPhase.NeedCassette);
        NotifyStateChanged();
        return true;
    }

    public bool SelectCassette(CassetteData cassette)
    {
        if (cassette == null)
        {
            Debug.LogWarning("ProgressionManager received a null cassette selection.", this);
            return false;
        }

        DayData dayData = CurrentDayData;
        if (dayData != null && !dayData.ContainsCassette(cassette))
        {
            Debug.LogWarning(
                $"Cassette '{cassette.CassetteName}' is not assigned to day {currentDay}. Selection was ignored.",
                this);
            return false;
        }

        selectedCassetteToday = cassette;
        RefreshActiveRadioEvents();
        SetPhaseInternal(DayPhase.NeedCassette);
        NotifyStateChanged();
        return true;
    }

    public bool BeginRadioScan()
    {
        if (currentPhase == DayPhase.ScanningRadio)
        {
            return true;
        }

        if (!generatorStartedToday)
        {
            Debug.LogWarning("Radio scan cannot begin before the generator has started.", this);
            return false;
        }

        if (selectedCassetteToday == null && !allowRadioDebugWithoutCassette)
        {
            Debug.LogWarning("Radio scan cannot begin before a cassette has been selected.", this);
            return false;
        }

        radioScanElapsedSeconds = 0f;
        RefreshActiveRadioEvents();
        SetPhaseInternal(DayPhase.ScanningRadio);
        NotifyStateChanged();
        return true;
    }

    public bool EnsureRadioScanActive()
    {
        if (powerOutTriggeredToday)
        {
            return false;
        }

        return currentPhase == DayPhase.ScanningRadio || BeginRadioScan();
    }

    public bool TryGetMatchingRadioEvent(float tunedFrequency, out RadioEventData matchingEvent)
    {
        if (TryGetClosestRadioEvent(tunedFrequency, false, out RadioEventData closestEvent, out float closestDistance) &&
            closestDistance <= frequencyMatchTolerance)
        {
            matchingEvent = closestEvent;
            return true;
        }

        matchingEvent = null;
        return false;
    }

    public bool TryGetClosestRadioEvent(
        float tunedFrequency,
        bool includeResolvedToday,
        out RadioEventData closestEvent,
        out float closestDistance)
    {
        closestEvent = null;
        closestDistance = float.MaxValue;

        DayData dayData = CurrentDayData;
        if (dayData == null || (!allowRadioDebugWithoutCassette && selectedCassetteToday == null))
        {
            return false;
        }

        IReadOnlyList<RadioEventData> candidateEvents = dayData.AvailableRadioEvents;
        for (int i = 0; i < candidateEvents.Count; i++)
        {
            RadioEventData radioEvent = candidateEvents[i];
            if (!IsRadioEventAvailableForCurrentState(radioEvent, includeResolvedToday))
            {
                continue;
            }

            float distance = Mathf.Abs(radioEvent.TargetFrequency - tunedFrequency);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEvent = radioEvent;
            }
        }

        return closestEvent != null;
    }

    public bool MarkRadioEventFound(RadioEventData radioEvent)
    {
        if (radioEvent == null)
        {
            return false;
        }

        if (resolvedRadioEventsToday.Contains(radioEvent))
        {
            return false;
        }

        RefreshActiveRadioEvents();
        if (!activeRadioEventsToday.Contains(radioEvent))
        {
            Debug.LogWarning(
                $"Radio event '{radioEvent.EventName}' is not currently valid for day {currentDay}.",
                this);
            return false;
        }

        resolvedRadioEventsToday.Add(radioEvent);

        if (radioEvent.OneTimeOnly && !completedOneTimeRadioEvents.Contains(radioEvent))
        {
            completedOneTimeRadioEvents.Add(radioEvent);
        }

        RefreshActiveRadioEvents();
        EvaluatePowerOutCondition();

        NotifyStateChanged();
        return true;
    }

    public bool TriggerPowerOut()
    {
        if (powerOutTriggeredToday)
        {
            return false;
        }

        powerOutTriggeredToday = true;
        SetPhaseInternal(DayPhase.PowerOut);

        if (playDayEndDialogueOnPowerOut)
        {
            TryPlayDialogue(CurrentDayData != null ? CurrentDayData.DayEndDialogue : null);
        }

        NotifyStateChanged();
        return true;
    }

    public void EndDay()
    {
        SetPhaseInternal(DayPhase.CanSleep);
        NotifyStateChanged();
    }

    public void CompleteDebugRadioLoopAfterDialogue()
    {
        TriggerPowerOut();
        EndDay();
    }

    public void AdvanceToNextDay()
    {
        currentDay = repeatSameRadioLoopEveryNightInTestBuild
            ? Mathf.Max(1, startingDay)
            : Mathf.Max(1, currentDay + 1);
        StartDay();
    }

    public void SetCurrentDay(int dayNumber)
    {
        currentDay = Mathf.Max(1, dayNumber);
        StartDay();
    }

    public void SetPhase(DayPhase phase)
    {
        SetPhaseInternal(phase);
        NotifyStateChanged();
    }

    public IReadOnlyList<CassetteData> GetAvailableCassettesForToday()
    {
        DayData dayData = CurrentDayData;
        return dayData != null ? dayData.AvailableCassettes : Array.Empty<CassetteData>();
    }

    public IReadOnlyList<RadioEventData> GetAvailableRadioEventsForToday()
    {
        RefreshActiveRadioEvents();
        return activeRadioEventsToday;
    }

    public List<string> GetConfigurationWarnings()
    {
        List<string> warnings = new List<string>();

        if (days == null || days.Count == 0)
        {
            warnings.Add("No DayData assets are assigned.");
            return warnings;
        }

        Dictionary<int, DayData> assignedDays = new Dictionary<int, DayData>();

        for (int i = 0; i < days.Count; i++)
        {
            DayData dayData = days[i];
            if (dayData == null)
            {
                warnings.Add($"Day slot {i + 1} is empty.");
                continue;
            }

            if (assignedDays.TryGetValue(dayData.DayNumber, out DayData existingDay))
            {
                warnings.Add(
                    $"Duplicate day number {dayData.DayNumber} between '{existingDay.name}' and '{dayData.name}'.");
            }
            else
            {
                assignedDays.Add(dayData.DayNumber, dayData);
            }
        }

        if (!assignedDays.ContainsKey(startingDay))
        {
            warnings.Add($"No DayData asset is assigned for Starting Day {startingDay}.");
        }

        return warnings;
    }

    public string GetStateSummary()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Day: {currentDay}");
        builder.AppendLine($"Phase: {currentPhase}");
        builder.AppendLine($"Generator Started: {generatorStartedToday}");
        builder.AppendLine($"Power Out Triggered: {powerOutTriggeredToday}");
        builder.AppendLine($"Selected Cassette: {(selectedCassetteToday != null ? selectedCassetteToday.CassetteName : "None")}");
        builder.AppendLine($"Current Day Asset: {(CurrentDayData != null ? CurrentDayData.name : "Missing")}");
        builder.AppendLine($"Active Radio Events: {BuildNameList(activeRadioEventsToday)}");
        builder.AppendLine($"Resolved Radio Events: {BuildNameList(resolvedRadioEventsToday)}");
        builder.AppendLine($"Completed One-Time Events: {BuildNameList(completedOneTimeRadioEvents)}");
        return builder.ToString().TrimEnd();
    }

    [ContextMenu("Print Current Progression State")]
    public void PrintCurrentState()
    {
        Debug.Log(GetStateSummary(), this);
    }

    private void ResolveReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }
    }

    private void RefreshActiveRadioEvents()
    {
        activeRadioEventsToday.Clear();

        DayData dayData = CurrentDayData;
        if (dayData == null || (!allowRadioDebugWithoutCassette && selectedCassetteToday == null))
        {
            return;
        }

        IReadOnlyList<RadioEventData> candidateEvents = dayData.AvailableRadioEvents;
        for (int i = 0; i < candidateEvents.Count; i++)
        {
            RadioEventData radioEvent = candidateEvents[i];
            if (!IsRadioEventAvailableForCurrentState(radioEvent, false))
            {
                continue;
            }

            activeRadioEventsToday.Add(radioEvent);
        }
    }

    private bool IsRadioEventAvailableForCurrentState(RadioEventData radioEvent, bool includeResolvedToday)
    {
        if (radioEvent == null)
        {
            return false;
        }

        bool isResolvedToday = resolvedRadioEventsToday.Contains(radioEvent);
        if (!includeResolvedToday && isResolvedToday)
        {
            return false;
        }

        if (!repeatSameRadioLoopEveryNightInTestBuild &&
            radioEvent.OneTimeOnly &&
            completedOneTimeRadioEvents.Contains(radioEvent) &&
            !isResolvedToday)
        {
            return false;
        }

        if (!radioEvent.IsAvailableOnDay(currentDay))
        {
            return false;
        }

        if (selectedCassetteToday == null)
        {
            return allowRadioDebugWithoutCassette;
        }

        return radioEvent.AllowsCassette(selectedCassetteToday);
    }

    private DayData GetDayData(int dayNumber)
    {
        int normalizedDay = Mathf.Max(1, dayNumber);

        for (int i = 0; i < days.Count; i++)
        {
            DayData dayData = days[i];
            if (dayData != null && dayData.DayNumber == normalizedDay)
            {
                return dayData;
            }
        }

        return null;
    }

    private void EvaluatePowerOutCondition()
    {
        if (powerOutTriggeredToday)
        {
            return;
        }

        if (DeferPowerOutUntilDialogueEndsInTestBuild)
        {
            return;
        }

        if (powerOutMode == PowerOutMode.RadioEventsFoundCount &&
            resolvedRadioEventsToday.Count >= Mathf.Max(1, radioEventsNeededBeforePowerOut))
        {
            TriggerPowerOut();
        }
    }

    private void SetPhaseInternal(DayPhase phase)
    {
        currentPhase = phase;
    }

    private bool TryPlayDialogue(DialogueConversation conversation)
    {
        if (conversation == null)
        {
            return false;
        }

        ResolveReferences();
        if (dialogueRunner == null)
        {
            Debug.LogWarning(
                $"ProgressionManager could not find a DialogueRunner to play '{conversation.name}'.",
                this);
            return false;
        }

        return dialogueRunner.StartConversation(conversation);
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private static string BuildNameList(IReadOnlyList<RadioEventData> radioEvents)
    {
        if (radioEvents == null || radioEvents.Count == 0)
        {
            return "None";
        }

        StringBuilder builder = new StringBuilder();

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

    private void OnValidate()
    {
        startingDay = Mathf.Max(1, startingDay);
        debugSetDay = Mathf.Max(1, debugSetDay);
        radioEventsNeededBeforePowerOut = Mathf.Max(1, radioEventsNeededBeforePowerOut);
        radioScanSecondsBeforePowerOut = Mathf.Max(0f, radioScanSecondsBeforePowerOut);
        frequencyMatchTolerance = Mathf.Max(0.001f, frequencyMatchTolerance);
        debugTestFrequency = Mathf.Max(0f, debugTestFrequency);
    }
}
