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
    [SerializeField] private string workInProgressPrompt = "WIP";

    private bool hasBeenPickedUp;

    public bool IsWorkInProgress => slotMode == SlotMode.WorkInProgress;
    public CassetteData CassetteData => cassetteData;

    private void Awake()
    {
        ResolveReferences();
        ApplyPrompt();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyPrompt();
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
    }

    private void ApplyPrompt()
    {
        if (hoverInteractable == null)
        {
            return;
        }

        hoverInteractable.SetPrompt(IsWorkInProgress ? workInProgressPrompt : functionalPrompt);
    }
}
