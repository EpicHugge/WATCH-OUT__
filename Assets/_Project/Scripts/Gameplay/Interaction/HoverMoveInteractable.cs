using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class HoverMoveInteractable : InteractableBase
{
    [Header("Hover Movement")]
    [SerializeField] private Transform objectToMove;
    [SerializeField] private Vector3 hoverOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private float moveSpeed = 10f;
    
    [Header("Interaction")]
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private UnityEvent onInteract;

    private Vector3 originalPosition;
    private bool isHovered;

    protected override void Awake()
    {
        base.Awake();
        
        if (objectToMove == null)
        {
            objectToMove = transform;
        }
        
        originalPosition = objectToMove.localPosition;
    }

    private void Update()
    {
        if (objectToMove == null) return;

        Vector3 targetPosition = isHovered ? originalPosition + hoverOffset : originalPosition;
        objectToMove.localPosition = Vector3.Lerp(objectToMove.localPosition, targetPosition, Time.deltaTime * moveSpeed);
    }

    protected override void OnHoverEnterInternal(PlayerInteractionController interactor)
    {
        isHovered = true;
    }

    protected override void OnHoverExitInternal(PlayerInteractionController interactor)
    {
        isHovered = false;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked) return base.GetInteractionPrompt(interactor);
        return prompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        onInteract?.Invoke();
    }
}