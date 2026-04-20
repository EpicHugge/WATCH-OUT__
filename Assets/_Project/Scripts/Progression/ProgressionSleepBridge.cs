using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProgressionSleepBridge : MonoBehaviour
{
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private SleepController sleepController;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (sleepController == null)
        {
            return;
        }

        sleepController.SleepSequenceStarted += HandleSleepSequenceStarted;
        sleepController.DayAdvanced += HandleDayAdvanced;
        sleepController.SleepSequenceFinished += HandleSleepSequenceFinished;
    }

    private void OnDisable()
    {
        if (sleepController == null)
        {
            return;
        }

        sleepController.SleepSequenceStarted -= HandleSleepSequenceStarted;
        sleepController.DayAdvanced -= HandleDayAdvanced;
        sleepController.SleepSequenceFinished -= HandleSleepSequenceFinished;
    }

    private void ResolveReferences()
    {
        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }

        if (sleepController == null)
        {
            sleepController = GetComponent<SleepController>();
        }

        if (sleepController == null)
        {
            sleepController = gameObject.AddComponent<SleepController>();
        }
    }

    private void HandleSleepSequenceStarted()
    {
        progressionManager?.EndDay();
    }

    private void HandleDayAdvanced()
    {
        progressionManager?.AdvanceToNextDay();
    }

    private void HandleSleepSequenceFinished()
    {
        progressionManager?.CompleteWakeUp();
    }
}
