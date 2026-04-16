using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private bool interactionEnabled = true;
    [SerializeField] private bool startsLocked;
    [SerializeField] private string interactionVerb = "Interact";
    [SerializeField] private string interactionDisplayName = string.Empty;
    [SerializeField] private string lockedPrompt = "Locked";

    [Header("Feedback")]
    [SerializeField] private bool autoCreateHighlight = true;
    [SerializeField] private InteractionOutlineHighlight interactionHighlight;
    [SerializeField] private UnityEvent onLockedInteract;

    private bool isLocked;

    public bool IsLocked => isLocked;
    public bool InteractionEnabled => interactionEnabled;

    protected virtual void Awake()
    {
        isLocked = startsLocked;

        if (autoCreateHighlight && interactionHighlight == null)
        {
            interactionHighlight = GetComponent<InteractionOutlineHighlight>();

            if (interactionHighlight == null)
            {
                interactionHighlight = gameObject.AddComponent<InteractionOutlineHighlight>();
            }
        }
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

        if (isLocked)
        {
            return string.IsNullOrWhiteSpace(lockedPrompt) ? "Locked" : lockedPrompt;
        }

        return BuildPromptLabel(interactionVerb, interactionDisplayName);
    }

    public void Interact(PlayerInteractionController interactor)
    {
        if (!interactionEnabled)
        {
            return;
        }

        if (isLocked)
        {
            OnLockedInteract(interactor);
            onLockedInteract?.Invoke();
            return;
        }

        InteractInternal(interactor);
    }

    public virtual void SetHighlighted(bool isHighlighted)
    {
        if (interactionHighlight != null)
        {
            interactionHighlight.SetHighlighted(isHighlighted);
        }
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        interactionEnabled = isEnabled;

        if (!interactionEnabled)
        {
            SetHighlighted(false);
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    protected virtual void OnLockedInteract(PlayerInteractionController interactor)
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
