using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class SleepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonPlayerController playerController;
    [SerializeField] private PlayerInteractionController interactionController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private SleepTransitionUI transitionUI;

    [Header("Timing")]
    [SerializeField] [Min(0f)] private float moveIntoBedDuration = 0.9f;
    [SerializeField] [Min(0f)] private float eyeCloseDuration = 0.55f;
    [SerializeField] [Min(0f)] private float nextDayTextDelay = 0.15f;
    [SerializeField] [Min(0f)] private float nextDayHoldDuration = 0.9f;
    [SerializeField] [Min(0f)] private float moveOutOfBedDuration = 1.05f;
    [SerializeField] [Min(0f)] private float eyeOpenDuration = 0.55f;

    [Header("Events")]
    [SerializeField] private UnityEvent onAdvanceDay;

    private Coroutine activeSequence;
    private SleepBedInteractable activeBed;
    private bool restoreInteractionEnabled;
    private bool restorePlayerControllerEnabled;
    private bool restoreCharacterControllerEnabled;

    public bool IsSequenceRunning => activeSequence != null;

    public bool TryStartSleepSequence(SleepBedInteractable bed)
    {
        if (bed == null || bed.SleepPoint == null || IsSequenceRunning)
        {
            return false;
        }

        EnsureReferences();
        activeBed = bed;
        activeSequence = StartCoroutine(SleepSequenceRoutine(bed));
        return true;
    }

    private IEnumerator SleepSequenceRoutine(SleepBedInteractable bed)
    {
        Vector3 originalRootPosition = transform.position;
        Quaternion originalRootRotation = transform.rotation;
        Quaternion originalCameraLocalRotation = cameraPivot != null ? cameraPivot.localRotation : Quaternion.identity;

        DisablePlayerControl();
        EnsureTransitionUIVisible();

        GetSleepTargetPose(bed.SleepPoint, out Vector3 sleepRootPosition, out Quaternion sleepRootRotation);

        yield return MovePlayer(transform.position, transform.rotation, cameraPivot != null ? cameraPivot.localRotation : Quaternion.identity, sleepRootPosition, sleepRootRotation, Quaternion.identity, moveIntoBedDuration);
        yield return AnimateEyes(0f, 1f, eyeCloseDuration);

        if (nextDayTextDelay > 0f)
        {
            yield return WaitUnscaled(nextDayTextDelay);
        }

        transitionUI.ShowNextDayText(true);
        AdvanceDay();

        if (nextDayHoldDuration > 0f)
        {
            yield return WaitUnscaled(nextDayHoldDuration);
        }

        transitionUI.ShowNextDayText(false);

        Vector3 wakeRootPosition = originalRootPosition;
        Quaternion wakeRootRotation = originalRootRotation;
        Quaternion wakeCameraLocalRotation = originalCameraLocalRotation;

        if (bed.StandUpPoint != null)
        {
            wakeRootPosition = bed.StandUpPoint.position;
            wakeRootRotation = bed.StandUpPoint.rotation;
            wakeCameraLocalRotation = Quaternion.identity;
        }

        Coroutine openEyesRoutine = StartCoroutine(AnimateEyes(1f, 0f, eyeOpenDuration));
        yield return MovePlayer(transform.position, transform.rotation, cameraPivot != null ? cameraPivot.localRotation : Quaternion.identity, wakeRootPosition, wakeRootRotation, wakeCameraLocalRotation, moveOutOfBedDuration);
        yield return openEyesRoutine;

        transitionUI.SetVisible(false);
        RestorePlayerControl();

        SleepBedInteractable completedBed = activeBed;
        activeBed = null;
        activeSequence = null;
        completedBed?.NotifySleepSequenceFinished();
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

    private IEnumerator MovePlayer(
        Vector3 startRootPosition,
        Quaternion startRootRotation,
        Quaternion startCameraLocalRotation,
        Vector3 targetRootPosition,
        Quaternion targetRootRotation,
        Quaternion targetCameraLocalRotation,
        float duration)
    {
        if (duration <= 0f)
        {
            transform.SetPositionAndRotation(targetRootPosition, targetRootRotation);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = targetCameraLocalRotation;
            }

            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            transform.SetPositionAndRotation(
                Vector3.Lerp(startRootPosition, targetRootPosition, eased),
                Quaternion.Slerp(startRootRotation, targetRootRotation, eased));

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Slerp(startCameraLocalRotation, targetCameraLocalRotation, eased);
            }

            yield return null;
        }

        transform.SetPositionAndRotation(targetRootPosition, targetRootRotation);
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = targetCameraLocalRotation;
        }
    }

    private IEnumerator AnimateEyes(float from, float to, float duration)
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

    private void GetSleepTargetPose(Transform sleepPoint, out Vector3 rootPosition, out Quaternion rootRotation)
    {
        Quaternion targetRotation = sleepPoint.rotation;
        Vector3 cameraLocalOffset = cameraPivot != null ? cameraPivot.localPosition : Vector3.zero;
        rootRotation = targetRotation;
        rootPosition = sleepPoint.position - (targetRotation * cameraLocalOffset);
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

    private void AdvanceDay()
    {
        onAdvanceDay?.Invoke();
    }
}
