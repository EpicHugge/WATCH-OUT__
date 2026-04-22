using UnityEngine;

[DisallowMultipleComponent]
public sealed class RadioDeskInteractable : InteractableBase
{
    [SerializeField] private RadioDeskOperationController radioDeskController;
    [SerializeField] private string prompt = "Operate Radio";

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
    }

    public override bool CanInteract(PlayerInteractionController interactor)
    {
        return base.CanInteract(interactor) &&
            radioDeskController != null &&
            !radioDeskController.IsInOperationMode;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        return CanInteract(interactor) ? prompt : string.Empty;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        ResolveReferences();
        radioDeskController?.TryEnterOperationMode(interactor);
    }

    private void ResolveReferences()
    {
        if (radioDeskController == null)
        {
            radioDeskController = GetComponent<RadioDeskOperationController>();
        }

        if (radioDeskController == null)
        {
            radioDeskController = GetComponentInParent<RadioDeskOperationController>();
        }
    }
}
