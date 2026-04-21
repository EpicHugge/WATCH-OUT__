using UnityEngine;

[DisallowMultipleComponent]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private bool interactionEnabled = true;
    [SerializeField] private string interactionVerb = "Interact";
    [SerializeField] private string interactionDisplayName = string.Empty;

    public bool IsLocked => false;
    public bool InteractionEnabled => interactionEnabled;

    protected virtual void Awake()
    {
    }

    public virtual bool CanInteract(PlayerInteractionController interactor)
    {
        return interactionEnabled;
    }

    public virtual string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (!interactionEnabled)
        {
            return string.Empty;
        }

        return BuildPromptLabel(interactionVerb, interactionDisplayName);
    }

    public void Interact(PlayerInteractionController interactor)
    {
        if (!interactionEnabled)
        {
            return;
        }

        InteractInternal(interactor);
    }

    public void EndInteract(PlayerInteractionController interactor)
    {
        if (!interactionEnabled)
        {
            return;
        }

        EndInteractInternal(interactor);
    }

    public void OnHoverEnter(PlayerInteractionController interactor)
    {
        if (!interactionEnabled) return;
        OnHoverEnterInternal(interactor);
    }

    public void OnHoverExit(PlayerInteractionController interactor)
    {
        OnHoverExitInternal(interactor);
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        interactionEnabled = isEnabled;
    }

    public void SetLocked(bool locked)
    {
    }

    protected virtual void EndInteractInternal(PlayerInteractionController interactor)
    {
    }

    protected virtual void OnHoverEnterInternal(PlayerInteractionController interactor)
    {
    }

    protected virtual void OnHoverExitInternal(PlayerInteractionController interactor)
    {
    }

    protected abstract void InteractInternal(PlayerInteractionController interactor);

    protected static string BuildPromptLabel(string verb, string displayName)
    {
        bool hasVerb = !string.IsNullOrWhiteSpace(verb);
        bool hasName = !string.IsNullOrWhiteSpace(displayName);

        if (hasVerb && hasName)
        {
            return $"{verb} {displayName}";
        }

        if (hasVerb)
        {
            return verb;
        }

        if (hasName)
        {
            return displayName;
        }

        return string.Empty;
    }
}
