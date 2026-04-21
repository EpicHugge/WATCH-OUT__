using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class HoverMoveInteractable : InteractableBase
{
    [Header("Hover Movement")]
    [SerializeField] private Transform objectToMove;
    [SerializeField] private Vector3 hoverOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private float moveSpeed = 10f;

    [Header("Hover Audio")]
    [SerializeField] private AudioSource hoverAudioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] [Min(0f)] private float hoverSoundVolume = 0.5f;
    [SerializeField] private float minHoverPitch = 0.92f;
    [SerializeField] private float maxHoverPitch = 1.08f;
    [SerializeField] [Range(0f, 1f)] private float hoverPitchNormalized = 0.5f;

    [Header("Interaction")]
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private UnityEvent onInteract;

    private Vector3 originalPosition;
    private bool isHovered;

    public Transform ObjectToMove => objectToMove;
    public UnityEvent OnInteractEvent => onInteract;

    protected override void Awake()
    {
        base.Awake();

        if (objectToMove == null)
        {
            objectToMove = transform;
        }

        originalPosition = objectToMove.localPosition;
        EnsureHoverAudioSource();
    }

    private void Update()
    {
        if (objectToMove == null) return;

        Vector3 targetPosition = isHovered ? originalPosition + hoverOffset : originalPosition;
        objectToMove.localPosition = Vector3.Lerp(objectToMove.localPosition, targetPosition, Time.deltaTime * moveSpeed);
    }

    protected override void OnHoverEnterInternal(PlayerInteractionController interactor)
    {
        isHovered = true;
        PlayHoverSound();
    }

    protected override void OnHoverExitInternal(PlayerInteractionController interactor)
    {
        isHovered = false;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked) return base.GetInteractionPrompt(interactor);
        return prompt;
    }

    public void SetPrompt(string value)
    {
        prompt = string.IsNullOrWhiteSpace(value) ? "Interact" : value;
    }

    public void SetHoverSlideDistance(float distance)
    {
        float signedDistance = Mathf.Sign(hoverOffset.z);
        if (Mathf.Approximately(signedDistance, 0f))
        {
            signedDistance = -1f;
        }

        hoverOffset.z = signedDistance * Mathf.Abs(distance);
    }

    public void ConfigureHoverAudio(AudioClip clip, float volume, float minPitch, float maxPitch, float normalizedPitch)
    {
        hoverSound = clip;
        hoverSoundVolume = Mathf.Max(0f, volume);
        minHoverPitch = Mathf.Min(minPitch, maxPitch);
        maxHoverPitch = Mathf.Max(minPitch, maxPitch);
        hoverPitchNormalized = Mathf.Clamp01(normalizedPitch);
        EnsureHoverAudioSource();
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        onInteract?.Invoke();
    }

    private void EnsureHoverAudioSource()
    {
        if (hoverSound == null)
        {
            return;
        }

        if (hoverAudioSource == null)
        {
            hoverAudioSource = GetComponent<AudioSource>();
        }

        if (hoverAudioSource == null)
        {
            hoverAudioSource = gameObject.AddComponent<AudioSource>();
        }

        hoverAudioSource.playOnAwake = false;
        hoverAudioSource.loop = false;
        hoverAudioSource.spatialBlend = 1f;
        hoverAudioSource.minDistance = 0.8f;
        hoverAudioSource.maxDistance = 6f;
        hoverAudioSource.dopplerLevel = 0f;
    }

    private void PlayHoverSound()
    {
        if (hoverSound == null)
        {
            return;
        }

        EnsureHoverAudioSource();
        if (hoverAudioSource == null)
        {
            return;
        }

        hoverAudioSource.pitch = Mathf.Lerp(minHoverPitch, maxHoverPitch, hoverPitchNormalized);
        hoverAudioSource.PlayOneShot(hoverSound, hoverSoundVolume);
    }
}
