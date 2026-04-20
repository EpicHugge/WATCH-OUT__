using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DAY_", menuName = "Progression/Day Data")]
public sealed class DayData : ScriptableObject
{
    [Header("Day")]
    [SerializeField] [Min(1)] private int dayNumber = 1;
    [SerializeField] private List<CassetteData> availableCassettes = new List<CassetteData>();
    [SerializeField] private List<RadioEventData> availableRadioEvents = new List<RadioEventData>();

    [Header("Dialogue")]
    [SerializeField] private DialogueConversation dayStartDialogue;
    [SerializeField] private DialogueConversation dayEndDialogue;

    [Header("Notes")]
    [SerializeField] [TextArea(2, 4)] private string debugDescription = string.Empty;

    public int DayNumber => Mathf.Max(1, dayNumber);
    public IReadOnlyList<CassetteData> AvailableCassettes => availableCassettes;
    public IReadOnlyList<RadioEventData> AvailableRadioEvents => availableRadioEvents;
    public DialogueConversation DayStartDialogue => dayStartDialogue;
    public DialogueConversation DayEndDialogue => dayEndDialogue;
    public string DebugDescription => debugDescription;

    public bool ContainsCassette(CassetteData cassette)
    {
        return cassette != null && availableCassettes != null && availableCassettes.Contains(cassette);
    }

    public List<string> GetValidationMessages()
    {
        List<string> messages = new List<string>();

        if (availableCassettes == null || availableCassettes.Count == 0)
        {
            messages.Add("This day has no cassettes assigned.");
        }

        if (availableRadioEvents == null || availableRadioEvents.Count == 0)
        {
            messages.Add("This day has no radio events assigned.");
        }

        if (availableRadioEvents != null)
        {
            Dictionary<int, RadioEventData> frequencies = new Dictionary<int, RadioEventData>();

            for (int i = 0; i < availableRadioEvents.Count; i++)
            {
                RadioEventData radioEvent = availableRadioEvents[i];
                if (radioEvent == null)
                {
                    messages.Add($"Radio event slot {i + 1} is empty.");
                    continue;
                }

                if (radioEvent.DialogueConversation == null)
                {
                    messages.Add($"Radio event '{radioEvent.EventName}' has no dialogue conversation assigned.");
                }

                int roundedFrequency = Mathf.RoundToInt(radioEvent.TargetFrequency * 10f);
                if (frequencies.TryGetValue(roundedFrequency, out RadioEventData existingEvent))
                {
                    messages.Add(
                        $"Duplicate target frequency {radioEvent.TargetFrequency:F1} FM between '{existingEvent.EventName}' and '{radioEvent.EventName}'.");
                }
                else
                {
                    frequencies.Add(roundedFrequency, radioEvent);
                }
            }
        }

        return messages;
    }

    private void OnValidate()
    {
        dayNumber = Mathf.Max(1, dayNumber);
    }
}
