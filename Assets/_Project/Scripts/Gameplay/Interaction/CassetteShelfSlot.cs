using UnityEngine;

[DisallowMultipleComponent]
public sealed class CassetteShelfSlot : MonoBehaviour
{
    private enum SlotMode
    {
        Functional,
        WorkInProgress
    }

    [Header("Setup")]
    [SerializeField] private SlotMode slotMode = SlotMode.Functional;
    [SerializeField] private CassetteData cassetteData;
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private HoverMoveInteractable hoverInteractable;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private GameObject visualToHide;

    [Header("Prompts")]
    [SerializeField] private string functionalPrompt = "Pick Cassette";
    [SerializeField] private string workInProgressPrompt = "WIP";

    private bool hasBeenPickedUp;

    public bool IsWorkInProgress => slotMode == SlotMode.WorkInProgress;
    public CassetteData CassetteData => cassetteData;

    private void Awake()
    {
        ResolveReferences();
        ApplyPrompt();
        RefreshAvailabilityState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (progressionManager != null)
        {
            progressionManager.StateChanged += RefreshAvailabilityState;
        }

        RefreshAvailabilityState();
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.StateChanged -= RefreshAvailabilityState;
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyPrompt();
        RefreshAvailabilityState();
    }

    public void HandleInteract()
    {
        if (hasBeenPickedUp || IsWorkInProgress)
        {
            return;
        }

        ResolveReferences();
        if (cassetteData == null || cassettePlayerReceiver == null)
        {
            Debug.LogWarning("CassetteShelfSlot is missing a CassetteData or CassettePlayerReceiver reference.", this);
            return;
        }

        if (!cassettePlayerReceiver.TrySelectCassette(cassetteData))
        {
            return;
        }

        hasBeenPickedUp = true;

        if (hoverInteractable != null)
        {
            hoverInteractable.SetInteractionEnabled(false);
        }

        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
        }

        GameObject targetVisual = visualToHide != null ? visualToHide : gameObject;
        targetVisual.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (hoverInteractable == null)
        {
            hoverInteractable = GetComponent<HoverMoveInteractable>();
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider>();
        }

        if (visualToHide == null)
        {
            visualToHide = gameObject;
        }

        if (cassettePlayerReceiver == null && Application.isPlaying)
        {
            cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
        }

        if (progressionManager == null && Application.isPlaying)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }
    }

    private void ApplyPrompt()
    {
        if (hoverInteractable == null)
        {
            return;
        }

        hoverInteractable.SetPrompt(IsWorkInProgress ? workInProgressPrompt : functionalPrompt);
    }

    private void RefreshAvailabilityState()
    {
        if (hoverInteractable == null)
        {
            return;
        }

        if (hasBeenPickedUp)
        {
            hoverInteractable.SetInteractionEnabled(false);
            return;
        }

        if (IsWorkInProgress)
        {
            hoverInteractable.SetInteractionEnabled(true);
            hoverInteractable.SetLocked(false);
            hoverInteractable.SetPrompt(workInProgressPrompt);
            return;
        }

        if (progressionManager == null)
        {
            hoverInteractable.SetInteractionEnabled(true);
            hoverInteractable.SetLocked(false);
            hoverInteractable.SetPrompt(functionalPrompt);
            return;
        }

        bool canChooseCassette = progressionManager.CanSelectCassette(cassetteData);
        hoverInteractable.SetPrompt(functionalPrompt);
        hoverInteractable.SetLocked(false);
        hoverInteractable.SetInteractionEnabled(canChooseCassette || progressionManager.UnlockAllInteractionsForDebug);
    }
}
