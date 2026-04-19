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
        private bool hiddenSignalLocked;
        private float holdTimer;
        private float changeTimer;
        private RadioEventData currentLockedEvent;
        private RadioEventData pendingResolvedEvent;
        private MaterialPropertyBlock screenPropertyBlock;
        private ProgressionManager subscribedProgressionManager;
        private DialogueRunner subscribedDialogueRunner;
        private GeneratorInteractable subscribedGeneratorInteractable;
        private RadioEventData lastLoggedActiveEvent;
        private bool lastLoggedLockMatch;

        public float CurrentFrequency => currentFrequency;
        public float DisplayedFrequency => Mathf.Round(currentFrequency * 10f) / 10f;
        public bool IsAutoScanning => isAutoScanning;
        public RadioEventData ActiveJamEvent =>
            progressionManager != null && progressionManager.ActiveRadioEventsToday.Count > 0
                ? progressionManager.ActiveRadioEventsToday[0]
                : null;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            RefreshSubscriptions(clearOnly: true);
        }

        private void Start()
        {
            ResolveReferences();
            UpdateDisplay();
            ConfigureAudioSources();

            RefreshPowerState();
            LogActiveEventIfChanged();
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
                        if (isIncreasing)
                        {
                            StepFrequency(frequencyStep, "Tune Up", attemptManualLock: true, snapToStep: true);
                        }
                        else if (isDecreasing)
                        {
                            StepFrequency(-frequencyStep, "Tune Down", attemptManualLock: true, snapToStep: true);
                        }

                        if (currentLockedEvent != null || pendingResolvedEvent != null)
                        {
                            break;
                        }
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
                StepFrequency(autoScanSpeed * Time.deltaTime, "Scan", attemptManualLock: false, snapToStep: false);
            }
        }

        private void HandleAudioAndEvents()
        {
            bool isDialogueRunning = dialogueRunner != null && dialogueRunner.IsRunning;
            if (isDialogueRunning)
            {
                MuteRadioAudio(stopExactLock: false);
            }
            else
            {
                SyncStaticLoopPlayback(IsRadioPowered());
            }

            if (!IsRadioPowered())
            {
                ResetSignalState();
                return;
            }

            if (isDialogueRunning)
            {
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
                SyncNearSignalLoopPlayback(nearSignalAudioSource != null && nearSignalAudioSource.clip != null);
                
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

                if (useHiddenTestSignal)
                {
                    bool canTriggerHiddenLock = !isAutoScanning && IsFrequencyWithinLockWindow(currentFrequency, targetFrequency, lockTolerance);
                    if (canTriggerHiddenLock && !hiddenSignalLocked)
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
                    if (closestDistance > lockTolerance && currentLockedEvent == closestEvent)
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
            if (radioEvent == null)
            {
                return;
            }

            if (progressionManager != null && !progressionManager.IsCurrentTargetRadioEvent(radioEvent))
            {
                return;
            }

            currentLockedEvent = radioEvent;

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

            Debug.Log(
                $"RadioSystem locked onto '{radioEvent.EventName}' at {radioEvent.TargetFrequency:F1} FM on Day {progressionManager?.CurrentDay ?? 0}.",
                this);

            if (radioEvent.DialogueConversation != null && dialogueRunner != null)
            {
                pendingResolvedEvent = radioEvent;
                MuteRadioAudio();
                if (!dialogueRunner.StartConversation(radioEvent.DialogueConversation))
                {
                    pendingResolvedEvent = null;
                    return;
                }

                return;
            }

            progressionManager?.MarkRadioEventFound(radioEvent);
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
                isAutoScanning = false;
                isDecreasing = false;
                StepFrequency(frequencyStep, "Tune Up", attemptManualLock: true, snapToStep: true);
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
                isAutoScanning = false;
                isIncreasing = false;
                StepFrequency(-frequencyStep, "Tune Down", attemptManualLock: true, snapToStep: true);
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
        }

        private void UpdateDisplay()
        {
            if (frequencyDisplayText != null)
            {
                frequencyDisplayText.text = DisplayedFrequency.ToString("F1") + " FM";
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

            if (dialogueRunner != null && dialogueRunner.IsRunning)
            {
                MuteRadioAudio(stopExactLock: false);
            }
            else
            {
                SyncStaticLoopPlayback(isPowered);

                if (staticAudioSource != null)
                {
                    staticAudioSource.volume = isPowered ? maxStaticVolume : 0f;
                }
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
                SyncNearSignalLoopPlayback(false);
                nearSignalAudioSource.volume = 0f;
            }

            currentLockedEvent = null;
            hiddenSignalLocked = false;
        }

        private void MuteRadioAudio(bool stopExactLock = true)
        {
            SyncStaticLoopPlayback(false);
            SyncNearSignalLoopPlayback(false);

            if (staticAudioSource != null)
            {
                staticAudioSource.volume = 0f;
            }

            if (nearSignalAudioSource != null)
            {
                nearSignalAudioSource.volume = 0f;
            }

            if (stopExactLock && exactLockAudioSource != null)
            {
                exactLockAudioSource.Stop();
            }
        }

        private void ResetTuningState()
        {
            isIncreasing = false;
            isDecreasing = false;
            isAutoScanning = false;
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

            if (frequencyDisplayText == null)
            {
                frequencyDisplayText = FindFrequencyDisplayText();
            }

            if (poweredScreenRenderer == null)
            {
                poweredScreenRenderer = FindPoweredScreenRenderer();
            }

            if (staticAudioSource == null)
            {
                staticAudioSource = FindAudioSource("Audio Static");
            }

            if (nearSignalAudioSource == null)
            {
                nearSignalAudioSource = FindAudioSource("Audio Near Signal");
            }

            if (exactLockAudioSource == null)
            {
                exactLockAudioSource = FindAudioSource("Audio Lock Cue");
            }

            if (poweredScreenRenderer == null && frequencyDisplayText != null)
            {
                Transform current = frequencyDisplayText.transform.parent;
                while (current != null && poweredScreenRenderer == null)
            {
                poweredScreenRenderer = current.GetComponent<Renderer>();
                current = current.parent;
            }
        }

            RefreshSubscriptions();
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
                pendingResolvedEvent = null;
                UpdateDisplay();
            }

            RefreshPowerState();
            LogActiveEventIfChanged();
            LogLockMatchIfChanged();
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
                nearSignalAudioSource.volume = 0f;
            }
        }

        private void TriggerHiddenTestSignalLock(float lockTolerance)
        {
            hiddenSignalLocked = true;

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

        private void HandleDialogueEnded(DialogueConversation conversation)
        {
            if (pendingResolvedEvent == null || conversation == null)
            {
                return;
            }

            if (pendingResolvedEvent.DialogueConversation != conversation)
            {
                return;
            }

            RadioEventData resolvedEvent = pendingResolvedEvent;
            pendingResolvedEvent = null;
            currentLockedEvent = null;
            progressionManager?.MarkRadioEventFound(resolvedEvent);
            RefreshPowerState();
            LogActiveEventIfChanged();
            LogLockMatchIfChanged();
        }

        private void HandleDialogueStarted(DialogueConversation conversation)
        {
            MuteRadioAudio();
        }

        private void StepFrequency(float delta, string inputSource, bool attemptManualLock, bool snapToStep)
        {
            currentFrequency = WrapFrequency(currentFrequency + delta);

            if (snapToStep)
            {
                currentFrequency = Mathf.Round(currentFrequency * 10f) / 10f;
            }

            UpdateDisplay();
            LogLockMatchIfChanged();

            if (attemptManualLock)
            {
                TryLockCurrentFrequencyIfPossible(inputSource);
            }
        }

        private void TryLockCurrentFrequencyIfPossible(string inputSource)
        {
            if (isAutoScanning || pendingResolvedEvent != null || currentLockedEvent != null)
            {
                return;
            }

            if (progressionManager == null || !progressionManager.CanUseRadioControls)
            {
                Debug.LogWarning($"RadioSystem ignored {inputSource} because radio controls are not currently enabled.", this);
                return;
            }

            if (!TryGetActiveSignal(out float targetFrequency, out _, out float lockTolerance, out RadioEventData closestEvent) ||
                closestEvent == null)
            {
                Debug.LogWarning($"RadioSystem could not find an active jam event after {inputSource}.", this);
                return;
            }

            if (!IsFrequencyWithinLockWindow(currentFrequency, targetFrequency, lockTolerance))
            {
                LogFailedManualLock(inputSource, closestEvent, targetFrequency, lockTolerance);
                return;
            }

            LockOnSignal(closestEvent);
        }

        private bool IsFrequencyWithinLockWindow(float tunedFrequency, float targetFrequency, float baseTolerance)
        {
            int tunedBucket = Mathf.RoundToInt(tunedFrequency * 10f);
            int targetBucket = Mathf.RoundToInt(targetFrequency * 10f);
            if (tunedBucket == targetBucket)
            {
                return true;
            }

            float effectiveTolerance = Mathf.Max(baseTolerance, (frequencyStep * 0.5f) + 0.005f);
            return Mathf.Abs(targetFrequency - tunedFrequency) <= effectiveTolerance;
        }

        private float WrapFrequency(float frequency)
        {
            if (frequency > maxFrequency)
            {
                return minFrequency;
            }

            if (frequency < minFrequency)
            {
                return maxFrequency;
            }

            return frequency;
        }

        private void RefreshSubscriptions(bool clearOnly = false)
        {
            if (subscribedProgressionManager != null)
            {
                subscribedProgressionManager.StateChanged -= HandleProgressionStateChanged;
                subscribedProgressionManager = null;
            }

            if (subscribedDialogueRunner != null)
            {
                subscribedDialogueRunner.ConversationStarted -= HandleDialogueStarted;
                subscribedDialogueRunner.ConversationEnded -= HandleDialogueEnded;
                subscribedDialogueRunner = null;
            }

            if (subscribedGeneratorInteractable != null)
            {
                subscribedGeneratorInteractable.StateChanged -= HandleGeneratorStateChanged;
                subscribedGeneratorInteractable = null;
            }

            if (clearOnly || !isActiveAndEnabled)
            {
                return;
            }

            if (progressionManager != null)
            {
                progressionManager.StateChanged += HandleProgressionStateChanged;
                subscribedProgressionManager = progressionManager;
            }

            if (dialogueRunner != null)
            {
                dialogueRunner.ConversationStarted += HandleDialogueStarted;
                dialogueRunner.ConversationEnded += HandleDialogueEnded;
                subscribedDialogueRunner = dialogueRunner;
            }

            if (generatorInteractable != null)
            {
                generatorInteractable.StateChanged += HandleGeneratorStateChanged;
                subscribedGeneratorInteractable = generatorInteractable;
            }
        }

        private void LogActiveEventIfChanged()
        {
            RadioEventData activeEvent = null;
            if (progressionManager != null && progressionManager.ActiveRadioEventsToday.Count > 0)
            {
                activeEvent = progressionManager.ActiveRadioEventsToday[0];
            }

            if (activeEvent == lastLoggedActiveEvent)
            {
                return;
            }

            lastLoggedActiveEvent = activeEvent;

            if (activeEvent == null)
            {
                Debug.Log($"RadioSystem has no active jam event during step {progressionManager?.CurrentObjectiveStep.ToString() ?? "Unknown"}.", this);
                return;
            }

            Debug.Log(
                $"RadioSystem active jam event is now '{activeEvent.EventName}' at {activeEvent.TargetFrequency:F1} FM during step {progressionManager?.CurrentObjectiveStep.ToString() ?? "Unknown"}.",
                this);
        }

        public bool IsCurrentFrequencyLockMatch(out RadioEventData activeEvent, out float targetFrequency)
        {
            activeEvent = ActiveJamEvent;
            targetFrequency = activeEvent != null ? activeEvent.TargetFrequency : 0f;

            if (activeEvent == null)
            {
                return false;
            }

            float effectiveTolerance = Mathf.Max(signalLockTolerance, (frequencyStep * 0.5f) + 0.005f);
            return IsFrequencyWithinLockWindow(currentFrequency, targetFrequency, effectiveTolerance);
        }

        public string GetDebugStateSummary()
        {
            bool isLockMatch = IsCurrentFrequencyLockMatch(out _, out float targetFrequency);
            return
                $"Internal Frequency: {currentFrequency:F3} FM\n" +
                $"Displayed Frequency: {DisplayedFrequency:F1} FM\n" +
                $"Scan Active: {(isAutoScanning ? "Yes" : "No")}\n" +
                $"Lock Match: {(isLockMatch ? "Yes" : "No")}";
        }

        private void LogLockMatchIfChanged()
        {
            bool isLockMatch = IsCurrentFrequencyLockMatch(out RadioEventData activeEvent, out float targetFrequency);
            if (isLockMatch == lastLoggedLockMatch)
            {
                return;
            }

            lastLoggedLockMatch = isLockMatch;
            Debug.Log(
                $"RadioSystem lock match changed to {(isLockMatch ? "true" : "false")} at {currentFrequency:F3} FM. Active event: {(activeEvent != null ? activeEvent.EventName : "None")} ({targetFrequency:F1} FM).",
                this);
        }

        private void LogFailedManualLock(string inputSource, RadioEventData activeEvent, float targetFrequency, float lockTolerance)
        {
            if (activeEvent == null)
            {
                return;
            }

            float distanceToTarget = Mathf.Abs(targetFrequency - currentFrequency);
            if (distanceToTarget > Mathf.Max(signalFadeRange, 0.5f))
            {
                return;
            }

            Debug.Log(
                $"RadioSystem manual tune '{inputSource}' at {currentFrequency:F3} FM did not lock. Active event: {(activeEvent != null ? activeEvent.EventName : "None")} at {targetFrequency:F1} FM. Effective display: {DisplayedFrequency:F1} FM. Tolerance: {lockTolerance:F3}.",
                this);
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

        private TMP_Text FindFrequencyDisplayText()
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TMP_Text fallback = null;

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text candidate = texts[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.gameObject.name, "FrequencyDisplay", System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }

                if (fallback == null &&
                    candidate.transform.parent != null &&
                    string.Equals(candidate.transform.parent.gameObject.name, "Screen", System.StringComparison.OrdinalIgnoreCase))
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private Renderer FindPoweredScreenRenderer()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidate = renderers[i];
                if (candidate != null &&
                    string.Equals(candidate.gameObject.name, "Screen", System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private AudioSource FindAudioSource(string objectName)
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource candidate = sources[i];
                if (candidate != null &&
                    string.Equals(candidate.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void SyncNearSignalLoopPlayback(bool shouldPlay)
        {
            if (nearSignalAudioSource == null)
            {
                return;
            }

            if (!shouldPlay || nearSignalAudioSource.clip == null)
            {
                if (nearSignalAudioSource.isPlaying)
                {
                    nearSignalAudioSource.Stop();
                }

                return;
            }

            if (!nearSignalAudioSource.isPlaying)
            {
                nearSignalAudioSource.Play();
            }
        }
    }
}
