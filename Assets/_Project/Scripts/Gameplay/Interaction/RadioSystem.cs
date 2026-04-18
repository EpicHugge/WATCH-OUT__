using UnityEngine;
using System.Collections.Generic;

namespace WatchOut
{
    [DisallowMultipleComponent]
    public class RadioSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProgressionManager progressionManager;
        [SerializeField] private DialogueRunner dialogueRunner;
        
        [Header("Audio")]
        [SerializeField] private AudioSource staticAudioSource;
        [SerializeField] private AudioSource broadcastAudioSource;
        [SerializeField] private float maxStaticVolume = 0.5f;
        [SerializeField] private float maxBroadcastVolume = 1f;

        [Header("Tuning Settings")]
        [SerializeField] private float minFrequency = 87.5f;
        [SerializeField] private float maxFrequency = 108.0f;
        [SerializeField] private float manualTuneSpeed = 1.0f;
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
        private RadioEventData currentLockedEvent;

        public float CurrentFrequency => currentFrequency;

        private void Start()
        {
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
            if (isIncreasing)
            {
                currentFrequency += manualTuneSpeed * Time.deltaTime;
                isAutoScanning = false;
            }
            else if (isDecreasing)
            {
                currentFrequency -= manualTuneSpeed * Time.deltaTime;
                isAutoScanning = false;
            }
            else if (isAutoScanning)
            {
                currentFrequency += autoScanSpeed * Time.deltaTime;
            }

            // Wrap frequencies around when passing min/max limits
            if (currentFrequency > maxFrequency) currentFrequency = minFrequency;
            if (currentFrequency < minFrequency) currentFrequency = maxFrequency;
        }

        private void HandleAudioAndEvents()
        {
            if (progressionManager == null) return;

            var activeEvents = progressionManager.ActiveRadioEventsToday;
            RadioEventData closestEvent = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < activeEvents.Count; i++)
            {
                RadioEventData radioEvent = activeEvents[i];
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
            if (value) isAutoScanning = false; // Interrupt scanning with manual tuning
        }

        public void SetDecreasing(bool value)
        {
            isDecreasing = value;
            if (value) isAutoScanning = false; // Interrupt scanning with manual tuning
        }

        public void ToggleAutoScan()
        {
            isAutoScanning = !isAutoScanning;
        }
    }
}