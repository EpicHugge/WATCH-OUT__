using UnityEngine;
using UnityEngine.Events;

public sealed class DoorInteractable : InteractableBase
{
    [Header("Door")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Vector3 openLocalEulerAngles = new Vector3(0f, 95f, 0f);
    [SerializeField] private float openSpeed = 6f;
    [SerializeField] private bool startsOpen;
    [SerializeField] private string openPrompt = "Open Door";
    [SerializeField] private string closePrompt = "Close Door";

    [Header("Events")]
    [SerializeField] private UnityEvent onOpened;
    [SerializeField] private UnityEvent onClosed;

    private Quaternion closedLocalRotation;
    private Quaternion openLocalRotation;
    private bool isOpen;

    protected override void Awake()
    {
        base.Awake();

        if (doorPivot == null)
        {
            doorPivot = transform;
        }

        closedLocalRotation = doorPivot.localRotation;
        openLocalRotation = closedLocalRotation * Quaternion.Euler(openLocalEulerAngles);
        isOpen = startsOpen;
        doorPivot.localRotation = isOpen ? openLocalRotation : closedLocalRotation;
    }

    private void Update()
    {
        if (doorPivot == null)
        {
            return;
        }

        Quaternion targetRotation = isOpen ? openLocalRotation : closedLocalRotation;
        doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, targetRotation, Time.deltaTime * openSpeed);
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        return isOpen ? closePrompt : openPrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            onOpened?.Invoke();
        }
        else
        {
            onClosed?.Invoke();
        }
    }
}
