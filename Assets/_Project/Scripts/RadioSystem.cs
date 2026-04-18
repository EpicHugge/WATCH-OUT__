using UnityEngine;
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

        private float _currentFrequency;
        private bool _isIncreasing;
        private bool _isDecreasing;
        private float _holdTimer;
        private float _changeTimer;

        public float CurrentFrequency => _currentFrequency;

        private void Start()
        {
            _currentFrequency = startingFrequency;
            UpdateDisplay();
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
        }

        public void SetIncreasing(bool isIncreasing)
        {
            _isIncreasing = isIncreasing;
            if (isIncreasing)
            {
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
                _isIncreasing = false;
                DecreaseFrequencyInternal(); // Trigger the first single tap immediately
                _holdTimer = 0f;
                _changeTimer = 0f;
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
