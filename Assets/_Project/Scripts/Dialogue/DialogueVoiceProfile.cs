using UnityEngine;

[CreateAssetMenu(fileName = "DialogueVoiceProfile", menuName = "Dialogue/Voice Profile")]
public sealed class DialogueVoiceProfile : ScriptableObject
{
    [SerializeField] private AudioClip[] voiceClips;
    [SerializeField] [Min(0f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] [Min(1)] private int lettersPerSound = 2;
    [SerializeField] [Min(0f)] private float minimumTimeBetweenBleeps = 0.035f;
    [SerializeField] private bool ignoreWhitespace = true;
    [SerializeField] private bool ignorePunctuation = true;

    public AudioClip[] VoiceClips => voiceClips;
    public float Volume => volume;
    public float PitchMin => Mathf.Min(pitchRange.x, pitchRange.y);
    public float PitchMax => Mathf.Max(pitchRange.x, pitchRange.y);
    public int LettersPerSound => Mathf.Max(1, lettersPerSound);
    public float MinimumTimeBetweenBleeps => Mathf.Max(0f, minimumTimeBetweenBleeps);
    public bool IgnoreWhitespace => ignoreWhitespace;
    public bool IgnorePunctuation => ignorePunctuation;

    public bool HasVoiceClips
    {
        get
        {
            if (voiceClips == null)
            {
                return false;
            }

            for (int i = 0; i < voiceClips.Length; i++)
            {
                if (voiceClips[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
