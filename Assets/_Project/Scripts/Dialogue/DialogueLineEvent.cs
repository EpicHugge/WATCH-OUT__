using System;
using UnityEngine;

[Serializable]
public sealed class DialogueLineEvent
{
    [SerializeField] private DialogueLineEventType eventType = DialogueLineEventType.None;
    [SerializeField] private DialogueLineEventTiming triggerTiming = DialogueLineEventTiming.OnLineStart;
    [SerializeField] [Min(0f)] private float duration = 0.2f;
    [SerializeField] [Min(0f)] private float magnitude = 0.08f;
    [SerializeField] [Min(1f)] private float frequency = 24f;

    public DialogueLineEventType EventType => eventType;
    public DialogueLineEventTiming TriggerTiming => triggerTiming;
    public float Duration => Mathf.Max(0f, duration);
    public float Magnitude => Mathf.Max(0f, magnitude);
    public float Frequency => Mathf.Max(1f, frequency);
    public bool IsConfigured => eventType != DialogueLineEventType.None;

    public bool TriggersAt(DialogueLineEventTiming timing)
    {
        return IsConfigured && triggerTiming == timing;
    }
}

public enum DialogueLineEventType
{
    [InspectorName("None")]
    None,
    [InspectorName("Camera Shake")]
    CameraShake
}

public enum DialogueLineEventTiming
{
    [InspectorName("On Line Start")]
    OnLineStart,
    [InspectorName("After Typing Finishes")]
    OnLineComplete
}
