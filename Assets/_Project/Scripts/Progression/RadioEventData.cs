using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EVT_", menuName = "Progression/Radio Event Data")]
public sealed class RadioEventData : ScriptableObject
{
    [Header("Event")]
    [SerializeField] private string eventName = "New Radio Event";
    [SerializeField] [Min(0f)] private float targetFrequency = 94.3f;
    [SerializeField] private DialogueConversation dialogueConversation;

    [Header("Availability")]
    [SerializeField] [Min(1)] private int minDay = 1;
    [SerializeField] [Min(0)] private int maxDay;
    [SerializeField] private List<CassetteData> allowedCassettes = new List<CassetteData>();
    [SerializeField] private bool oneTimeOnly = true;

    [Header("Notes")]
    [SerializeField] [TextArea(2, 4)] private string debugNotes = string.Empty;

    public string EventName => string.IsNullOrWhiteSpace(eventName) ? name : eventName.Trim();
    public float TargetFrequency => targetFrequency;
    public DialogueConversation DialogueConversation => dialogueConversation;
    public int MinDay => Mathf.Max(1, minDay);
    public int MaxDay => Mathf.Max(0, maxDay);
    public IReadOnlyList<CassetteData> AllowedCassettes => allowedCassettes;
    public bool OneTimeOnly => oneTimeOnly;
    public string DebugNotes => debugNotes;

    public bool IsAvailableOnDay(int dayNumber)
    {
        int normalizedDay = Mathf.Max(1, dayNumber);
        if (normalizedDay < MinDay)
        {
            return false;
        }

        return MaxDay <= 0 || normalizedDay <= MaxDay;
    }

    public bool AllowsCassette(CassetteData cassette)
    {
        if (allowedCassettes == null || allowedCassettes.Count == 0)
        {
            return true;
        }

        if (cassette == null)
        {
            return false;
        }

        return allowedCassettes.Contains(cassette);
    }

    public List<string> GetValidationMessages()
    {
        List<string> messages = new List<string>();

        if (dialogueConversation == null)
        {
            messages.Add("This radio event has no dialogue conversation assigned.");
        }

        if (maxDay > 0 && maxDay < minDay)
        {
            messages.Add("Max Day is lower than Min Day.");
        }

        return messages;
    }

    private void OnValidate()
    {
        minDay = Mathf.Max(1, minDay);
        maxDay = Mathf.Max(0, maxDay);
        targetFrequency = Mathf.Max(0f, targetFrequency);
    }
}
