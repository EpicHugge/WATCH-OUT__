using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class ProgressionManager : MonoBehaviour
{
    private const int FinalJamDay = 2;

    [Header("Content")]
    [SerializeField] private List<DayData> days = new List<DayData>();
    [SerializeField] [Min(1)] private int startingDay = 1;
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private bool completeWakeUpImmediatelyOnSceneStart = true;
    [SerializeField] private CassetteData cassette1;
    [SerializeField] private CassetteData cassette2;
    [SerializeField] private RadioEventData day1Event1;
    [SerializeField] private RadioEventData day1Event2;
    [SerializeField] private RadioEventData day2Event1;
    [SerializeField] private RadioEventData day2Event2;

    [Header("Scene References")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private GeneratorInteractable generatorInteractable;
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private SleepBedInteractable sleepBedInteractable;

    [Header("Radio")]
    [SerializeField] [Min(0.001f)] private float frequencyMatchTolerance = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool unlockAllInteractionsForDebug;
    [SerializeField] private bool enableDebugInstantPowerKey = true;
    [SerializeField] private KeyCode debugInstantPowerKey = KeyCode.F8;
    [SerializeField] private GeneratorInteractable debugGeneratorInteractable;
    [SerializeField] [Min(1)] private int debugSetDay = 1;
    [SerializeField] private DayPhase debugSetPhase = DayPhase.NeedGenerator;
    [SerializeField] private CassetteData debugTestCassette;
    [SerializeField] [Min(0f)] private float debugTestFrequency = 94.3f;

    private readonly List<RadioEventData> activeRadioEventsToday = new List<RadioEventData>();
    private readonly List<RadioEventData> resolvedRadioEventsToday = new List<RadioEventData>();
    private readonly List<CassetteData> availableCassettesToday = new List<CassetteData>();

    private int currentDay;
    private DayPhase currentPhase = DayPhase.WakingUp;
    private GameJamObjectiveStep currentObjectiveStep = GameJamObjectiveStep.WakeUp;
    private bool generatorStartedToday;
    private bool powerOutTriggeredToday;
    private CassetteData selectedCassetteToday;
    private bool cassettePlaybackStartedToday;
    private bool gameJamCompleted;
    private float radioScanElapsedSeconds;

    public event Action StateChanged;

    public int CurrentDay => currentDay;
    public DayPhase CurrentPhase => currentPhase;
    public GameJamObjectiveStep CurrentObjectiveStep => currentObjectiveStep;
    public bool GeneratorStartedToday => generatorStartedToday;
    public bool PowerOutTriggeredToday => powerOutTriggeredToday;
    public CassetteData SelectedCassetteToday => selectedCassetteToday;
    public bool CassettePlaybackStartedToday => cassettePlaybackStartedToday;
    public bool IsGameComplete => gameJamCompleted;
    public DayData CurrentDayData => GetDayData(currentDay);
    public float RadioScanElapsedSeconds => radioScanElapsedSeconds;
    public IReadOnlyList<RadioEventData> ActiveRadioEventsToday => activeRadioEventsToday;
    public IReadOnlyList<RadioEventData> ResolvedRadioEventsToday => resolvedRadioEventsToday;
    public bool UnlockAllInteractionsForDebug => unlockAllInteractionsForDebug;
    public bool CanUseRadioControls =>
        unlockAllInteractionsForDebug ||
        !gameJamCompleted &&
        !powerOutTriggeredToday &&
        generatorStartedToday &&
        selectedCassetteToday != null &&
        cassettePlaybackStartedToday &&
        (currentObjectiveStep == GameJamObjectiveStep.FindFirstSignal ||
         currentObjectiveStep == GameJamObjectiveStep.FindSecondSignal);
    public bool CanInteractWithGenerator =>
        unlockAllInteractionsForDebug ||
        !gameJamCompleted &&
        !powerOutTriggeredToday &&
        currentObjectiveStep == GameJamObjectiveStep.StartGenerator &&
        !generatorStartedToday;
    public bool CanSleepNow =>
        !gameJamCompleted &&
        currentObjectiveStep == GameJamObjectiveStep.ReturnToBed &&
        powerOutTriggeredToday;
    public string CurrentObjectiveText => BuildObjectiveText();
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
        HandleDebugInput();

        if (!Application.isPlaying || powerOutTriggeredToday || currentPhase != DayPhase.ScanningRadio)
        {
            return;
        }
    }

    public void StartDay()
    {
        ResolveReferences();

        currentDay = Mathf.Clamp(currentDay <= 0 ? startingDay : currentDay, 1, FinalJamDay);
        gameJamCompleted = false;
        generatorStartedToday = false;
        powerOutTriggeredToday = false;
        selectedCassetteToday = null;
        cassettePlaybackStartedToday = false;
        radioScanElapsedSeconds = 0f;
        availableCassettesToday.Clear();
        activeRadioEventsToday.Clear();
        resolvedRadioEventsToday.Clear();
        RefreshAvailableCassettes();
        SetObjectiveStepInternal(GameJamObjectiveStep.WakeUp);
        NotifyStateChanged();
    }

    public void CompleteWakeUp()
    {
        if (currentObjectiveStep == GameJamObjectiveStep.WakeUp && !gameJamCompleted)
        {
            SetObjectiveStepInternal(GameJamObjectiveStep.StartGenerator);
            NotifyStateChanged();
        }
    }

    public bool StartGenerator()
    {
        if (generatorStartedToday || !CanInteractWithGenerator)
        {
            return false;
        }

        generatorStartedToday = true;
        SetObjectiveStepInternal(GameJamObjectiveStep.PickCassette);
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

        if (!CanSelectCassette(cassette))
        {
            return false;
        }

        selectedCassetteToday = cassette;
        cassettePlaybackStartedToday = false;
        SetObjectiveStepInternal(GameJamObjectiveStep.PlayCassette);
        NotifyStateChanged();
        return true;
    }

    public bool MarkCassettePlaybackStarted(CassetteData cassette)
    {
        if (!CanPlayCassette(cassette))
        {
            return false;
        }

        cassettePlaybackStartedToday = true;
        SetObjectiveStepInternal(GameJamObjectiveStep.FindFirstSignal);
        NotifyStateChanged();
        return true;
    }

    public bool BeginRadioScan()
    {
        if (currentPhase == DayPhase.ScanningRadio && CanUseRadioControls)
        {
            return true;
        }

        if (!CanUseRadioControls)
        {
            return false;
        }

        radioScanElapsedSeconds = 0f;
        NotifyStateChanged();
        return true;
    }

    public bool EnsureRadioScanActive()
    {
        if (powerOutTriggeredToday)
        {
            return false;
        }

        return CanUseRadioControls && (currentPhase == DayPhase.ScanningRadio || BeginRadioScan());
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

        RadioEventData currentTarget = GetCurrentTargetRadioEvent();
        if (currentTarget == null)
        {
            return false;
        }

        if (!includeResolvedToday && resolvedRadioEventsToday.Contains(currentTarget))
        {
            return false;
        }

        closestEvent = currentTarget;
        closestDistance = Mathf.Abs(currentTarget.TargetFrequency - tunedFrequency);
        return true;
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

        RadioEventData currentTarget = GetCurrentTargetRadioEvent();
        if (!unlockAllInteractionsForDebug && currentTarget != radioEvent)
        {
            return false;
        }

        resolvedRadioEventsToday.Add(radioEvent);
        if (currentObjectiveStep == GameJamObjectiveStep.FindFirstSignal)
        {
            SetObjectiveStepInternal(GameJamObjectiveStep.FindSecondSignal);
        }
        else if (currentObjectiveStep == GameJamObjectiveStep.FindSecondSignal)
        {
            TriggerPowerOut();
            EndDay();
            return true;
        }

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
        if (generatorInteractable != null && generatorInteractable.IsOn)
        {
            generatorInteractable.SetPowerState(false);
        }

        SetObjectiveStepInternal(GameJamObjectiveStep.PowerOut);
        NotifyStateChanged();
        return true;
    }

    public void EndDay()
    {
        if (gameJamCompleted)
        {
            return;
        }

        SetObjectiveStepInternal(GameJamObjectiveStep.ReturnToBed);
        NotifyStateChanged();
    }

    public void DebugForceStartPower()
    {
        ResolveReferences();

        if (debugGeneratorInteractable != null)
        {
            debugGeneratorInteractable.SetPowerState(true);
        }
        else
        {
            GeneratorInteractable[] generators = FindObjectsByType<GeneratorInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < generators.Length; i++)
            {
                generators[i].SetPowerState(true);
            }
        }

        StartGenerator();
    }

    public void AdvanceToNextDay()
    {
        if (currentDay >= FinalJamDay)
        {
            CompleteGameJamFlow();
            return;
        }

        currentDay = Mathf.Clamp(currentDay + 1, 1, FinalJamDay);
        StartDay();
    }

    public void SetCurrentDay(int dayNumber)
    {
        currentDay = Mathf.Max(1, dayNumber);
        StartDay();
    }

    public void SetPhase(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.WakingUp:
                SetObjectiveStepInternal(GameJamObjectiveStep.WakeUp);
                break;
            case DayPhase.NeedGenerator:
                SetObjectiveStepInternal(GameJamObjectiveStep.StartGenerator);
                break;
            case DayPhase.NeedCassette:
                SetObjectiveStepInternal(selectedCassetteToday == null
                    ? GameJamObjectiveStep.PickCassette
                    : GameJamObjectiveStep.PlayCassette);
                break;
            case DayPhase.ScanningRadio:
                SetObjectiveStepInternal(resolvedRadioEventsToday.Count == 0
                    ? GameJamObjectiveStep.FindFirstSignal
                    : GameJamObjectiveStep.FindSecondSignal);
                break;
            case DayPhase.PowerOut:
                SetObjectiveStepInternal(GameJamObjectiveStep.PowerOut);
                break;
            case DayPhase.CanSleep:
                SetObjectiveStepInternal(GameJamObjectiveStep.ReturnToBed);
                break;
            default:
                SetPhaseInternal(phase);
                break;
        }

        NotifyStateChanged();
    }

    public IReadOnlyList<CassetteData> GetAvailableCassettesForToday()
    {
        return availableCassettesToday;
    }

    public IReadOnlyList<RadioEventData> GetAvailableRadioEventsForToday()
    {
        RefreshActiveRadioEvents();
        return activeRadioEventsToday;
    }

    public List<string> GetConfigurationWarnings()
    {
        List<string> warnings = new List<string>();

        if (days == null)
        {
            days = new List<DayData>();
        }

        if (days.Count == 0)
        {
            warnings.Add("No DayData assets are assigned. The jam flow can still run from explicit manager references.");
        }

        if (cassette1 == null || cassette2 == null)
        {
            warnings.Add("Assign both Cassette 1 and Cassette 2.");
        }

        if (day1Event1 == null || day1Event2 == null || day2Event1 == null || day2Event2 == null)
        {
            warnings.Add("Assign Day 1 Event 1/2 and Day 2 Event 1/2.");
        }

        if (day1Event1 != null && day1Event2 != null &&
            Mathf.Approximately(day1Event1.TargetFrequency, day1Event2.TargetFrequency))
        {
            warnings.Add("Day 1 Event 1 and Event 2 must use different frequencies.");
        }

        if (day2Event1 != null && day2Event2 != null &&
            Mathf.Approximately(day2Event1.TargetFrequency, day2Event2.TargetFrequency))
        {
            warnings.Add("Day 2 Event 1 and Event 2 must use different frequencies.");
        }

        if (generatorInteractable == null)
        {
            warnings.Add("Generator reference is missing.");
        }

        if (sleepBedInteractable == null)
        {
            warnings.Add("Bed reference is missing.");
        }

        if (cassettePlayerReceiver == null)
        {
            warnings.Add("Cassette player reference is missing.");
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
        builder.AppendLine($"Step: {currentObjectiveStep}");
        builder.AppendLine($"Objective: {CurrentObjectiveText}");
        builder.AppendLine($"Generator Started: {generatorStartedToday}");
        builder.AppendLine($"Power Out Triggered: {powerOutTriggeredToday}");
        builder.AppendLine($"Selected Cassette: {(selectedCassetteToday != null ? selectedCassetteToday.CassetteName : "None")}");
        builder.AppendLine($"Cassette Playback Completed: {(cassettePlaybackStartedToday ? "Yes" : "No")}");
        builder.AppendLine($"Current Day Asset: {(CurrentDayData != null ? CurrentDayData.name : "Missing")}");
        builder.AppendLine($"Active Radio Events: {BuildNameList(activeRadioEventsToday)}");
        builder.AppendLine($"Resolved Radio Events: {BuildNameList(resolvedRadioEventsToday)}");
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

        if (generatorInteractable == null)
        {
            generatorInteractable = FindAnyObjectByType<GeneratorInteractable>();
        }

        if (cassettePlayerReceiver == null)
        {
            cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
        }

        if (sleepBedInteractable == null)
        {
            sleepBedInteractable = FindAnyObjectByType<SleepBedInteractable>();
        }

        if (debugGeneratorInteractable == null)
        {
            debugGeneratorInteractable = generatorInteractable != null
                ? generatorInteractable
                : FindAnyObjectByType<GeneratorInteractable>();
        }
    }

    private void RefreshActiveRadioEvents()
    {
        activeRadioEventsToday.Clear();
        RadioEventData currentTarget = GetCurrentTargetRadioEvent();
        if (currentTarget != null)
        {
            activeRadioEventsToday.Add(currentTarget);
        }
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

    private void SetObjectiveStepInternal(GameJamObjectiveStep step)
    {
        currentObjectiveStep = step;
        RefreshAvailableCassettes();
        RefreshActiveRadioEvents();
        UpdatePhaseFromCurrentObjective();
        RefreshSleepAvailability();
    }

    private void SetPhaseInternal(DayPhase phase)
    {
        currentPhase = phase;
    }

    private void NotifyStateChanged()
    {
        RefreshActiveRadioEvents();
        StateChanged?.Invoke();
    }

    private void HandleDebugInput()
    {
        if (!Application.isPlaying || !WasDebugInstantPowerKeyPressed())
        {
            return;
        }

        DebugForceStartPower();
    }

    private bool WasDebugInstantPowerKeyPressed()
    {
        KeyCode effectiveDebugKey = debugInstantPowerKey == KeyCode.None
            ? KeyCode.F8
            : debugInstantPowerKey;

        if (enableDebugInstantPowerKey && Input.GetKeyDown(effectiveDebugKey))
        {
            return true;
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        if (keyboard.f8Key.wasPressedThisFrame)
        {
            return true;
        }

        if (!enableDebugInstantPowerKey)
        {
            return false;
        }

        return effectiveDebugKey switch
        {
            KeyCode.F1 => keyboard.f1Key.wasPressedThisFrame,
            KeyCode.F2 => keyboard.f2Key.wasPressedThisFrame,
            KeyCode.F3 => keyboard.f3Key.wasPressedThisFrame,
            KeyCode.F4 => keyboard.f4Key.wasPressedThisFrame,
            KeyCode.F5 => keyboard.f5Key.wasPressedThisFrame,
            KeyCode.F6 => keyboard.f6Key.wasPressedThisFrame,
            KeyCode.F7 => keyboard.f7Key.wasPressedThisFrame,
            KeyCode.F8 => keyboard.f8Key.wasPressedThisFrame,
            KeyCode.F9 => keyboard.f9Key.wasPressedThisFrame,
            KeyCode.F10 => keyboard.f10Key.wasPressedThisFrame,
            KeyCode.F11 => keyboard.f11Key.wasPressedThisFrame,
            KeyCode.F12 => keyboard.f12Key.wasPressedThisFrame,
            KeyCode.G => keyboard.gKey.wasPressedThisFrame,
            KeyCode.P => keyboard.pKey.wasPressedThisFrame,
            _ => false
        };
#else
        return false;
#endif
    }

    private void RefreshSleepAvailability()
    {
        ProgressionInteractablePhaseGate[] gates = FindObjectsByType<ProgressionInteractablePhaseGate>(FindObjectsSortMode.None);
        for (int i = 0; i < gates.Length; i++)
        {
            gates[i].RefreshLockState();
        }

        if (!powerOutTriggeredToday || gameJamCompleted)
        {
            return;
        }

        SleepBedInteractable[] sleepBeds = FindObjectsByType<SleepBedInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < sleepBeds.Length; i++)
        {
            sleepBeds[i].SetLocked(false);
        }
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
        frequencyMatchTolerance = Mathf.Max(0.001f, frequencyMatchTolerance);
        debugTestFrequency = Mathf.Max(0f, debugTestFrequency);
    }

    public bool CanSelectCassette(CassetteData cassette)
    {
        if (cassette == null)
        {
            return false;
        }

        if (unlockAllInteractionsForDebug)
        {
            return true;
        }

        return !gameJamCompleted &&
               currentObjectiveStep == GameJamObjectiveStep.PickCassette &&
               selectedCassetteToday == null &&
               availableCassettesToday.Contains(cassette);
    }

    public bool CanPlayCassette(CassetteData cassette)
    {
        if (cassette == null)
        {
            return false;
        }

        if (unlockAllInteractionsForDebug)
        {
            return true;
        }

        return !gameJamCompleted &&
               currentObjectiveStep == GameJamObjectiveStep.PlayCassette &&
               !cassettePlaybackStartedToday &&
               selectedCassetteToday == cassette;
    }

    public bool IsCurrentTargetRadioEvent(RadioEventData radioEvent)
    {
        return radioEvent != null && radioEvent == GetCurrentTargetRadioEvent();
    }

    private void RefreshAvailableCassettes()
    {
        availableCassettesToday.Clear();

        AddCassetteIfMissing(cassette1);
        AddCassetteIfMissing(cassette2);

        DayData dayData = CurrentDayData;
        if (dayData == null)
        {
            return;
        }

        IReadOnlyList<CassetteData> configuredCassettes = dayData.AvailableCassettes;
        for (int i = 0; i < configuredCassettes.Count; i++)
        {
            AddCassetteIfMissing(configuredCassettes[i]);
        }
    }

    private void AddCassetteIfMissing(CassetteData cassette)
    {
        if (cassette != null && !availableCassettesToday.Contains(cassette))
        {
            availableCassettesToday.Add(cassette);
        }
    }

    private RadioEventData GetCurrentTargetRadioEvent()
    {
        if (gameJamCompleted)
        {
            return null;
        }

        return currentObjectiveStep switch
        {
            GameJamObjectiveStep.FindFirstSignal => GetConfiguredDayEvent(currentDay, firstEvent: true),
            GameJamObjectiveStep.FindSecondSignal => GetConfiguredDayEvent(currentDay, firstEvent: false),
            _ => null
        };
    }

    private RadioEventData GetConfiguredDayEvent(int dayNumber, bool firstEvent)
    {
        RadioEventData configuredEvent = dayNumber switch
        {
            1 => firstEvent ? day1Event1 : day1Event2,
            2 => firstEvent ? day2Event1 : day2Event2,
            _ => null
        };

        if (configuredEvent != null)
        {
            return configuredEvent;
        }

        DayData dayData = GetDayData(dayNumber);
        if (dayData == null)
        {
            return null;
        }

        IReadOnlyList<RadioEventData> dayEvents = dayData.AvailableRadioEvents;
        int targetIndex = firstEvent ? 0 : 1;
        return dayEvents != null && dayEvents.Count > targetIndex ? dayEvents[targetIndex] : null;
    }

    private void UpdatePhaseFromCurrentObjective()
    {
        DayPhase mappedPhase = currentObjectiveStep switch
        {
            GameJamObjectiveStep.WakeUp => DayPhase.WakingUp,
            GameJamObjectiveStep.StartGenerator => DayPhase.NeedGenerator,
            GameJamObjectiveStep.PickCassette => DayPhase.NeedCassette,
            GameJamObjectiveStep.PlayCassette => DayPhase.NeedCassette,
            GameJamObjectiveStep.FindFirstSignal => DayPhase.ScanningRadio,
            GameJamObjectiveStep.FindSecondSignal => DayPhase.ScanningRadio,
            GameJamObjectiveStep.PowerOut => DayPhase.PowerOut,
            GameJamObjectiveStep.ReturnToBed => DayPhase.CanSleep,
            GameJamObjectiveStep.GameComplete => DayPhase.CanSleep,
            _ => currentPhase
        };

        SetPhaseInternal(mappedPhase);
    }

    private string BuildObjectiveText()
    {
        return currentObjectiveStep switch
        {
            GameJamObjectiveStep.WakeUp => $"Day {Mathf.Max(1, currentDay)}\nWake up",
            GameJamObjectiveStep.StartGenerator => $"Day {Mathf.Max(1, currentDay)}\nStart the generator",
            GameJamObjectiveStep.PickCassette => $"Day {Mathf.Max(1, currentDay)}\nPick a cassette",
            GameJamObjectiveStep.PlayCassette => $"Day {Mathf.Max(1, currentDay)}\nPlay the cassette",
            GameJamObjectiveStep.FindFirstSignal => $"Day {Mathf.Max(1, currentDay)}\nTune the radio",
            GameJamObjectiveStep.FindSecondSignal => $"Day {Mathf.Max(1, currentDay)}\nFind the second signal",
            GameJamObjectiveStep.PowerOut => $"Day {Mathf.Max(1, currentDay)}\nThe power is out",
            GameJamObjectiveStep.ReturnToBed => $"Day {Mathf.Max(1, currentDay)}\nReturn to bed",
            GameJamObjectiveStep.GameComplete => "Game Jam demo complete",
            _ => string.Empty
        };
    }

    private void CompleteGameJamFlow()
    {
        ResolveReferences();

        gameJamCompleted = true;
        generatorStartedToday = false;
        powerOutTriggeredToday = true;
        activeRadioEventsToday.Clear();
        SetObjectiveStepInternal(GameJamObjectiveStep.GameComplete);

        if (generatorInteractable != null && generatorInteractable.IsOn)
        {
            generatorInteractable.SetPowerState(false);
        }

        NotifyStateChanged();
    }
}
