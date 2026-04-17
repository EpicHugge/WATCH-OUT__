using UnityEngine;
using WatchOut;

[DisallowMultipleComponent]
public sealed class RadioInteractable : InteractableBase
{
    [Header("Radio")]
    [SerializeField] private RadioSystem radioSystem;
    [SerializeField] private bool isIncreaseButton = true;
    [SerializeField] private string prompt = "Tune Up";

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        return prompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        if (radioSystem == null)
        {
            Debug.LogWarning("RadioSystem is missing on RadioInteractable!", this);
            return;
        }

        if (isIncreaseButton)
        {
            Debug.Log("Increasing Frequency Button Pressed!");
            radioSystem.IncreaseFrequency();
        }
        else
        {
            Debug.Log("Decreasing Frequency Button Pressed!");
            radioSystem.DecreaseFrequency();
        }
    }
}