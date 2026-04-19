using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public sealed class PlayerInteractionController : MonoBehaviour
{
    private const string InteractActionName = "Interact";

    [Header("Detection")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask interactableLayers;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private Key interactKey = Key.E;

    [Header("State")]
    [SerializeField] private bool interactionEnabled = true;
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private Font interactionPromptFont;

    private PlayerInput playerInput;
    private InputAction interactAction;
    private InteractableBase currentTarget;
    private RaycastHit currentTargetHit;
    private string currentPrompt = string.Empty;
    private InteractionOutlineHighlight currentTargetHighlight;
    private InteractableBase activeInteractable;

    public IInteractable CurrentTarget => currentTarget;
    public RaycastHit CurrentTargetHit => currentTargetHit;
    public bool InteractionEnabled => interactionEnabled;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        if (interactionCamera == null)
        {
            interactionCamera = GetComponentInChildren<Camera>();
        }

        if (interactionUI == null)
        {
            interactionUI = GetComponentInChildren<InteractionUI>(true);
        }

        if (interactionUI == null)
        {
            interactionUI = InteractionUI.Create(transform);
        }

        if (interactionUI != null && interactionPromptFont != null)
        {
            interactionUI.SetPromptFont(interactionPromptFont);
        }

        if (interactableLayers.value == 0)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                interactableLayers = 1 << interactableLayer;
            }
        }

        ClearCurrentTarget();
    }

    private void OnEnable()
    {
        CacheActions();
    }

    private void OnDisable()
    {
        if (activeInteractable != null)
        {
            activeInteractable.EndInteract(this);
            activeInteractable = null;
        }
        ClearCurrentTarget();
    }

    private void Update()
    {
        if (!interactionEnabled)
        {
            if (activeInteractable != null)
            {
                activeInteractable.EndInteract(this);
                activeInteractable = null;
            }
            ClearCurrentTarget();
            return;
        }

        UpdateCurrentTarget();

        if (activeInteractable != null)
        {
            if (WasInteractReleasedThisFrame() || currentTarget != activeInteractable)
            {
                activeInteractable.EndInteract(this);
                activeInteractable = null;
            }
        }

        if (currentTarget != null && activeInteractable == null && WasInteractPressedThisFrame())
        {
            activeInteractable = currentTarget;
            activeInteractable.Interact(this);
            RefreshCurrentTarget();
        }
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        if (interactionEnabled == isEnabled)
        {
            return;
        }

        interactionEnabled = isEnabled;

        if (!interactionEnabled)
        {
            if (activeInteractable != null)
            {
                activeInteractable.EndInteract(this);
                activeInteractable = null;
            }
            ClearCurrentTarget();
        }
    }

    public void RefreshCurrentTarget()
    {
        if (!interactionEnabled)
        {
            ClearCurrentTarget();
            return;
        }

        UpdateCurrentTarget(forceUiRefresh: true);
    }

    private void CacheActions()
    {
        interactAction = playerInput != null && playerInput.actions != null
            ? playerInput.actions[InteractActionName]
            : null;
    }

    private void UpdateCurrentTarget(bool forceUiRefresh = false)
    {
        InteractableBase nextTarget = null;
        RaycastHit nextHit = default;
        string nextPrompt = string.Empty;

        if (interactionCamera != null)
        {
            Ray centerRay = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(centerRay, out RaycastHit hitInfo, interactionDistance, interactableLayers, queryTriggerInteraction) &&
                TryGetInteractable(hitInfo.collider, out InteractableBase interactable) &&
                interactable.CanInteract(this))
            {
                string interactablePrompt = interactable.GetInteractionPrompt(this);
                if (!string.IsNullOrWhiteSpace(interactablePrompt))
                {
                    nextTarget = interactable;
                    nextHit = hitInfo;
                    nextPrompt = FormatPrompt(interactablePrompt);
                }
            }
        }

        ApplyTargetChange(nextTarget, nextHit, nextPrompt, forceUiRefresh);
    }

    private void ApplyTargetChange(InteractableBase nextTarget, RaycastHit nextHit, string nextPrompt, bool forceUiRefresh)
    {
        bool targetChanged = currentTarget != nextTarget;
        bool promptChanged = currentPrompt != nextPrompt;

        if (!targetChanged && !promptChanged && !forceUiRefresh)
        {
            return;
        }

        if (targetChanged)
        {
            SetTargetHighlight(currentTarget, currentTargetHighlight, false);

            if (currentTarget != null)
            {
                currentTarget.OnHoverExit(this);
            }
        }

        currentTarget = nextTarget;
        currentTargetHit = nextHit;
        currentPrompt = nextPrompt;
        currentTargetHighlight = currentTarget != null ? ResolveHighlight(currentTarget) : null;

        if (targetChanged)
        {
            SetTargetHighlight(currentTarget, currentTargetHighlight, true);

            if (currentTarget != null)
            {
                currentTarget.OnHoverEnter(this);
            }
        }

        if (interactionUI != null)
        {
            interactionUI.SetPrompt(currentPrompt, currentTarget != null);
        }
    }

    private void ClearCurrentTarget()
    {
        ApplyTargetChange(null, default, string.Empty, forceUiRefresh: true);
    }

    private bool WasInteractPressedThisFrame()
    {
        if (interactAction != null)
        {
            return interactAction.WasPressedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame;
    }

    private bool WasInteractReleasedThisFrame()
    {
        if (interactAction != null)
        {
            return interactAction.WasReleasedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current[interactKey].wasReleasedThisFrame;
    }

    private string FormatPrompt(string prompt)
    {
        string bindingLabel = GetInteractBindingLabel();
        return string.IsNullOrWhiteSpace(bindingLabel) ? prompt : $"[{bindingLabel}] {prompt}";
    }

    private string GetInteractBindingLabel()
    {
        if (interactAction == null)
        {
            return interactKey.ToString().ToUpperInvariant();
        }

        int bindingIndex = playerInput != null && !string.IsNullOrWhiteSpace(playerInput.currentControlScheme)
            ? interactAction.GetBindingIndex(group: playerInput.currentControlScheme)
            : -1;

        string displayString = bindingIndex >= 0
            ? interactAction.GetBindingDisplayString(bindingIndex)
            : interactAction.GetBindingDisplayString();

        return string.IsNullOrWhiteSpace(displayString)
            ? interactKey.ToString().ToUpperInvariant()
            : displayString;
    }

    private static InteractionOutlineHighlight ResolveHighlight(InteractableBase interactable)
    {
        if (interactable == null)
        {
            return null;
        }

        InteractionOutlineHighlight highlight = interactable.GetComponent<InteractionOutlineHighlight>();
        if (highlight == null)
        {
            highlight = interactable.gameObject.AddComponent<InteractionOutlineHighlight>();
        }

        return highlight;
    }

    private static void SetTargetHighlight(InteractableBase interactable, InteractionOutlineHighlight highlight, bool highlighted)
    {
        if (interactable == null || highlight == null)
        {
            return;
        }

        highlight.SetHighlighted(highlighted);
    }

    private static bool TryGetInteractable(Collider hitCollider, out InteractableBase interactable)
    {
        interactable = hitCollider != null ? hitCollider.GetComponentInParent<InteractableBase>() : null;
        return interactable != null;
    }
}
