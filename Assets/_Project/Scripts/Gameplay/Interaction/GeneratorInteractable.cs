using UnityEngine;
using UnityEngine.Events;
using System;

[DisallowMultipleComponent]
public sealed class GeneratorInteractable : InteractableBase
{
    [Header("Generator")]
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private bool startsOn;
    [SerializeField] private string turnOnPrompt = "Turn On Generator";
    [SerializeField] private string turnOffPrompt = "Turn Off Generator";
    [SerializeField] private Renderer[] activeStateRenderers;
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Vector3 offLeverLocalEulerAngles = new Vector3(-38f, 0f, 0f);
    [SerializeField] private Vector3 onLeverLocalEulerAngles = new Vector3(38f, 0f, 0f);
    [SerializeField] private float leverMoveSpeed = 8f;
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.04f, 0.04f, 1f);
    [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.35f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent onTurnedOn;
    [SerializeField] private UnityEvent onTurnedOff;

    private bool isOn;
    private MaterialPropertyBlock propertyBlock;
    private Quaternion offLeverRotation;
    private Quaternion onLeverRotation;

    public bool IsOn => isOn;
    public event Action TurnedOn;
    public event Action TurnedOff;
    public event Action<bool> StateChanged;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
        offLeverRotation = Quaternion.Euler(offLeverLocalEulerAngles);
        onLeverRotation = Quaternion.Euler(onLeverLocalEulerAngles);
        isOn = startsOn;
        ApplyVisualState();
        ApplyLeverStateImmediate();
        RefreshAvailabilityState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (progressionManager != null)
        {
            progressionManager.StateChanged += HandleProgressionStateChanged;
        }

        RefreshAvailabilityState();
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.StateChanged -= HandleProgressionStateChanged;
        }
    }

    private void Update()
    {
        if (leverPivot == null)
        {
            return;
        }

        Quaternion targetRotation = isOn ? onLeverRotation : offLeverRotation;
        leverPivot.localRotation = Quaternion.Slerp(leverPivot.localRotation, targetRotation, Time.deltaTime * leverMoveSpeed);
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        return isOn ? turnOffPrompt : turnOnPrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        SetPowerState(!isOn);
    }

    public bool SetPowerState(bool value)
    {
        if (isOn == value)
        {
            return false;
        }

        isOn = value;
        ApplyVisualState();

        if (isOn)
        {
            progressionManager?.StartGenerator();
            onTurnedOn?.Invoke();
            TurnedOn?.Invoke();
        }
        else
        {
            onTurnedOff?.Invoke();
            TurnedOff?.Invoke();
        }

        StateChanged?.Invoke(isOn);
        RefreshAvailabilityState();
        return true;
    }

    private void ResolveReferences()
    {
        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }
    }

    private void HandleProgressionStateChanged()
    {
        RefreshAvailabilityState();
    }

    private void RefreshAvailabilityState()
    {
        if (progressionManager == null)
        {
            return;
        }

        if (progressionManager.UnlockAllInteractionsForDebug)
        {
            SetLocked(false);
            return;
        }

        SetLocked(!progressionManager.CanInteractWithGenerator);
    }

    private void ApplyVisualState()
    {
        if (activeStateRenderers == null || activeStateRenderers.Length == 0)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        Color targetColor = isOn ? activeColor : inactiveColor;

        for (int i = 0; i < activeStateRenderers.Length; i++)
        {
            Renderer targetRenderer = activeStateRenderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", targetColor);
            propertyBlock.SetColor("_Color", targetColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ApplyLeverStateImmediate()
    {
        if (leverPivot == null)
        {
            return;
        }

        leverPivot.localRotation = isOn ? onLeverRotation : offLeverRotation;
    }
}
