using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrapdoorLadderInteractable : InteractableBase
{
    [Header("Ladder")]
    [SerializeField] private TrapdoorLadderSetup ladderSetup;
    [SerializeField] private TrapdoorLadderDirection direction = TrapdoorLadderDirection.Down;
    [SerializeField] private TrapdoorLadderTransitionController transitionController;
    [SerializeField] private string climbDownPrompt = "Climb Down";
    [SerializeField] private string climbUpPrompt = "Climb Up";

    public override bool CanInteract(PlayerInteractionController interactor)
    {
        if (!base.CanInteract(interactor) || ladderSetup == null || !ladderSetup.LadderAvailable)
        {
            return false;
        }

        if (ladderSetup.GetDestination(direction) == null)
        {
            return false;
        }

        TrapdoorLadderTransitionController controller = ResolveTransitionController(interactor);
        return controller == null || !controller.IsTransitionRunning;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        return direction == TrapdoorLadderDirection.Down ? climbDownPrompt : climbUpPrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        TrapdoorLadderTransitionController controller = ResolveTransitionController(interactor);
        if (controller == null)
        {
            Debug.LogWarning($"TrapdoorLadderInteractable on {name} could not find or create a TrapdoorLadderTransitionController.");
            return;
        }

        Transform destination = ladderSetup != null ? ladderSetup.GetDestination(direction) : null;
        if (!controller.TryStartTransition(destination))
        {
            return;
        }
    }

    private TrapdoorLadderTransitionController ResolveTransitionController(PlayerInteractionController interactor)
    {
        if (transitionController != null)
        {
            return transitionController;
        }

        if (interactor == null)
        {
            return null;
        }

        TrapdoorLadderTransitionController controller = interactor.GetComponent<TrapdoorLadderTransitionController>();
        if (controller == null)
        {
            controller = interactor.gameObject.AddComponent<TrapdoorLadderTransitionController>();
        }

        return controller;
    }
}

public enum TrapdoorLadderDirection
{
    Down,
    Up
}
