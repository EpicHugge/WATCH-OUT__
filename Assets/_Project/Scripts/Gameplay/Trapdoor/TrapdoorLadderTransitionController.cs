using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrapdoorLadderTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonPlayerController playerController;
    [SerializeField] private PlayerInteractionController interactionController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private SleepTransitionUI transitionUI;

    [Header("Timing")]
    [SerializeField] [Min(0f)] private float fadeOutDuration = 0.18f;
    [SerializeField] [Min(0f)] private float blackoutHoldDuration = 0.06f;
    [SerializeField] [Min(0f)] private float fadeInDuration = 0.18f;

    private Coroutine activeTransition;
    private bool restoreInteractionEnabled;
    private bool restorePlayerControllerEnabled;
    private bool restoreCharacterControllerEnabled;

    public bool IsTransitionRunning => activeTransition != null;

    public bool TryStartTransition(Transform destination)
    {
        if (destination == null || IsTransitionRunning)
        {
            return false;
        }

        EnsureReferences();
        activeTransition = StartCoroutine(TransitionRoutine(destination));
        return true;
    }

    private IEnumerator TransitionRoutine(Transform destination)
    {
        DisablePlayerControl();
        EnsureTransitionUIVisible();

        yield return AnimateFade(0f, 1f, fadeOutDuration);

        if (blackoutHoldDuration > 0f)
        {
            yield return WaitUnscaled(blackoutHoldDuration);
        }

        TeleportPlayer(destination);

        if (blackoutHoldDuration > 0f)
        {
            yield return WaitUnscaled(blackoutHoldDuration);
        }

        yield return AnimateFade(1f, 0f, fadeInDuration);

        if (transitionUI != null)
        {
            transitionUI.SetVisible(false);
        }

        RestorePlayerControl();
        activeTransition = null;
    }

    private void DisablePlayerControl()
    {
        if (interactionController != null)
        {
            restoreInteractionEnabled = interactionController.InteractionEnabled;
            interactionController.SetInteractionEnabled(false);
        }

        if (playerController != null)
        {
            restorePlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (characterController != null)
        {
            restoreCharacterControllerEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void RestorePlayerControl()
    {
        if (characterController != null)
        {
            characterController.enabled = restoreCharacterControllerEnabled;
        }

        if (playerController != null)
        {
            playerController.enabled = restorePlayerControllerEnabled;
        }

        if (interactionController != null)
        {
            interactionController.SetInteractionEnabled(restoreInteractionEnabled);
            interactionController.RefreshCurrentTarget();
        }
    }

    private IEnumerator AnimateFade(float from, float to, float duration)
    {
        if (transitionUI == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            transitionUI.SetClosedAmount(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transitionUI.SetClosedAmount(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        transitionUI.SetClosedAmount(to);
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void TeleportPlayer(Transform destination)
    {
        transform.SetPositionAndRotation(destination.position, destination.rotation);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.identity;
        }
    }

    private void EnsureTransitionUIVisible()
    {
        if (transitionUI == null)
        {
            transitionUI = GetComponentInChildren<SleepTransitionUI>(true);
        }

        if (transitionUI == null)
        {
            transitionUI = SleepTransitionUI.Create(transform);
        }

        transitionUI.SetVisible(true);
        transitionUI.SetClosedAmount(0f);
        transitionUI.ShowNextDayText(false);
    }

    private void EnsureReferences()
    {
        if (playerController == null)
        {
            playerController = GetComponent<FirstPersonPlayerController>();
        }

        if (interactionController == null)
        {
            interactionController = GetComponent<PlayerInteractionController>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (cameraPivot == null && playerController != null)
        {
            cameraPivot = playerController.CameraPivot;
        }

        if (cameraPivot == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                cameraPivot = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
            }
        }
    }
}
