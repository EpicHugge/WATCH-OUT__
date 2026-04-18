using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

namespace WatchOut
{
    [DisallowMultipleComponent]
    public class RadioSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProgressionManager progressionManager;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private GeneratorInteractable generatorInteractable;
        
        [Header("UI References")]
        [SerializeField] private TMP_Text frequencyDisplayText;
        [SerializeField] private Renderer poweredScreenRenderer;
        [SerializeField] private Color screenPoweredColor = new Color(0.12f, 0.36f, 0.18f, 1f);
        [SerializeField] private Color screenUnpoweredColor = new Color(0.02f, 0.04f, 0.03f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioSource staticAudioSource;
        [SerializeField] private float maxStaticVolume = 0.5f;
        [FormerlySerializedAs("broadcastAudioSource")]
        [SerializeField] private AudioSource nearSignalAudioSource;
        [SerializeField] private AudioSource exactLockAudioSource;
        [SerializeField] private AudioClip staticLoopClip;
        [SerializeField] private AudioClip nearSignalLoopClip;
        [SerializeField] private AudioClip exactLockClip;
        [FormerlySerializedAs("maxBroadcastVolume")]
        [SerializeField] private float maxNearSignalVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float dialogueAudioDuckMultiplier = 0.05f;

        [Header("Tuning Settings")]
        [SerializeField] private float minFrequency = 87.5f;
        [SerializeField] private float maxFrequency = 108.0f;
        [SerializeField] private float frequencyStep = 0.1f;
        [SerializeField] private float continuousChangeSpeed = 10f;
        [SerializeField] private float maxContinuousChangeSpeed = 20f;
        [SerializeField] private float holdDelay = 0.5f;
        [SerializeField] private float holdAccelerationDuration = 1.25f;
        [SerializeField] private float autoScanSpeed = 2.0f;
        
        [Header("Signal Events")]
        [Tooltip("Distance from target frequency where the broadcast starts fading in.")]
        [SerializeField] private float signalFadeRange = 0.5f; 
        [Tooltip("Distance from target frequency where the channel is locked and dialogue plays.")]
        [SerializeField] private float signalLockTolerance = 0.05f;

        [Header("Hidden Test Signal")]
        [SerializeField] private bool useHiddenTestSignal;
        [SerializeField] private float hiddenTestTargetFrequency = 94.3f;
        [SerializeField] [Min(0.01f)] private float hiddenTestProximityRange = 0.5f;
        [SerializeField] [Min(0.001f)] private float hiddenTestExactTolerance = 0.05f;

        private float currentFrequency = 87.5f;
        private bool isIncreasing;
        private bool isDecreasing;
        private bool isAutoScanning;
        private bool manualSignalLockArmed;
        private bool hiddenSignalLocked;
        private float holdTimer;
        private float changeTimer;
        private RadioEventData currentLockedEvent;
        private Coroutine pendingDebugLoopRoutine;
        private MaterialPropertyBlock screenPropertyBlock;

        public float CurrentFrequency => currentFrequency;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (progressionManager != null)
            {
                progressionManager.StateChanged += HandleProgressionStateChanged;
            }

            if (generatorInteractable != null)
            {
                generatorInteractable.StateChanged += HandleGeneratorStateChanged;
            }
        }

        private void OnDisable()
        {
            if (progressionManager != null)
            {
                progressionManager.StateChanged -= HandleProgressionStateChanged;
            }

            if (generatorInteractable != null)
            {
                generatorInteractable.StateChanged -= HandleGeneratorStateChanged;
            }
        }

        private void Start()
        {
            ResolveReferences();
            UpdateDisplay();
            ConfigureAudioSources();

            RefreshPowerState();
        }

        private void Update()
        {
            HandleTuning();
            HandleAudioAndEvents();
        }

        private void HandleTuning()
        {
            if (!IsRadioPowered())
            {
                ResetTuningState();
                return;
            }

            if (isIncreasing || isDecreasing)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdDelay)
                {
                    changeTimer += Time.deltaTime;
                    float heldBeyondDelay = holdTimer - holdDelay;
                    float accelerationProgress = holdAccelerationDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(heldBeyondDelay / holdAccelerationDuration);
                    float currentChangeSpeed = Mathf.Lerp(
                        continuousChangeSpeed,
                        Mathf.Max(continuousChangeSpeed, maxContinuousChangeSpeed),
                        accelerationProgress);
                    float timePerStep = 1f / Mathf.Max(0.001f, currentChangeSpeed);

                    while (changeTimer >= timePerStep)
                    {
                        changeTimer -= timePerStep;
                        if (isIncreasing) currentFrequency += frequencyStep;
                        if (isDecreasing) currentFrequency -= frequencyStep;

                        // Wrap frequencies around when passing min/max limits
                        if (currentFrequency > maxFrequency) currentFrequency = minFrequency;
                        if (currentFrequency < minFrequency) currentFrequency = maxFrequency;
                        
                        UpdateDisplay();
                    }
                }
            }
            else
            {
                holdTimer = 0f;
                changeTimer = 0f;
            }

            if (isAutoScanning)
            {
                currentFrequency += autoScanSpeed * Time.deltaTime;
                if (currentFrequency > maxFrequency) currentFrequency = minFrequency;
                if (currentFrequency < minFrequency) currentFrequency = maxFrequency;
                UpdateDisplay();
            }
        }

        private void HandleAudioAndEvents()
        {
            bool isDialogueRunning = dialogueRunner != null && dialogueRunner.IsRunning;
            SyncStaticLoopPlayback(IsRadioPowered() && !isDialogueRunning);

            if (!IsRadioPowered())
            {
                ResetSignalState();
                return;
            }

            if (!TryGetActiveSignal(out float targetFrequency, out float proximityRange, out float lockTolerance, out RadioEventData closestEvent))
            {
                ResetSignalState();
                return;
            }

            float closestDistance = Mathf.Abs(targetFrequency - currentFrequency);

            // Smoothly crossfade static and near-signal audio when inside the fade range.
            if (closestDistance <= proximityRange)
            {
                float signalStrength = 1f - (closestDistance / proximityRange);
                float nearSignalMultiplier = isDialogueRunning ? dialogueAudioDuckMultiplier : 1f;
                
                if (staticAudioSource != null)
                {
                    staticAudioSource.volume = isDialogueRunning
                        ? 0f
                        : Mathf.Lerp(maxStaticVolume, 0f, signalStrength);
                }

                if (nearSignalAudioSource != null)
                {
                    nearSignalAudioSource.volume = Mathf.Lerp(0f, maxNearSignalVolume, signalStrength) * nearSignalMultiplier;
                }

                bool manualExactMatch = closestDistance <= lockTolerance && manualSignalLockArmed && !isAutoScanning;

                if (useHiddenTestSignal)
                {
                    if (manualExactMatch && !hiddenSignalLocked)
                    {
                        TriggerHiddenTestSignalLock(lockTolerance);
                    }
                    else if (closestDistance > lockTolerance && hiddenSignalLocked)
                    {
                        hiddenSignalLocked = false;
                    }
                }
                else if (closestEvent != null)
                {
                    if (closestDistance <= lockTolerance &&
                        currentLockedEvent != closestEvent &&
                        manualSignalLockArmed)
                    {
                        LockOnSignal(closestEvent);
                    }
                    else if (closestDistance > lockTolerance && currentLockedEvent == closestEvent)
                    {
                        currentLockedEvent = null;
                    }
                }
            }
            else
            {
                ResetSignalState();
            }
        }

        private void LockOnSignal(RadioEventData radioEvent)
        {
            currentLockedEvent = radioEvent;
            manualSignalLockArmed = false;

            // Stop all tuning mechanisms to "lock" onto the channel
            isAutoScanning = false;
            isIncreasing = false;
            isDecreasing = false;

            // Snap the frequency exactly to the target for a clean UI reading and magnetic feel
            currentFrequency = radioEvent.TargetFrequency;
            UpdateDisplay();

            if (exactLockAudioSource != null && exactLockClip != null)
            {
                exactLockAudioSource.PlayOneShot(exactLockClip);
            }

            bool eventFoundNow = progressionManager == null || progressionManager.MarkRadioEventFound(radioEvent);

            if (eventFoundNow && radioEvent.DialogueConversation != null && dialogueRunner != null)
            {
                SyncStaticLoopPlayback(false);
                staticAudioSource.volume = 0f;
                dialogueRunner.StartConversation(radioEvent.DialogueConversation);
            }

            HandleDebugLoopCompletion(eventFoundNow);
        }

        public void SetIncreasing(bool value)
        {
            if (value && !IsRadioPowered())
            {
                return;
            }

            isIncreasing = value;
            if (value)
            {
                TryBeginRadioScan();
                manualSignalLockArmed = true;
                isAutoScanning = false;
                isDecreasing = false;
                
                // Trigger the first single tap immediately
                currentFrequency += frequencyStep;
                if (currentFrequency > maxFrequency) currentFrequency = minFrequency;
                UpdateDisplay();
                
                holdTimer = 0f;
                changeTimer = 0f;
            }
        }

        public void SetDecreasing(bool value)
        {
            if (value && !IsRadioPowered())
            {
                return;
            }

            isDecreasing = value;
            if (value)
            {
                TryBeginRadioScan();
                manualSignalLockArmed = true;
                isAutoScanning = false;
                isIncreasing = false;
                
                // Trigger the first single tap immediately
                currentFrequency -= frequencyStep;
                if (currentFrequency < minFrequency) currentFrequency = maxFrequency;
                UpdateDisplay();
                
                holdTimer = 0f;
                changeTimer = 0f;
            }
        }

        public void ToggleScan()
        {
            if (!IsRadioPowered())
            {
                return;
            }

            TryBeginRadioScan();
            isAutoScanning = !isAutoScanning;

            if (isAutoScanning)
            {
                manualSignalLockArmed = false;
            }
        }

        private void UpdateDisplay()
        {
            if (frequencyDisplayText != null)
            {
                frequencyDisplayText.text = currentFrequency.ToString("F1") + " FM";
            }
        }

        private void RefreshPowerState()
        {
            bool isPowered = IsRadioPowered();

            if (frequencyDisplayText != null)
            {
                frequencyDisplayText.enabled = isPowered;
            }

            if (poweredScreenRenderer != null)
            {
                if (screenPropertyBlock == null)
                {
                    screenPropertyBlock = new MaterialPropertyBlock();
                }

                Color targetColor = isPowered ? screenPoweredColor : screenUnpoweredColor;
                poweredScreenRenderer.GetPropertyBlock(screenPropertyBlock);
                screenPropertyBlock.SetColor("_BaseColor", targetColor);
                screenPropertyBlock.SetColor("_Color", targetColor);
                poweredScreenRenderer.SetPropertyBlock(screenPropertyBlock);
            }

            if (!isPowered)
            {
                ResetTuningState();
                ResetSignalState();
            }

            SyncStaticLoopPlayback(isPowered && (dialogueRunner == null || !dialogueRunner.IsRunning));

            if (staticAudioSource != null)
            {
                staticAudioSource.volume = isPowered ? maxStaticVolume : 0f;
            }
        }

        private void ResetSignalState()
        {
            if (staticAudioSource != null)
            {
                staticAudioSource.volume = IsRadioPowered() ? maxStaticVolume : 0f;
            }

            if (nearSignalAudioSource != null)
            {
                nearSignalAudioSource.volume = 0f;
            }

            currentLockedEvent = null;
            hiddenSignalLocked = false;
        }

        private void ResetTuningState()
        {
            isIncreasing = false;
            isDecreasing = false;
            isAutoScanning = false;
            manualSignalLockArmed = false;
            holdTimer = 0f;
            changeTimer = 0f;
        }

        private void ConfigureAudioSources()
        {
            staticAudioSource = EnsureLoopSource(staticAudioSource, "Radio Static", staticLoopClip);
            nearSignalAudioSource = EnsureLoopSource(nearSignalAudioSource, "Radio Near Signal", nearSignalLoopClip);
            exactLockAudioSource = EnsureOneShotSource(exactLockAudioSource, "Radio Lock Cue");

            if (staticAudioSource != null)
            {
                staticAudioSource.volume = 0f;
            }

            if (nearSignalAudioSource != null)
            {
                nearSignalAudioSource.volume = 0f;
            }
        }

        private void ResolveReferences()
        {
            if (progressionManager == null)
            {
                progressionManager = FindAnyObjectByType<ProgressionManager>();
            }

            if (dialogueRunner == null)
            {
                dialogueRunner = FindAnyObjectByType<DialogueRunner>();
            }

            if (generatorInteractable == null)
            {
                generatorInteractable = FindAnyObjectByType<GeneratorInteractable>();
            }

            if (poweredScreenRenderer == null && frequencyDisplayText != null)
            {
                Transform screenTransform = frequencyDisplayText.transform.parent;
                if (screenTransform != null)
                {
                    poweredScreenRenderer = screenTransform.GetComponent<Renderer>();
                }
            }
        }

        private void TryBeginRadioScan()
        {
            progressionManager?.EnsureRadioScanActive();
        }

        private bool IsRadioPowered()
        {
            if (generatorInteractable != null)
            {
                return generatorInteractable.IsOn;
            }

            return progressionManager != null && progressionManager.GeneratorStartedToday;
        }

        private void HandleGeneratorStateChanged(bool isPowered)
        {
            RefreshPowerState();
        }

        private void HandleProgressionStateChanged()
        {
            if (progressionManager != null && !progressionManager.GeneratorStartedToday)
            {
                currentFrequency = minFrequency;
                currentLockedEvent = null;
                UpdateDisplay();
            }

            RefreshPowerState();
        }

        private bool TryGetActiveSignal(
            out float targetFrequency,
            out float proximityRange,
            out float lockTolerance,
            out RadioEventData closestEvent)
        {
            closestEvent = null;
            targetFrequency = 0f;
            proximityRange = Mathf.Max(signalFadeRange, 0.01f);
            lockTolerance = Mathf.Max(signalLockTolerance, 0.001f);

            if (currentLockedEvent != null && dialogueRunner != null && dialogueRunner.IsRunning)
            {
                closestEvent = currentLockedEvent;
                targetFrequency = currentLockedEvent.TargetFrequency;
                SetNearSignalClip(currentLockedEvent.BroadcastAudio != null
                    ? currentLockedEvent.BroadcastAudio
                    : nearSignalLoopClip);
                return true;
            }

            if (progressionManager != null &&
                progressionManager.TryGetClosestRadioEvent(currentFrequency, false, out closestEvent, out _))
            {
                targetFrequency = closestEvent.TargetFrequency;
                SetNearSignalClip(closestEvent.BroadcastAudio != null ? closestEvent.BroadcastAudio : nearSignalLoopClip);
                return true;
            }

            if (!useHiddenTestSignal)
            {
                return false;
            }

            targetFrequency = hiddenTestTargetFrequency;
            proximityRange = Mathf.Max(hiddenTestProximityRange, 0.01f);
            lockTolerance = Mathf.Max(hiddenTestExactTolerance, 0.001f);
            SetNearSignalClip(nearSignalLoopClip);
            return true;
        }

        private void SetNearSignalClip(AudioClip clip)
        {
            if (nearSignalAudioSource == null)
            {
                return;
            }

            if (nearSignalAudioSource.clip != clip)
            {
                nearSignalAudioSource.Stop();
                nearSignalAudioSource.clip = clip;
            }

            if (clip != null && !nearSignalAudioSource.isPlaying)
            {
                nearSignalAudioSource.Play();
            }

            nearSignalAudioSource.volume = 0f;
        }

        private void TriggerHiddenTestSignalLock(float lockTolerance)
        {
            hiddenSignalLocked = true;
            manualSignalLockArmed = false;

            if (exactLockAudioSource != null && exactLockClip != null)
            {
                exactLockAudioSource.PlayOneShot(exactLockClip);
            }

            if (progressionManager != null &&
                progressionManager.TryGetClosestRadioEvent(currentFrequency, true, out RadioEventData hiddenEvent, out float closestDistance) &&
                closestDistance <= lockTolerance)
            {
                LockOnSignal(hiddenEvent);
            }
        }

        private void HandleDebugLoopCompletion(bool eventFoundNow)
        {
            if (!eventFoundNow ||
                progressionManager == null ||
                !progressionManager.PowerDownGeneratorAndRadioAfterDialogueInTestBuild)
            {
                return;
            }

            if (pendingDebugLoopRoutine != null)
            {
                StopCoroutine(pendingDebugLoopRoutine);
            }

            pendingDebugLoopRoutine = StartCoroutine(CompleteDebugLoopAfterDialogueRoutine());
        }

        private IEnumerator CompleteDebugLoopAfterDialogueRoutine()
        {
            while (dialogueRunner != null && dialogueRunner.IsRunning)
            {
                yield return null;
            }

            pendingDebugLoopRoutine = null;

            progressionManager.CompleteDebugRadioLoopAfterDialogue();
            generatorInteractable?.SetPowerState(false);
            RefreshPowerState();
        }

        private AudioSource EnsureLoopSource(AudioSource source, string childName, AudioClip clip)
        {
            if (source == null)
            {
                source = CreateChildAudioSource(childName);
            }

            if (source == null)
            {
                return null;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.clip = clip;

            return source;
        }

        private AudioSource EnsureOneShotSource(AudioSource source, string childName)
        {
            if (source == null)
            {
                source = CreateChildAudioSource(childName);
            }

            if (source == null)
            {
                return null;
            }

            source.playOnAwake = false;
            source.loop = false;
            return source;
        }

        private AudioSource CreateChildAudioSource(string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            return child.AddComponent<AudioSource>();
        }

        private void SyncStaticLoopPlayback(bool shouldPlay)
        {
            if (staticAudioSource == null)
            {
                return;
            }

            if (staticAudioSource.clip != staticLoopClip)
            {
                staticAudioSource.clip = staticLoopClip;
            }

            if (!shouldPlay || staticLoopClip == null)
            {
                if (staticAudioSource.isPlaying)
                {
                    staticAudioSource.Stop();
                }

                return;
            }

            if (!staticAudioSource.isPlaying)
            {
                staticAudioSource.Play();
            }
        }
    }
}
