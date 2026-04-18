using System.Collections;
using UnityEngine;
using TMPro;

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
        [SerializeField] private AudioSource broadcastAudioSource;
        [SerializeField] private float maxStaticVolume = 0.5f;
        [SerializeField] private float maxBroadcastVolume = 1f;

        [Header("Tuning Settings")]
        [SerializeField] private float minFrequency = 87.5f;
        [SerializeField] private float maxFrequency = 108.0f;
        [SerializeField] private float frequencyStep = 0.1f;
        [SerializeField] private float continuousChangeSpeed = 10f;
        [SerializeField] private float holdDelay = 0.5f;
        [SerializeField] private float autoScanSpeed = 2.0f;
        
        [Header("Signal Events")]
        [Tooltip("Distance from target frequency where the broadcast starts fading in.")]
        [SerializeField] private float signalFadeRange = 0.5f; 
        [Tooltip("Distance from target frequency where the channel is locked and dialogue plays.")]
        [SerializeField] private float signalLockTolerance = 0.05f;

        private float currentFrequency = 87.5f;
        private bool isIncreasing;
        private bool isDecreasing;
        private bool isAutoScanning;
        private bool manualSignalLockArmed;
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

            if (staticAudioSource != null)
            {
                staticAudioSource.loop = true;
                staticAudioSource.volume = maxStaticVolume;
                if (!staticAudioSource.isPlaying) staticAudioSource.Play();
            }

            if (broadcastAudioSource != null)
            {
                broadcastAudioSource.loop = true;
                broadcastAudioSource.volume = 0f;
            }

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
                    float timePerStep = 1f / continuousChangeSpeed;

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
            if (!IsRadioPowered())
            {
                ResetSignalState();
                return;
            }

            if (progressionManager == null ||
                !progressionManager.TryGetClosestRadioEvent(currentFrequency, true, out RadioEventData closestEvent, out float closestDistance))
            {
                ResetSignalState();
                return;
            }

            // Smoothly crossfade static and broadcast audio when inside the fade range
            if (closestDistance <= signalFadeRange)
            {
                float signalStrength = 1f - (closestDistance / signalFadeRange);
                
                if (staticAudioSource != null)
                {
                    staticAudioSource.volume = Mathf.Lerp(maxStaticVolume, 0f, signalStrength);
                }

                if (broadcastAudioSource != null && closestEvent.BroadcastAudio != null)
                {
                    if (broadcastAudioSource.clip != closestEvent.BroadcastAudio)
                    {
                        broadcastAudioSource.clip = closestEvent.BroadcastAudio;
                        if (!broadcastAudioSource.isPlaying) broadcastAudioSource.Play();
                    }
                    broadcastAudioSource.volume = Mathf.Lerp(0f, maxBroadcastVolume, signalStrength);
                }

                // Check for a solid lock to trigger dialogue
                if (closestDistance <= signalLockTolerance &&
                    currentLockedEvent != closestEvent &&
                    manualSignalLockArmed)
                {
                    LockOnSignal(closestEvent);
                }
                else if (closestDistance > signalLockTolerance && currentLockedEvent == closestEvent)
                {
                    currentLockedEvent = null;
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

            bool eventFoundNow = progressionManager == null || progressionManager.MarkRadioEventFound(radioEvent);

            if (eventFoundNow && radioEvent.DialogueConversation != null && dialogueRunner != null)
            {
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
        }

        private void ResetSignalState()
        {
            if (staticAudioSource != null)
            {
                staticAudioSource.volume = maxStaticVolume;
            }

            if (broadcastAudioSource != null)
            {
                broadcastAudioSource.volume = 0f;
            }

            currentLockedEvent = null;
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
    }
}
