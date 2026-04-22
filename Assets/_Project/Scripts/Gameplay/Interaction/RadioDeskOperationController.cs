using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using WatchOut;

[DisallowMultipleComponent]
public sealed class RadioDeskOperationController : MonoBehaviour
{
    private const string ScanDisplaySuffix = " SCN";

    [Header("References")]
    [SerializeField] private RadioSystem radioSystem;
    [SerializeField] private Camera operationCamera;
    [SerializeField] private TMP_Text frequencyDisplayText;
    [SerializeField] private Transform coarseTuneKnob;
    [SerializeField] private Transform fineTuneKnob;
    [SerializeField] private Transform signalNeedle;
    [SerializeField] private Transform signalStrengthFill;
    [SerializeField] private Renderer meterFaceRenderer;
    [SerializeField] private Renderer lockLampRenderer;

    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "Operate Radio";
    [SerializeField] private Key scanToggleKey = Key.S;

    [Header("Scan")]
    [SerializeField] [Min(0.05f)] private float scanReactionRange = 0.28f;
    [SerializeField] [Min(0f)] private float scanHitHoldDuration = 1.1f;
    [SerializeField] [Min(0f)] private float scanMeterBoost = 0.35f;
    [SerializeField] [Min(0f)] private float scanLampPulseSpeed = 6f;

    [Header("Signal Tuning")]
    [SerializeField] [Min(0.05f)] private float fineTuneActivationRange = 0.2f;
    [SerializeField] [Min(0f)] private float mouseAlignmentSensitivity = 0.0025f;
    [SerializeField] [Min(0f)] private float cursorFrequencyBiasPerSecond = 1.1f;
    [SerializeField] [Min(0f)] private float targetFrequencyBias = 0.5f;
    [SerializeField] [Min(0f)] private float targetWobbleAmplitude = 0.22f;
    [SerializeField] [Min(0f)] private float secondaryTargetWobbleAmplitude = 0.08f;
    [SerializeField] [Min(0f)] private float targetWobbleFrequency = 1.8f;
    [SerializeField] [Min(0f)] private float secondaryTargetWobbleFrequency = 4.1f;
    [SerializeField] [Range(0.01f, 0.5f)] private float lockAlignmentWindow = 0.12f;
    [SerializeField] [Min(0.1f)] private float lockHoldDuration = 1.1f;
    [SerializeField] [Min(0f)] private float lockDecayPerSecond = 0.85f;

    [Header("Visual Tuning")]
    [SerializeField] private float coarseKnobMinZ = 120f;
    [SerializeField] private float coarseKnobMaxZ = -120f;
    [SerializeField] private float fineTuneKnobMaxZ = 80f;
    [SerializeField] private float meterNeedleMaxZ = 42f;
    [SerializeField] [Min(0.01f)] private float meterHorizontalTravel = 0.16f;
    [SerializeField] [Min(0.05f)] private float targetBandMinWidth = 0.22f;
    [SerializeField] [Min(0.05f)] private float targetBandMaxWidth = 0.5f;
    [SerializeField] private Color idleMeterColor = new Color(0.11f, 0.09f, 0.06f, 1f);
    [SerializeField] private Color scanMeterColor = new Color(0.35f, 0.27f, 0.16f, 1f);
    [SerializeField] private Color activeMeterColor = new Color(0.29f, 0.23f, 0.15f, 1f);
    [SerializeField] private Color lockLampOffColor = new Color(0.07f, 0.03f, 0.01f, 1f);
    [SerializeField] private Color scanLampColor = new Color(0.92f, 0.66f, 0.21f, 1f);
    [SerializeField] private Color lockLampOnColor = new Color(0.93f, 0.54f, 0.18f, 1f);

    private PlayerInteractionController activeInteractor;
    private FirstPersonPlayerController activePlayerController;
    private Camera activePlayerCamera;
    private bool restoreInteractionEnabled;
    private bool restorePlayerControllerEnabled;
    private bool restorePlayerCameraEnabled;
    private bool wasTuneDownPressed;
    private bool wasTuneUpPressed;
    private float playerCursorOffset;
    private float targetOffset;
    private float lockProgress;
    private float scanHitHoldTimer;
    private float lastScanSignalStrength;
    private Vector3 signalStrengthBaseScale = Vector3.one;
    private Vector3 signalStrengthBasePosition = Vector3.zero;
    private MaterialPropertyBlock meterPropertyBlock;
    private MaterialPropertyBlock lockLampPropertyBlock;

    public bool IsInOperationMode => activeInteractor != null;
    public string InteractionPrompt => interactionPrompt;

    private void Awake()
    {
        ResolveReferences();

        if (signalStrengthFill != null)
        {
            signalStrengthBaseScale = signalStrengthFill.localScale;
            signalStrengthBasePosition = signalStrengthFill.localPosition;
        }

        if (operationCamera != null)
        {
            operationCamera.enabled = false;
        }
    }

    private void OnDisable()
    {
        if (IsInOperationMode)
        {
            ExitOperationMode();
        }
        else if (radioSystem != null)
        {
            radioSystem.SetAutoLockTemporarilySuppressed(false);
            radioSystem.StopTuning();
        }
    }

    private void Update()
    {
        ResolveReferences();
        RefreshVisuals();

        if (!IsInOperationMode)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitOperationMode();
            return;
        }

        HandleScanInput();
        HandleFrequencyInput();
        HandleFineTuning();
    }

    public bool TryEnterOperationMode(PlayerInteractionController interactor)
    {
        if (interactor == null || IsInOperationMode)
        {
            return false;
        }

        ResolveReferences();

        activeInteractor = interactor;
        activePlayerController = interactor.GetComponent<FirstPersonPlayerController>();
        activePlayerCamera = ResolvePlayerCamera(activeInteractor, activePlayerController);

        restoreInteractionEnabled = activeInteractor.InteractionEnabled;
        restorePlayerControllerEnabled = activePlayerController != null && activePlayerController.enabled;
        restorePlayerCameraEnabled = activePlayerCamera != null && activePlayerCamera.enabled;

        activeInteractor.SetInteractionEnabled(false);

        if (activePlayerController != null)
        {
            activePlayerController.enabled = false;
        }

        if (activePlayerCamera != null)
        {
            activePlayerCamera.enabled = false;
        }

        if (operationCamera != null)
        {
            operationCamera.enabled = true;
        }

        if (radioSystem != null)
        {
            radioSystem.StopTuning();
            radioSystem.SetAutoLockTemporarilySuppressed(true);
            radioSystem.SetScan(false);
        }
        playerCursorOffset = 0f;
        targetOffset = 0f;
        lockProgress = 0f;
        scanHitHoldTimer = 0f;
        lastScanSignalStrength = 0f;
        wasTuneDownPressed = false;
        wasTuneUpPressed = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        RefreshVisuals();
        return true;
    }

    public void ExitOperationMode()
    {
        if (!IsInOperationMode)
        {
            return;
        }

        radioSystem?.StopTuning();
        radioSystem?.SetAutoLockTemporarilySuppressed(false);
        radioSystem?.SetScan(false);

        if (operationCamera != null)
        {
            operationCamera.enabled = false;
        }

        if (activePlayerCamera != null)
        {
            activePlayerCamera.enabled = restorePlayerCameraEnabled;
        }

        if (activePlayerController != null)
        {
            activePlayerController.enabled = restorePlayerControllerEnabled;
        }

        if (activeInteractor != null)
        {
            activeInteractor.SetInteractionEnabled(restoreInteractionEnabled);
            activeInteractor.RefreshCurrentTarget();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        activeInteractor = null;
        activePlayerController = null;
        activePlayerCamera = null;
        wasTuneDownPressed = false;
        wasTuneUpPressed = false;
        playerCursorOffset = 0f;
        targetOffset = 0f;
        lockProgress = 0f;
        scanHitHoldTimer = 0f;
        lastScanSignalStrength = 0f;
        RefreshVisuals();
    }

    private void HandleScanInput()
    {
        if (radioSystem == null)
        {
            return;
        }

        bool toggleScanPressed = Keyboard.current != null && Keyboard.current[scanToggleKey].wasPressedThisFrame;
        if (toggleScanPressed)
        {
            SetScanMode(!radioSystem.IsAutoScanning);
        }

        if (scanHitHoldTimer > 0f)
        {
            scanHitHoldTimer = Mathf.Max(0f, scanHitHoldTimer - Time.unscaledDeltaTime);
        }

        if (!radioSystem.TryGetClosestActiveSignal(out RadioEventData _, out float closestDistance))
        {
            lastScanSignalStrength = Mathf.MoveTowards(lastScanSignalStrength, 0f, Time.unscaledDeltaTime * 1.5f);
            return;
        }

        float reactionRange = Mathf.Max(scanReactionRange, radioSystem.SignalFadeRange * 0.35f);
        float signalStrength = Mathf.Clamp01(1f - (closestDistance / Mathf.Max(0.01f, reactionRange)));
        lastScanSignalStrength = Mathf.Max(signalStrength, Mathf.MoveTowards(lastScanSignalStrength, 0f, Time.unscaledDeltaTime * 0.65f));

        if (!radioSystem.IsAutoScanning || closestDistance > reactionRange)
        {
            return;
        }

        scanHitHoldTimer = scanHitHoldDuration;
        SetScanMode(false);
    }

    private void HandleFrequencyInput()
    {
        if (radioSystem == null)
        {
            return;
        }

        bool tuneDown = Keyboard.current != null && Keyboard.current.aKey.isPressed;
        bool tuneUp = Keyboard.current != null && Keyboard.current.dKey.isPressed;

        if (tuneDown == tuneUp)
        {
            if (wasTuneDownPressed)
            {
                radioSystem.SetDecreasing(false);
            }

            if (wasTuneUpPressed)
            {
                radioSystem.SetIncreasing(false);
            }

            wasTuneDownPressed = false;
            wasTuneUpPressed = false;
            return;
        }

        if (tuneDown)
        {
            if (!wasTuneDownPressed)
            {
                SetScanMode(false);
                radioSystem.SetIncreasing(false);
                radioSystem.SetDecreasing(true);
            }

            wasTuneDownPressed = true;
            wasTuneUpPressed = false;
            return;
        }

        if (!wasTuneUpPressed)
        {
            SetScanMode(false);
            radioSystem.SetDecreasing(false);
            radioSystem.SetIncreasing(true);
        }

        wasTuneDownPressed = false;
        wasTuneUpPressed = true;
    }

    private void HandleFineTuning()
    {
        if (radioSystem == null)
        {
            return;
        }

        float activationRange = Mathf.Min(fineTuneActivationRange, radioSystem.SignalFadeRange);
        bool isScanning = radioSystem.IsAutoScanning;

        if (isScanning)
        {
            lockProgress = Mathf.MoveTowards(lockProgress, 0f, Time.unscaledDeltaTime * lockDecayPerSecond);
            return;
        }

        if (!radioSystem.TryGetClosestActiveSignal(out RadioEventData closestEvent, out float closestDistance) ||
            closestEvent == null ||
            closestDistance > activationRange)
        {
            playerCursorOffset = Mathf.MoveTowards(playerCursorOffset, -0.9f, Time.unscaledDeltaTime * 1.8f);
            targetOffset = Mathf.MoveTowards(targetOffset, 0f, Time.unscaledDeltaTime * 1.5f);
            lockProgress = Mathf.MoveTowards(lockProgress, 0f, Time.unscaledDeltaTime * lockDecayPerSecond);
            return;
        }

        float mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue().x : 0f;
        float frequencyOffset = closestEvent.TargetFrequency - radioSystem.CurrentFrequency;
        float normalizedFrequencyOffset = Mathf.Clamp(frequencyOffset / activationRange, -1f, 1f);
        float wobbleTime = Time.unscaledTime;
        float wobbleOffset =
            Mathf.Sin(wobbleTime * targetWobbleFrequency) * targetWobbleAmplitude +
            Mathf.Sin((wobbleTime * secondaryTargetWobbleFrequency) + 1.37f) * secondaryTargetWobbleAmplitude;

        targetOffset = Mathf.Clamp((normalizedFrequencyOffset * targetFrequencyBias) + wobbleOffset, -1f, 1f);
        playerCursorOffset -= mouseDelta * mouseAlignmentSensitivity;
        playerCursorOffset += normalizedFrequencyOffset * cursorFrequencyBiasPerSecond * Time.unscaledDeltaTime;
        playerCursorOffset = Mathf.Clamp(playerCursorOffset, -1f, 1f);

        float alignmentError = Mathf.Abs(playerCursorOffset - targetOffset);
        bool isCentered = alignmentError <= lockAlignmentWindow;
        bool canLockSignal = radioSystem.CanLockSignal(closestEvent);

        if (canLockSignal && isCentered)
        {
            lockProgress = Mathf.MoveTowards(lockProgress, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, lockHoldDuration));
        }
        else
        {
            lockProgress = Mathf.MoveTowards(lockProgress, 0f, Time.unscaledDeltaTime * lockDecayPerSecond);
        }

        if (lockProgress < 1f)
        {
            return;
        }

        ExitOperationMode();
        radioSystem.TryLockSignal(closestEvent);
    }

    private void RefreshVisuals()
    {
        if (frequencyDisplayText != null)
        {
            frequencyDisplayText.text = GetFrequencyDisplayText();
        }

        RefreshKnobs();
        RefreshMeter();
    }

    private void RefreshKnobs()
    {
        if (radioSystem != null)
        {
            float frequencyRange = Mathf.Max(0.01f, radioSystem.MaxFrequency - radioSystem.MinFrequency);
            float normalizedFrequency = Mathf.InverseLerp(radioSystem.MinFrequency, radioSystem.MaxFrequency, radioSystem.DisplayedFrequency);
            float coarseRotation = Mathf.Lerp(coarseKnobMinZ, coarseKnobMaxZ, normalizedFrequency);

            if (coarseTuneKnob != null)
            {
                coarseTuneKnob.localRotation = Quaternion.Euler(90f, 0f, coarseRotation);
            }

            if (fineTuneKnob != null)
            {
                fineTuneKnob.localRotation = Quaternion.Euler(90f, 0f, playerCursorOffset * -fineTuneKnobMaxZ);
            }
        }
    }

    private void RefreshMeter()
    {
        bool isScanning = radioSystem != null && radioSystem.IsAutoScanning;
        float activationRange = radioSystem != null ? Mathf.Min(fineTuneActivationRange, radioSystem.SignalFadeRange) : fineTuneActivationRange;
        float closestDistance = float.MaxValue;
        bool hasSignal = radioSystem != null &&
            radioSystem.TryGetClosestActiveSignal(out RadioEventData _, out closestDistance) &&
            closestDistance <= activationRange;
        float signalStrength = hasSignal
            ? Mathf.Clamp01(1f - (closestDistance / Mathf.Max(0.01f, activationRange)))
            : 0f;
        float scanMeterStrength = Mathf.Clamp01(Mathf.Max(lastScanSignalStrength + scanMeterBoost * (scanHitHoldTimer > 0f ? 1f : 0f), 0f));

        if (signalNeedle != null)
        {
            float needleTarget = isScanning
                ? Mathf.Lerp(-0.85f, 0.85f, scanMeterStrength)
                : hasSignal ? playerCursorOffset : -0.9f;
            signalNeedle.localRotation = Quaternion.Euler(0f, 0f, needleTarget * -meterNeedleMaxZ);
        }

        if (signalStrengthFill != null)
        {
            Vector3 fillScale = signalStrengthBaseScale;
            Vector3 fillPosition = signalStrengthBasePosition;

            if (isScanning)
            {
                fillScale.x *= Mathf.Lerp(targetBandMinWidth, targetBandMaxWidth, Mathf.Clamp01(scanMeterStrength));
                fillPosition.x += Mathf.Sin(Time.unscaledTime * scanLampPulseSpeed) * meterHorizontalTravel * 0.35f * Mathf.Clamp01(scanMeterStrength + 0.15f);
            }
            else if (hasSignal)
            {
                float targetBandWidth = Mathf.Lerp(targetBandMinWidth, targetBandMaxWidth, signalStrength);
                fillScale.x *= Mathf.Clamp(targetBandWidth, targetBandMinWidth, targetBandMaxWidth);
                fillPosition.x += targetOffset * meterHorizontalTravel;
            }
            else
            {
                fillScale.x *= 0.08f;
            }

            signalStrengthFill.localScale = fillScale;
            signalStrengthFill.localPosition = fillPosition;
        }

        Color meterColor = idleMeterColor;
        if (isScanning)
        {
            meterColor = Color.Lerp(idleMeterColor, scanMeterColor, Mathf.Clamp01(0.35f + scanMeterStrength));
        }
        else if (hasSignal)
        {
            meterColor = Color.Lerp(idleMeterColor, activeMeterColor, signalStrength);
        }

        Color lampColor = lockLampOffColor;
        if (isScanning)
        {
            float lampPulse = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(Time.unscaledTime * scanLampPulseSpeed) * 0.5f) + 0.5f);
            lampColor = Color.Lerp(lockLampOffColor, scanLampColor, Mathf.Clamp01(scanMeterStrength + lampPulse * 0.45f));
        }
        else if (scanHitHoldTimer > 0f)
        {
            lampColor = Color.Lerp(lockLampOffColor, scanLampColor, 1f);
        }
        else
        {
            lampColor = Color.Lerp(lockLampOffColor, lockLampOnColor, lockProgress);
        }

        ApplyRendererColor(meterFaceRenderer, meterColor, ref meterPropertyBlock);
        ApplyRendererColor(lockLampRenderer, lampColor, ref lockLampPropertyBlock);
    }

    private void ResolveReferences()
    {
        if (radioSystem == null)
        {
            radioSystem = FindAnyObjectByType<RadioSystem>();
        }

        if (operationCamera == null)
        {
            operationCamera = GetComponentInChildren<Camera>(true);
        }
    }

    private static Camera ResolvePlayerCamera(PlayerInteractionController interactor, FirstPersonPlayerController playerController)
    {
        if (playerController != null && playerController.CameraPivot != null)
        {
            Camera pivotCamera = playerController.CameraPivot.GetComponentInChildren<Camera>(true);
            if (pivotCamera != null)
            {
                return pivotCamera;
            }
        }

        return interactor != null ? interactor.GetComponentInChildren<Camera>(true) : null;
    }

    private static void ApplyRendererColor(Renderer targetRenderer, Color color, ref MaterialPropertyBlock propertyBlock)
    {
        if (targetRenderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetScanMode(bool shouldScan)
    {
        if (radioSystem == null)
        {
            return;
        }

        radioSystem.SetScan(shouldScan);
        if (shouldScan)
        {
            radioSystem.StopTuning();
            radioSystem.SetScan(true);
            wasTuneDownPressed = false;
            wasTuneUpPressed = false;
            lockProgress = 0f;
            scanHitHoldTimer = 0f;
        }
    }

    private string GetFrequencyDisplayText()
    {
        if (radioSystem == null)
        {
            return "--.-";
        }

        string frequencyText = radioSystem.DisplayedFrequency.ToString("F1");
        if (radioSystem.IsAutoScanning)
        {
            return frequencyText + ScanDisplaySuffix;
        }

        return frequencyText;
    }
}
