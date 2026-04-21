using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class GeneratorInteractable : InteractableBase
{
    [Header("Generator")]
    [SerializeField] private bool startsOn;
    [SerializeField] private string turnOnPrompt = "Turn On Generator";
    [SerializeField] private string turnOffPrompt = "Turn Off Generator";
    [SerializeField] private Renderer[] activeStateRenderers;
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.04f, 0.04f, 1f);
    [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.35f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent onTurnedOn;
    [SerializeField] private UnityEvent onTurnedOff;

    private bool isOn;
    private MaterialPropertyBlock propertyBlock;

    public bool IsOn => isOn;
    public event Action TurnedOn;
    public event Action TurnedOff;
    public event Action<bool> StateChanged;

    protected override void Awake()
    {
        base.Awake();
        isOn = startsOn;
        ApplyVisualState();
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (!CanInteract(interactor))
        {
            return string.Empty;
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
            onTurnedOn?.Invoke();
            TurnedOn?.Invoke();
        }
        else
        {
            onTurnedOff?.Invoke();
            TurnedOff?.Invoke();
        }

        StateChanged?.Invoke(isOn);
        return true;
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
}
