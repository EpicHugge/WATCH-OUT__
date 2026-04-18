using UnityEngine;

[CreateAssetMenu(fileName = "SpeakerPreset", menuName = "Dialogue/Speaker Preset")]
public sealed class SpeakerPreset : ScriptableObject
{
    [SerializeField] private string speakerDisplayName = string.Empty;
    [SerializeField] private Color defaultSpeakerNameColor = Color.white;
    [SerializeField] private Color defaultDialogueTextColor = Color.white;
    [SerializeField] private DialogueVoiceProfile defaultVoiceProfile;
    [SerializeField] private DialogueTextEffect defaultTextEffect = DialogueTextEffect.None;

    public string SpeakerDisplayName => speakerDisplayName;
    public Color DefaultSpeakerNameColor => defaultSpeakerNameColor;
    public Color DefaultDialogueTextColor => defaultDialogueTextColor;
    public DialogueVoiceProfile DefaultVoiceProfile => defaultVoiceProfile;
    public DialogueTextEffect DefaultTextEffect => defaultTextEffect;
}
