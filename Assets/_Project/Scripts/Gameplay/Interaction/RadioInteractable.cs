using UnityEngine;
using WatchOut;

[DisallowMultipleComponent]
public sealed class RadioInteractable : InteractableBase
{
    [Header("Radio")]
    [SerializeField] private RadioSystem radioSystem;
    [SerializeField] private bool isIncreaseButton = true;
    [SerializeField] private bool isScanButton;
    [SerializeField] private string prompt = "Tune Up";

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        return CanInteract(interactor) ? prompt : string.Empty;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        ResolveReferences();
        if (radioSystem == null)
        {
            return;
        }

        if (isScanButton)
        {
            radioSystem.ToggleScan();
            return;
        }

        if (isIncreaseButton)
        {
            radioSystem.SetIncreasing(true);
        }
        else
        {
            radioSystem.SetDecreasing(true);
        }
    }

    protected override void EndInteractInternal(PlayerInteractionController interactor)
    {
        if (radioSystem == null || isScanButton)
        {
            return;
        }

        if (isIncreaseButton)
        {
            radioSystem.SetIncreasing(false);
        }
        else
        {
            radioSystem.SetDecreasing(false);
        }
    }

    private void ResolveReferences()
    {
        if (radioSystem == null)
        {
            radioSystem = GetComponentInParent<RadioSystem>();

            if (radioSystem == null)
            {
                radioSystem = FindAnyObjectByType<RadioSystem>();
            }
        }
    }
}
