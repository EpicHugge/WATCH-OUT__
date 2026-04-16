using UnityEngine;
using UnityEngine.Events;

public sealed class PickupInteractable : InteractableBase
{
    [Header("Pickup")]
    [SerializeField] private string pickupPrompt = "Pick Up Item";
    [SerializeField] private bool disableObjectOnPickup = true;
    [SerializeField] private GameObject objectToDisable;

    [Header("Events")]
    [SerializeField] private UnityEvent onPickedUp;

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        return pickupPrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        onPickedUp?.Invoke();

        if (disableObjectOnPickup)
        {
            GameObject target = objectToDisable != null ? objectToDisable : gameObject;
            target.SetActive(false);
        }
    }
}
