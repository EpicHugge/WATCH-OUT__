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
    [SerializeField] private HoverMoveInteractable hoverInteractable;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private GameObject visualToHide;

    [Header("Prompts")]
    [SerializeField] private string functionalPrompt = "Pick Cassette";
    [SerializeField] private string workInProgressPrompt = "Unavailable";
    [SerializeField] private string alreadyCarryingPrompt = "Already Carrying Cassette";

    private bool hasBeenPickedUp;
    private CassettePlayerReceiver subscribedCassettePlayerReceiver;

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
        RefreshSubscriptions();
        RefreshAvailabilityState();
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
    }

    public void HandleInteract()
    {
        if (hasBeenPickedUp || IsWorkInProgress || cassetteData == null)
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

        hoverInteractable.SetLocked(false);
        hoverInteractable.SetPrompt(ResolvePrompt());
        hoverInteractable.SetInteractionEnabled(cassetteData != null || IsWorkInProgress);
    }

    private string ResolvePrompt()
    {
        if (IsWorkInProgress)
        {
            return workInProgressPrompt;
        }

        if (cassetteData == null)
        {
            return string.Empty;
        }

        if (cassettePlayerReceiver != null && !cassettePlayerReceiver.CanSelectCassette(cassetteData))
        {
            return alreadyCarryingPrompt;
        }

        return functionalPrompt;
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
