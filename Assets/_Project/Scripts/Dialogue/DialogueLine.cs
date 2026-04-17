using System;
using UnityEngine;

[Serializable]
public sealed class DialogueLine
{
    [SerializeField] private string speakerName = string.Empty;
    [SerializeField] [TextArea(2, 6)] private string dialogueText = string.Empty;
    [SerializeField] private DialogueTextEffect textEffect = DialogueTextEffect.None;
    [SerializeField] private bool overrideRevealSpeed;
    [SerializeField] [Min(1f)] private float charactersPerSecond = 60f;
    [SerializeField] private DialogueVoiceProfile voiceProfileOverride;

    public string SpeakerName => speakerName;
    public string DialogueText => dialogueText;
    public DialogueTextEffect TextEffect => textEffect;
    public bool OverrideRevealSpeed => overrideRevealSpeed;
    public float CharactersPerSecond => Mathf.Max(1f, charactersPerSecond);
    public DialogueVoiceProfile VoiceProfileOverride => voiceProfileOverride;
}
