using UnityEngine;
using WatchOut;

[DisallowMultipleComponent]
public sealed class RadioInteractable : InteractableBase
{
    [Header("Radio")]
    [SerializeField] private RadioSystem radioSystem;
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private bool isIncreaseButton = true;
    [SerializeField] private bool isScanButton;
    [SerializeField] private string prompt = "Tune Up";

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
        RefreshLockState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (progressionManager != null)
        {
            progressionManager.StateChanged += RefreshLockState;
        }

        RefreshLockState();
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.StateChanged -= RefreshLockState;
        }
    }

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
        if (radioSystem == null)
        {
            return;
        }

        if (isScanButton)
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

        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }
    }

    private void RefreshLockState()
    {
        if (progressionManager == null)
        {
            return;
        }

        SetLocked(!progressionManager.CanUseRadioControls);
    }
}
