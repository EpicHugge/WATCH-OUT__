using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WatchOut
{
    [DisallowMultipleComponent]
    public sealed class RadioSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
        [SerializeField] private List<RadioEventData> radioEvents = new List<RadioEventData>();

        [Header("UI References")]
        [SerializeField] private TMP_Text frequencyDisplayText;
        [SerializeField] private Renderer poweredScreenRenderer;
        [SerializeField] private Color screenPoweredColor = new Color(0.12f, 0.36f, 0.18f, 1f);
        [SerializeField] private Color screenUnpoweredColor = new Color(0.02f, 0.04f, 0.03f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioSource staticAudioSource;
        [SerializeField] private AudioSource nearSignalAudioSource;
        [SerializeField] private AudioSource exactLockAudioSource;
        [SerializeField] private AudioClip staticLoopClip;
        [SerializeField] private AudioClip nearSignalLoopClip;
        [SerializeField] private AudioClip exactLockClip;
        [SerializeField] private float maxStaticVolume = 0.5f;
        [SerializeField] private float maxNearSignalVolume = 1f;

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
        [SerializeField] private float signalFadeRange = 0.5f;
        [SerializeField] private float signalLockTolerance = 0.05f;

        private readonly HashSet<RadioEventData> resolvedEvents = new HashSet<RadioEventData>();

        private float currentFrequency = 87.5f;
        private bool isIncreasing;
        private bool isDecreasing;
        private bool isAutoScanning;
        private float holdTimer;
        private float changeTimer;
        private RadioEventData currentLockedEvent;
        private MaterialPropertyBlock screenPropertyBlock;
        private DialogueRunner subscribedDialogueRunner;

        public float CurrentFrequency => currentFrequency;
        public float DisplayedFrequency => Mathf.Round(currentFrequency * 10f) / 10f;
        public bool IsAutoScanning => isAutoScanning;
        public IReadOnlyList<RadioEventData> RadioEvents => radioEvents;

        private void Awake()
        {
            ResolveReferences();
            RemoveMissingRadioEvents();
            currentFrequency = WrapFrequency(currentFrequency);
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
            UpdateDisplay();
            ConfigureAudioSources();
            RefreshPowerState();
        }

        private void Update()
        {
            HandleTuning();
            HandleAudioAndEvents();
        }

        public void SetIncreasing(bool value)
        {
            if (value && !IsRadioPowered())
            {
                return;
            }

            isIncreasing = value;
            if (!value)
            {
                return;
            }

            isAutoScanning = false;
            isDecreasing = false;
            StepFrequency(frequencyStep, true);
            holdTimer = 0f;
            changeTimer = 0f;
        }

        public void SetDecreasing(bool value)
        {
            if (value && !IsRadioPowered())
            {
                return;
            }

            isDecreasing = value;
            if (!value)
            {
                return;
            }

            isAutoScanning = false;
            isIncreasing = false;
            StepFrequency(-frequencyStep, true);
            holdTimer = 0f;
            changeTimer = 0f;
        }

        public void ToggleScan()
        {
            if (!IsRadioPowered())
            {
                return;
            }

            isAutoScanning = !isAutoScanning;
            if (isAutoScanning)
            {
                isIncreasing = false;
                isDecreasing = false;
            }
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
                        StepFrequency(isIncreasing ? frequencyStep : -frequencyStep, true);

                        if (currentLockedEvent != null)
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
                StepFrequency(autoScanSpeed * Time.deltaTime, false);
            }
        }

        private void HandleAudioAndEvents()
        {
            bool isPowered = IsRadioPowered();
            bool isDialogueRunning = dialogueRunner != null && dialogueRunner.IsRunning;

            if (!isPowered)
            {
                ResetSignalState();
                return;
            }

            if (isDialogueRunning)
            {
                MuteSignalAudio();
                return;
            }

            if (!TryGetClosestActiveEvent(out RadioEventData closestEvent, out float closestDistance))
            {
                ResetSignalState();
                return;
            }

            float fadeRange = Mathf.Max(signalFadeRange, 0.01f);
            float lockTolerance = Mathf.Max(signalLockTolerance, 0.001f);

            if (closestDistance <= fadeRange)
            {
                float signalStrength = 1f - (closestDistance / fadeRange);
                SetNearSignalClip(closestEvent.BroadcastAudio != null ? closestEvent.BroadcastAudio : nearSignalLoopClip);
                SyncNearSignalLoopPlayback(nearSignalAudioSource != null && nearSignalAudioSource.clip != null);

                if (staticAudioSource != null)
                {
                    staticAudioSource.volume = Mathf.Lerp(maxStaticVolume, 0f, signalStrength);
                }

                if (nearSignalAudioSource != null)
                {
                    nearSignalAudioSource.volume = Mathf.Lerp(0f, maxNearSignalVolume, signalStrength);
                }

                if (currentLockedEvent == null &&
                    !isAutoScanning &&
                    IsFrequencyWithinLockWindow(currentFrequency, closestEvent.TargetFrequency, lockTolerance))
                {
                    LockOnSignal(closestEvent);
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

            currentLockedEvent = radioEvent;
            isAutoScanning = false;
            isIncreasing = false;
            isDecreasing = false;
            currentFrequency = radioEvent.TargetFrequency;
            UpdateDisplay();

            if (exactLockAudioSource != null && exactLockClip != null)
            {
                exactLockAudioSource.PlayOneShot(exactLockClip);
            }

            if (radioEvent.DialogueConversation != null && dialogueRunner != null)
            {
                MuteSignalAudio();
                if (dialogueRunner.StartConversation(radioEvent.DialogueConversation))
                {
                    return;
                }
            }

            ResolveCurrentLockedEvent();
        }

        private void ResolveCurrentLockedEvent()
        {
            if (currentLockedEvent == null)
            {
                return;
            }

            if (currentLockedEvent.OneTimeOnly)
            {
                resolvedEvents.Add(currentLockedEvent);
            }

            currentLockedEvent = null;
            RefreshPowerState();
        }

        private void StepFrequency(float delta, bool snapToStep)
        {
            currentFrequency = WrapFrequency(currentFrequency + delta);

            if (snapToStep)
            {
                currentFrequency = Mathf.Round(currentFrequency * 10f) / 10f;
            }

            UpdateDisplay();
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
                return;
            }

            SyncStaticLoopPlayback(true);
            if (staticAudioSource != null)
            {
                staticAudioSource.volume = maxStaticVolume;
            }
        }

        private void ResetSignalState()
        {
            SyncStaticLoopPlayback(IsRadioPowered());

            if (staticAudioSource != null)
            {
                staticAudioSource.volume = IsRadioPowered() ? maxStaticVolume : 0f;
            }

            if (nearSignalAudioSource != null)
            {
                nearSignalAudioSource.volume = 0f;
            }

            SyncNearSignalLoopPlayback(false);
            currentLockedEvent = null;
        }

        private void MuteSignalAudio()
        {
            if (staticAudioSource != null)
            {
                staticAudioSource.volume = 0f;
            }

            if (nearSignalAudioSource != null)
            {
                nearSignalAudioSource.volume = 0f;
            }

            SyncNearSignalLoopPlayback(false);
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
            staticAudioSource = EnsureLoopSource(staticAudioSource, "Audio Static", staticLoopClip);
            nearSignalAudioSource = EnsureLoopSource(nearSignalAudioSource, "Audio Near Signal", nearSignalLoopClip);
            exactLockAudioSource = EnsureOneShotSource(exactLockAudioSource, "Audio Lock Cue");

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
            RemoveMissingRadioEvents();

            if (dialogueRunner == null)
            {
                dialogueRunner = FindAnyObjectByType<DialogueRunner>();
            }

            if (cassettePlayerReceiver == null)
            {
                cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
            }

            if (frequencyDisplayText == null)
            {
                frequencyDisplayText = FindFrequencyDisplayText();
            }

            if (poweredScreenRenderer == null)
            {
                poweredScreenRenderer = FindPoweredScreenRenderer();
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

        private void RemoveMissingRadioEvents()
        {
            if (radioEvents == null)
            {
                radioEvents = new List<RadioEventData>();
                return;
            }

            radioEvents.RemoveAll(candidate => candidate == null);
        }

        private bool TryGetClosestActiveEvent(out RadioEventData closestEvent, out float closestDistance)
        {
            closestEvent = null;
            closestDistance = float.MaxValue;
            CassetteData loadedCassette = cassettePlayerReceiver != null ? cassettePlayerReceiver.LoadedCassette : null;

            for (int i = 0; i < radioEvents.Count; i++)
            {
                RadioEventData candidate = radioEvents[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.OneTimeOnly && resolvedEvents.Contains(candidate))
                {
                    continue;
                }

                if (!candidate.AllowsCassette(loadedCassette))
                {
                    continue;
                }

                float distance = Mathf.Abs(candidate.TargetFrequency - currentFrequency);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEvent = candidate;
                }
            }

            return closestEvent != null;
        }

        private bool IsRadioPowered()
        {
            return true;
        }

        private void HandleDialogueStarted(DialogueConversation conversation)
        {
            MuteSignalAudio();
        }

        private void HandleDialogueEnded(DialogueConversation conversation)
        {
            if (currentLockedEvent == null || conversation == null)
            {
                return;
            }

            if (currentLockedEvent.DialogueConversation != conversation)
            {
                return;
            }

            ResolveCurrentLockedEvent();
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
            if (subscribedDialogueRunner != null)
            {
                subscribedDialogueRunner.ConversationStarted -= HandleDialogueStarted;
                subscribedDialogueRunner.ConversationEnded -= HandleDialogueEnded;
                subscribedDialogueRunner = null;
            }

            if (clearOnly || !isActiveAndEnabled)
            {
                return;
            }

            if (dialogueRunner != null)
            {
                dialogueRunner.ConversationStarted += HandleDialogueStarted;
                dialogueRunner.ConversationEnded += HandleDialogueEnded;
                subscribedDialogueRunner = dialogueRunner;
            }
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

        private void SetNearSignalClip(AudioClip clip)
        {
            if (nearSignalAudioSource == null)
            {
                return;
            }

            if (nearSignalAudioSource.clip == clip)
            {
                return;
            }

            nearSignalAudioSource.Stop();
            nearSignalAudioSource.clip = clip;
            nearSignalAudioSource.volume = 0f;
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
    }
}
