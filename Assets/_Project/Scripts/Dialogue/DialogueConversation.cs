using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueConversation", menuName = "Dialogue/Conversation")]
public sealed class DialogueConversation : ScriptableObject
{
    [Header("Defaults")]
    [SerializeField] private bool overrideRevealSpeed;
    [SerializeField] [Min(1f)] private float charactersPerSecond = 60f;
    [SerializeField] private DialogueVoiceProfile defaultVoiceProfile;

    [Header("Voice Mapping")]
    [SerializeField] private List<DialogueSpeakerVoiceBinding> speakerVoices = new List<DialogueSpeakerVoiceBinding>();

    [Header("Lines")]
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public IReadOnlyList<DialogueLine> Lines => lines;
    public int LineCount => lines != null ? lines.Count : 0;
    public bool OverrideRevealSpeed => overrideRevealSpeed;
    public float CharactersPerSecond => Mathf.Max(1f, charactersPerSecond);

    public DialogueVoiceProfile ResolveVoiceProfile(DialogueLine line)
    {
        if (line == null)
        {
            return null;
        }

        if (line.VoiceProfileOverride != null)
        {
            return line.VoiceProfileOverride;
        }

        string speakerName = line.SpeakerName;
        if (string.IsNullOrWhiteSpace(speakerName) || speakerVoices == null)
        {
            return defaultVoiceProfile;
        }

        for (int i = 0; i < speakerVoices.Count; i++)
        {
            DialogueSpeakerVoiceBinding binding = speakerVoices[i];
            if (binding != null && binding.MatchesSpeaker(speakerName))
            {
                return binding.VoiceProfile;
            }
        }

        return defaultVoiceProfile;
    }

    public float ResolveCharactersPerSecond(DialogueLine line, float fallbackCharactersPerSecond)
    {
        if (line != null && line.OverrideRevealSpeed)
        {
            return line.CharactersPerSecond;
        }

        if (overrideRevealSpeed)
        {
            return CharactersPerSecond;
        }

        return Mathf.Max(1f, fallbackCharactersPerSecond);
    }

    [Serializable]
    private sealed class DialogueSpeakerVoiceBinding
    {
        [SerializeField] private string speakerName = string.Empty;
        [SerializeField] private DialogueVoiceProfile voiceProfile;

        public DialogueVoiceProfile VoiceProfile => voiceProfile;

        public bool MatchesSpeaker(string candidateSpeakerName)
        {
            return !string.IsNullOrWhiteSpace(speakerName) &&
                   string.Equals(speakerName.Trim(), candidateSpeakerName.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
