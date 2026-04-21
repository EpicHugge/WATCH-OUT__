using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EVT_", menuName = "Radio/Radio Event Data")]
public sealed class RadioEventData : ScriptableObject
{
    [Header("Event")]
    [SerializeField] private string eventName = "New Radio Event";
    [SerializeField] [Min(0f)] private float targetFrequency = 94.3f;
    [SerializeField] private DialogueConversation dialogueConversation;
    [SerializeField] private AudioClip broadcastAudio;

    [Header("Restrictions")]
    [SerializeField] private List<CassetteData> allowedCassettes = new List<CassetteData>();
    [SerializeField] private bool oneTimeOnly = true;

    [Header("Notes")]
    [SerializeField] [TextArea(2, 4)] private string debugNotes = string.Empty;

    public string EventName => string.IsNullOrWhiteSpace(eventName) ? name : eventName.Trim();
    public float TargetFrequency => targetFrequency;
    public DialogueConversation DialogueConversation => dialogueConversation;
    public AudioClip BroadcastAudio => broadcastAudio;
    public IReadOnlyList<CassetteData> AllowedCassettes => allowedCassettes;
    public bool OneTimeOnly => oneTimeOnly;
    public string DebugNotes => debugNotes;

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

        return messages;
    }

    private void OnValidate()
    {
        targetFrequency = Mathf.Max(0f, targetFrequency);
    }
}
