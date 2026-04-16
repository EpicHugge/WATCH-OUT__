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
        [SerializeField] private float startingFrequency = 90.0f;
        [SerializeField] private float continuousChangeSpeed = 1.5f;
        [SerializeField] private float holdDelay = 0.5f;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI frequencyDisplayText;

        private float _currentFrequency;
        private bool _isIncreasing;
        private bool _isDecreasing;
        private float _holdTimer;

        private void Start()
        {
            _currentFrequency = Mathf.Clamp(startingFrequency, minFrequency, maxFrequency);
            UpdateDisplay();
        }

        private void Update()
        {
            if (_isIncreasing || _isDecreasing)
            {
                _holdTimer += Time.deltaTime;

                if (_holdTimer >= holdDelay)
                {
                    float direction = _isIncreasing ? 1f : -1f;
                    ApplyFrequencyChange(direction * continuousChangeSpeed * Time.deltaTime);
                }
            }
            else
            {
                _holdTimer = 0f;
            }
        }

        /// <summary>
        /// Increases the frequency by the defined step.
        /// </summary>
        public void IncreaseFrequency()
        {
            ApplyFrequencyChange(frequencyStep);
        }

        /// <summary>
        /// Decreases the frequency by the defined step.
        /// </summary>
        public void DecreaseFrequency()
        {
            ApplyFrequencyChange(-frequencyStep);
        }

        /// <summary>
        /// Sets whether the frequency is currently being increased (for hold functionality).
        /// </summary>
        public void SetIncreasing(bool state)
        {
            _isIncreasing = state;
            if (state)
            {
                IncreaseFrequency();
            }
            else
            {
                SnapFrequency();
            }
        }

        /// <summary>
        /// Sets whether the frequency is currently being decreased (for hold functionality).
        /// </summary>
        public void SetDecreasing(bool state)
        {
            _isDecreasing = state;
            if (state)
            {
                DecreaseFrequency();
            }
            else
            {
                SnapFrequency();
            }
        }

        private void ApplyFrequencyChange(float delta)
        {
            _currentFrequency = Mathf.Clamp(_currentFrequency + delta, minFrequency, maxFrequency);
            UpdateDisplay();
        }

        private void SnapFrequency()
        {
            _currentFrequency = Mathf.Round(_currentFrequency / frequencyStep) * frequencyStep;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (frequencyDisplayText != null)
            {
                // Formats to one decimal place (e.g., 101.5 MHz)
                frequencyDisplayText.text = $"{_currentFrequency:F1} MHz";
            }
        }

        // Property to access frequency from other systems (e.g., audio tuning)
        public float CurrentFrequency => _currentFrequency;
    }
}