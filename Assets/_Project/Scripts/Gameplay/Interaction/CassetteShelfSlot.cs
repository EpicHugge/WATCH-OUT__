using UnityEngine;

[DisallowMultipleComponent]
public sealed class CassetteShelfSlot : MonoBehaviour
{
    public enum SlotMode
    {
        Normal,
        WorkInProgress,
        Locked,
        Hidden
    }

    [Header("Setup")]
    [SerializeField] private SlotMode slotMode = SlotMode.Normal;
    [SerializeField] private CassetteData cassetteData;
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private HoverMoveInteractable hoverInteractable;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private GameObject visualToHide;

    [Header("Prompts")]
    [SerializeField] private string functionalPrompt = "Pick Cassette";
    [SerializeField] private string workInProgressPrompt = "Unavailable";
    [SerializeField] private string alreadyCarryingPrompt = "Already Carrying Cassette";

    private bool hasBeenPickedUp;
    private CassettePlayerReceiver subscribedCassettePlayerReceiver;

    [SerializeField] private string lockedPrompt = "Locked";
    [SerializeField] private string overrideDisplayName = string.Empty;
    [SerializeField] private string overrideInteractionText = string.Empty;

    public bool IsWorkInProgress => slotMode == SlotMode.WorkInProgress;
    public bool IsLockedState => slotMode == SlotMode.Locked;
    public bool IsHiddenState => slotMode == SlotMode.Hidden;
    public CassetteData CassetteData => cassetteData;

    private CassetteTapeLabelDisplay labelDisplay;

    private void Awake()
    {
        ResolveReferences();
        ApplyPrompt();
        RefreshAvailabilityState();
        RefreshLabelDisplay();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshSubscriptions();
        RefreshAvailabilityState();
        RefreshLabelDisplay();
    }

    private void OnDisable()
    {
        RefreshSubscriptions(clearOnly: true);
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyPrompt();
        RefreshAvailabilityState();
        RefreshLabelDisplay();
    }

    public void HandleInteract()
    {
        if (hasBeenPickedUp || IsWorkInProgress || IsLockedState || cassetteData == null)
        {
            return;
        }

        ResolveReferences();
        if (cassettePlayerReceiver == null || !cassettePlayerReceiver.TrySelectCassette(cassetteData))
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

        SetVisualVisible(false);
    }

    private void ResolveReferences()
    {
        if (hoverInteractable == null)
        {
            hoverInteractable = GetComponent<HoverMoveInteractable>();
        }

        if (labelDisplay == null)
        {
            labelDisplay = GetComponent<CassetteTapeLabelDisplay>();
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider>();
        }

        if (visualToHide == null)
        {
            Transform visualTransform = hoverInteractable != null ? hoverInteractable.ObjectToMove : null;
            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }

            visualToHide = visualTransform != null && visualTransform.gameObject != gameObject
                ? visualTransform.gameObject
                : gameObject;
        }

        if (cassettePlayerReceiver == null && Application.isPlaying)
        {
            cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
        }

        RefreshSubscriptions();
    }

    private void ApplyPrompt()
    {
        if (hoverInteractable == null)
        {
            return;
        }

        hoverInteractable.SetPrompt(ResolvePrompt());
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

        hoverInteractable.SetPrompt(ResolvePrompt());
        hoverInteractable.SetInteractionEnabled(cassetteData != null || IsWorkInProgress || IsLockedState);
    }

    private string ResolvePrompt()
    {
        if (IsHiddenState)
        {
            return string.Empty;
        }

        if (IsWorkInProgress)
        {
            return string.IsNullOrWhiteSpace(overrideInteractionText) ? workInProgressPrompt : overrideInteractionText.Trim();
        }

        if (IsLockedState)
        {
            return string.IsNullOrWhiteSpace(overrideInteractionText) ? lockedPrompt : overrideInteractionText.Trim();
        }

        if (cassetteData == null)
        {
            return string.IsNullOrWhiteSpace(overrideInteractionText) ? string.Empty : overrideInteractionText.Trim();
        }

        if (cassettePlayerReceiver != null && !cassettePlayerReceiver.CanSelectCassette(cassetteData))
        {
            return alreadyCarryingPrompt;
        }

        return string.IsNullOrWhiteSpace(overrideInteractionText) ? functionalPrompt : overrideInteractionText.Trim();
    }

    private void HandleCassetteLoaded(CassetteData cassette)
    {
        RefreshAvailabilityState();
    }

    private void HandleCassetteReleased(CassetteData cassette)
    {
        if (hasBeenPickedUp && cassetteData != null && cassette == cassetteData)
        {
            hasBeenPickedUp = false;

            if (interactionCollider != null)
            {
                interactionCollider.enabled = true;
            }

            SetVisualVisible(true);
        }

        RefreshAvailabilityState();
    }

    public void ApplyShelfDefinition(
        CassetteData cassette,
        SlotMode mode,
        string displayNameOverride,
        string interactionTextOverride,
        CassettePlayerReceiver receiver)
    {
        cassetteData = cassette;
        slotMode = mode;
        overrideDisplayName = displayNameOverride ?? string.Empty;
        overrideInteractionText = interactionTextOverride ?? string.Empty;
        cassettePlayerReceiver = receiver;
        hasBeenPickedUp = false;

        ResolveReferences();
        ApplyPrompt();
        RefreshAvailabilityState();
        SetVisualVisible(!IsHiddenState);
        RefreshLabelDisplay();

        if (interactionCollider != null)
        {
            interactionCollider.enabled = !IsHiddenState;
        }
    }

    public string ResolveDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(overrideDisplayName))
        {
            return overrideDisplayName.Trim();
        }

        return cassetteData != null ? cassetteData.CassetteName : string.Empty;
    }

    private void RefreshLabelDisplay()
    {
        if (labelDisplay != null)
        {
            labelDisplay.RefreshLabel();
        }
    }

    private void SetVisualVisible(bool isVisible)
    {
        GameObject targetVisual = visualToHide != null ? visualToHide : gameObject;
        if (targetVisual != gameObject)
        {
            targetVisual.SetActive(isVisible);
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = isVisible;
            }
        }
    }

    private void RefreshSubscriptions(bool clearOnly = false)
    {
        if (subscribedCassettePlayerReceiver != null)
        {
            subscribedCassettePlayerReceiver.CassetteLoaded -= HandleCassetteLoaded;
            subscribedCassettePlayerReceiver.CassetteReleased -= HandleCassetteReleased;
            subscribedCassettePlayerReceiver = null;
        }

        if (clearOnly || !isActiveAndEnabled || cassettePlayerReceiver == null)
        {
            return;
        }

        cassettePlayerReceiver.CassetteLoaded += HandleCassetteLoaded;
        cassettePlayerReceiver.CassetteReleased += HandleCassetteReleased;
        subscribedCassettePlayerReceiver = cassettePlayerReceiver;
    }
}
