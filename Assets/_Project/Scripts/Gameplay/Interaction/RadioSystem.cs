using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace WatchOut
{
    [DisallowMultipleComponent]
    public class RadioSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProgressionManager progressionManager;
        [SerializeField] private DialogueRunner dialogueRunner;
        
        [Header("UI References")]
        [SerializeField] private TMP_Text frequencyDisplayText;

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
        private float holdTimer;
        private float changeTimer;
        private RadioEventData currentLockedEvent;

        public float CurrentFrequency => currentFrequency;

        private void Start()
        {
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
            
            if (progressionManager == null)
            {
                progressionManager = FindAnyObjectByType<ProgressionManager>();
            }
            if (dialogueRunner == null)
            {
                dialogueRunner = FindAnyObjectByType<DialogueRunner>();
            }
        }

        private void Update()
        {
            HandleTuning();
            HandleAudioAndEvents();
        }

        private void HandleTuning()
        {
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
            if (progressionManager == null || progressionManager.CurrentDayData == null)
            {
                if (staticAudioSource != null) staticAudioSource.volume = maxStaticVolume;
                if (broadcastAudioSource != null) broadcastAudioSource.volume = 0f;
                currentLockedEvent = null;
                return;
            }

            var allEvents = progressionManager.CurrentDayData.AvailableRadioEvents;
            CassetteData currentCassette = progressionManager.SelectedCassetteToday;

            RadioEventData closestEvent = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < allEvents.Count; i++)
            {
                RadioEventData radioEvent = allEvents[i];
                if (radioEvent == null) continue;

                if (!radioEvent.IsAvailableOnDay(progressionManager.CurrentDay)) continue;
                if (!radioEvent.AllowsCassette(currentCassette)) continue;

                bool foundToday = progressionManager.ResolvedRadioEventsToday.Contains(radioEvent);
                if (radioEvent.OneTimeOnly && progressionManager.CompletedOneTimeRadioEvents.Contains(radioEvent) && !foundToday) 
                {
                    continue;
                }

                float distance = Mathf.Abs(radioEvent.TargetFrequency - currentFrequency);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEvent = radioEvent;
                }
            }

            // Smoothly crossfade static and broadcast audio when inside the fade range
            if (closestEvent != null && closestDistance <= signalFadeRange)
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
                if (closestDistance <= signalLockTolerance && currentLockedEvent != closestEvent)
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
                // Out of range: full static, zero broadcast
                if (staticAudioSource != null) staticAudioSource.volume = maxStaticVolume;
                if (broadcastAudioSource != null) broadcastAudioSource.volume = 0f;
                currentLockedEvent = null;
            }
        }

        private void LockOnSignal(RadioEventData radioEvent)
        {
            currentLockedEvent = radioEvent;

            // Stop all tuning mechanisms to "lock" onto the channel
            isAutoScanning = false;
            isIncreasing = false;
            isDecreasing = false;

            // Snap the frequency exactly to the target for a clean UI reading and magnetic feel
            currentFrequency = radioEvent.TargetFrequency;
            UpdateDisplay();

            // Mark the event as found for progression, but don't block the dialogue
            progressionManager.MarkRadioEventFound(radioEvent);

            // Always start the conversation when locked on
            if (radioEvent.DialogueConversation != null && dialogueRunner != null)
            {
                dialogueRunner.StartConversation(radioEvent.DialogueConversation);
            }
        }

        public void SetIncreasing(bool value)
        {
            isIncreasing = value;
            if (value)
            {
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
            isDecreasing = value;
            if (value)
            {
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
            isAutoScanning = !isAutoScanning;
        }

        private void UpdateDisplay()
        {
            if (frequencyDisplayText != null)
            {
                frequencyDisplayText.text = currentFrequency.ToString("F1") + " FM";
            }
        }
    }
}