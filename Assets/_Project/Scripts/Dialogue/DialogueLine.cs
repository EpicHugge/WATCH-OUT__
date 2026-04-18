using System;
using UnityEngine;

[Serializable]
public sealed class DialogueLine
{
    [Header("Speaker")]
    [SerializeField] private SpeakerPreset speakerPreset;
    [SerializeField] private string speakerName = string.Empty;

    [Header("Text")]
    [SerializeField] [TextArea(2, 6)] private string dialogueText = string.Empty;
    [SerializeField] private bool overrideTextEffect;
    [SerializeField] private DialogueTextEffect textEffect = DialogueTextEffect.None;
    [SerializeField] private bool overrideSpeakerNameColor;
    [SerializeField] private Color speakerNameColor = Color.white;
    [SerializeField] private bool overrideDialogueTextColor;
    [SerializeField] private Color dialogueTextColor = Color.white;

    [Header("Reveal")]
    [SerializeField] private bool overrideRevealSpeed;
    [SerializeField] [Min(1f)] private float charactersPerSecond = 60f;

    [Header("Audio")]
    [SerializeField] private DialogueVoiceProfile voiceProfileOverride;

    [Header("Flow")]
    [SerializeField] private DialogueLineEvent lineEvent = new DialogueLineEvent();
    [SerializeField] private bool autoAdvance;
    [SerializeField] [Min(0f)] private float autoAdvanceDelay = 0.15f;

    public SpeakerPreset SpeakerPreset => speakerPreset;
    public string SpeakerName => !string.IsNullOrWhiteSpace(speakerName)
        ? speakerName
        : speakerPreset != null ? speakerPreset.SpeakerDisplayName : string.Empty;
    public string DialogueText => dialogueText;
    public DialogueTextEffect TextEffect => speakerPreset != null && !overrideTextEffect
        ? speakerPreset.DefaultTextEffect
        : textEffect;
    public Color SpeakerNameColor => speakerPreset != null && !overrideSpeakerNameColor
        ? speakerPreset.DefaultSpeakerNameColor
        : speakerNameColor;
    public Color DialogueTextColor => speakerPreset != null && !overrideDialogueTextColor
        ? speakerPreset.DefaultDialogueTextColor
        : dialogueTextColor;
    public bool OverrideRevealSpeed => overrideRevealSpeed;
    public float CharactersPerSecond => Mathf.Max(1f, charactersPerSecond);
    public DialogueVoiceProfile VoiceProfileOverride => voiceProfileOverride;
    public DialogueLineEvent LineEvent => lineEvent;
    public bool AutoAdvance => autoAdvance;
    public float AutoAdvanceDelay => Mathf.Max(0f, autoAdvanceDelay);
}
