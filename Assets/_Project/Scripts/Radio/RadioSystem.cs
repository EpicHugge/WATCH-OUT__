using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace WatchOut
{
    public class RadioSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float minFrequency = 88.0f;
        [SerializeField] private float maxFrequency = 108.0f;
        [SerializeField] private float frequencyStep = 0.1f;
        [SerializeField] private float startingFrequency = 88.0f;

        [Header("Continuous Tuning")]
        [SerializeField] private float continuousChangeSpeed = 10f;
        [SerializeField] private float holdDelay = 0.5f;

        [Header("UI References")]
        [SerializeField] private TMP_Text frequencyDisplayText;

        [Header("Audio & Events")]
        [SerializeField] private AudioSource staticAudioSource;
        [SerializeField] private AudioSource broadcastAudioSource;
        [SerializeField] private ProgressionManager progressionManager;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private float scanSpeed = 2.0f;
        [SerializeField] private float signalFadeRange = 1.0f;
        [SerializeField] private float signalLockTolerance = 0.05f;

        private float _currentFrequency;
        private bool _isIncreasing;
        private bool _isDecreasing;
        private bool _isScanning;
        private float _holdTimer;
        private float _changeTimer;
        private RadioEventData _currentLockedEvent;

        public float CurrentFrequency => _currentFrequency;

        private void Start()
        {
            _currentFrequency = startingFrequency;
            UpdateDisplay();

            if (staticAudioSource != null)
            {
                staticAudioSource.loop = true;
                staticAudioSource.volume = 1f;
                if (!staticAudioSource.isPlaying) staticAudioSource.Play();
            }

            if (broadcastAudioSource != null)
            {
                broadcastAudioSource.loop = true;
                broadcastAudioSource.volume = 0f;
            }

            if (progressionManager == null) progressionManager = FindAnyObjectByType<ProgressionManager>();
            if (dialogueRunner == null) dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        private void Update()
        {
            if (_isIncreasing || _isDecreasing)
            {
                _holdTimer += Time.deltaTime;

                // Wait for the initial delay before fast-scrubbing
                if (_holdTimer >= holdDelay)
                {
                    _changeTimer += Time.deltaTime;
                    float timePerStep = 1f / continuousChangeSpeed;

                    while (_changeTimer >= timePerStep)
                    {
                        _changeTimer -= timePerStep;
                        
                        if (_isIncreasing) IncreaseFrequencyInternal();
                        if (_isDecreasing) DecreaseFrequencyInternal();
                    }
                }
            }
            else
            {
                _holdTimer = 0f;
                _changeTimer = 0f;
            }

            if (_isScanning)
            {
                _currentFrequency += scanSpeed * Time.deltaTime;
                if (_currentFrequency > maxFrequency) _currentFrequency = minFrequency;
                UpdateDisplay();
            }

            HandleAudioAndEvents();
        }

        public void SetIncreasing(bool isIncreasing)
        {
            _isIncreasing = isIncreasing;
            if (isIncreasing)
            {
                _isScanning = false;
                _isDecreasing = false;
                IncreaseFrequencyInternal(); // Trigger the first single tap immediately
                _holdTimer = 0f;
                _changeTimer = 0f;
            }
        }

        public void SetDecreasing(bool isDecreasing)
        {
            _isDecreasing = isDecreasing;
            if (isDecreasing)
            {
                _isScanning = false;
                _isIncreasing = false;
                DecreaseFrequencyInternal(); // Trigger the first single tap immediately
                _holdTimer = 0f;
                _changeTimer = 0f;
            }
        }

        public void ToggleScan()
        {
            _isScanning = !_isScanning;
            if (_isScanning)
            {
                _isIncreasing = false;
                _isDecreasing = false;
            }
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
                float distance = Mathf.Abs(radioEvent.TargetFrequency - _currentFrequency);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEvent = radioEvent;
                }
            }

            if (closestEvent != null && closestDistance <= signalFadeRange)
            {
                float signalStrength = 1f - (closestDistance / signalFadeRange);
                
                if (staticAudioSource != null) staticAudioSource.volume = 1f - signalStrength;

                if (broadcastAudioSource != null)
                {
                    if (broadcastAudioSource.clip != closestEvent.BroadcastAudio)
                    {
                        broadcastAudioSource.clip = closestEvent.BroadcastAudio;
                        if (closestEvent.BroadcastAudio != null && !broadcastAudioSource.isPlaying)
                        {
                            broadcastAudioSource.Play();
                        }
                    }
                    broadcastAudioSource.volume = signalStrength;
                }

                if (closestDistance <= signalLockTolerance)
                {
                    if (_currentLockedEvent != closestEvent)
                    {
                        LockOnSignal(closestEvent);
                    }
                }
                else if (_currentLockedEvent == closestEvent)
                {
                    _currentLockedEvent = null;
                }
            }
            else
            {
                if (staticAudioSource != null) staticAudioSource.volume = 1f;
                if (broadcastAudioSource != null) broadcastAudioSource.volume = 0f;
                _currentLockedEvent = null;
            }
        }

        private void LockOnSignal(RadioEventData radioEvent)
        {
            _currentLockedEvent = radioEvent;
            _isScanning = false; // Stop scanning once the right channel is found

            if (progressionManager.MarkRadioEventFound(radioEvent))
            {
                if (dialogueRunner != null && radioEvent.DialogueConversation != null)
                {
                    dialogueRunner.StartConversation(radioEvent.DialogueConversation);
                }
            }
        }

        private void IncreaseFrequencyInternal()
        {
            _currentFrequency += frequencyStep;
            if (_currentFrequency > maxFrequency) _currentFrequency = minFrequency;
            UpdateDisplay();
        }

        private void DecreaseFrequencyInternal()
        {
            _currentFrequency -= frequencyStep;
            if (_currentFrequency < minFrequency) _currentFrequency = maxFrequency;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (frequencyDisplayText != null)
            {
                frequencyDisplayText.text = _currentFrequency.ToString("F1") + " FM";
            }
        }
    }
}
