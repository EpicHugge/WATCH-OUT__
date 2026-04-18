using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class SleepBedInteractable : InteractableBase
{
    [Header("Bed")]
    [SerializeField] private Transform sleepPoint;
    [SerializeField] private Transform standUpPoint;
    [SerializeField] private SleepController sleepController;
    [SerializeField] private string sleepPrompt = "Sleep";

    [Header("Events")]
    [SerializeField] private UnityEvent onSleepStarted;
    [SerializeField] private UnityEvent onSleepFinished;

    private bool sleepInProgress;

    public Transform SleepPoint => sleepPoint;
    public Transform StandUpPoint => standUpPoint;
    public bool SleepInProgress => sleepInProgress;

    public override bool CanInteract(PlayerInteractionController interactor)
    {
        if (!base.CanInteract(interactor) || sleepPoint == null || sleepInProgress)
        {
            return false;
        }

        SleepController controller = ResolveSleepController(interactor);
        return controller == null || !controller.IsSequenceRunning;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        return sleepPrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        SleepController controller = ResolveSleepController(interactor);
        if (controller == null)
        {
            Debug.LogWarning($"SleepBedInteractable on {name} could not find or create a SleepController.");
            return;
        }

        if (!controller.TryStartSleepSequence(this))
        {
            return;
        }

        sleepInProgress = true;
        onSleepStarted?.Invoke();
    }

    public void NotifySleepSequenceFinished()
    {
        if (!sleepInProgress)
        {
            return;
        }

        sleepInProgress = false;
        onSleepFinished?.Invoke();
    }

    private SleepController ResolveSleepController(PlayerInteractionController interactor)
    {
        if (sleepController != null)
        {
            return sleepController;
        }

        if (interactor == null)
        {
            return null;
        }

        SleepController controller = interactor.GetComponent<SleepController>();
        if (controller == null)
        {
            controller = interactor.gameObject.AddComponent<SleepController>();
        }

        return controller;
    }
}
