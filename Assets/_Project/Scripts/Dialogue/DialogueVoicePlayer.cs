using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueVoicePlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private DialogueVoiceProfile activeProfile;
    private float lastBleepTime = float.MinValue;
    private int lettersSinceLastBleep;

    private void Awake()
    {
        EnsureAudioSource();
    }

    public void BeginLine(DialogueVoiceProfile voiceProfile)
    {
        activeProfile = voiceProfile;
        lettersSinceLastBleep = activeProfile != null ? activeProfile.LettersPerSound - 1 : 0;
        lastBleepTime = float.MinValue;
    }

    public void ProcessRevealedCharacter(char character)
    {
        if (activeProfile == null || !activeProfile.HasVoiceClips)
        {
            return;
        }

        if (activeProfile.IgnoreWhitespace && char.IsWhiteSpace(character))
        {
            return;
        }

        if (activeProfile.IgnorePunctuation && char.IsPunctuation(character))
        {
            return;
        }

        lettersSinceLastBleep++;
        if (lettersSinceLastBleep < activeProfile.LettersPerSound)
        {
            return;
        }

        if (Time.unscaledTime - lastBleepTime < activeProfile.MinimumTimeBetweenBleeps)
        {
            return;
        }

        PlayBleep();
        lettersSinceLastBleep = 0;
    }

    public void StopPlayback()
    {
        activeProfile = null;
        lettersSinceLastBleep = 0;

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void PlayBleep()
    {
        EnsureAudioSource();

        AudioClip[] voiceClips = activeProfile.VoiceClips;
        AudioClip selectedClip = GetRandomClip(voiceClips);
        if (selectedClip == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(activeProfile.PitchMin, activeProfile.PitchMax);
        audioSource.volume = activeProfile.Volume;
        audioSource.PlayOneShot(selectedClip, activeProfile.Volume);
        lastBleepTime = Time.unscaledTime;
    }

    private static AudioClip GetRandomClip(AudioClip[] voiceClips)
    {
        if (voiceClips == null || voiceClips.Length == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, voiceClips.Length);
        for (int i = 0; i < voiceClips.Length; i++)
        {
            AudioClip clip = voiceClips[(startIndex + i) % voiceClips.Length];
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }
}
