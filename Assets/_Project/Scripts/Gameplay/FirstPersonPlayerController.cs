using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public sealed class FirstPersonPlayerController : MonoBehaviour
{
    private const string CrosshairCanvasName = "CrosshairCanvas";
    private const string CrosshairName = "CircleCrosshair";

    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.1f;
    [SerializeField] private float sprintSpeed = 3.2f;
    [SerializeField] private float strafeSpeedMultiplier = 0.82f;
    [SerializeField] private float backwardSpeedMultiplier = 0.72f;
    [SerializeField] private float movementSmoothTime = 0.22f;
    [SerializeField] private float airMovementSmoothTime = 0.38f;
    [SerializeField] private float jumpHeight = 0.9f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.085f;
    [SerializeField] private float gamepadSensitivity = 115f;
    [SerializeField] private float lookSmoothTime = 0.08f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController characterController;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private Vector3 currentPlanarVelocity;
    private Vector3 currentPlanarVelocitySmoothVelocity;
    private float verticalVelocity;
    private float currentYaw;
    private float targetYaw;
    private float currentPitch;
    private float targetPitch;
    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;
    private bool cursorLocked = true;

    public Transform CameraPivot => cameraPivot;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (cameraPivot == null)
        {
            Camera fallbackCamera = GetComponentInChildren<Camera>();

            if (fallbackCamera != null)
            {
                cameraPivot = fallbackCamera.transform;
            }
        }

        if (GetComponent<PlayerInteractionController>() == null)
        {
            gameObject.AddComponent<PlayerInteractionController>();
        }

        if (GetComponent<DialogueRunner>() == null)
        {
            gameObject.AddComponent<DialogueRunner>();
        }

        ApplyLegacyTuningIfNeeded();
        EnsureCrosshair();
        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;
        currentPitch = GetSignedAngle(cameraPivot != null ? cameraPivot.localEulerAngles.x : 0f);
        targetPitch = currentPitch;
        ApplyLookRotation();
    }

    private void OnEnable()
    {
        CacheActions();
        SetCursorState(true);
    }

    private void OnDisable()
    {
        SetCursorState(false);
    }

    private void Update()
    {
        if (cameraPivot == null || moveAction == null || lookAction == null)
        {
            return;
        }

        UpdateCursorState();
        HandleLook();
        HandleMovement();
    }

    private void CacheActions()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 localMove = new Vector3(
            moveInput.x * strafeSpeedMultiplier,
            0f,
            moveInput.y < 0f ? moveInput.y * backwardSpeedMultiplier : moveInput.y);

        localMove = Vector3.ClampMagnitude(localMove, 1f);

        float currentSpeed = sprintAction != null && sprintAction.IsPressed() && localMove.z > 0.1f ? sprintSpeed : walkSpeed;
        Vector3 targetPlanarVelocity = transform.TransformDirection(localMove) * currentSpeed;
        float smoothTime = characterController.isGrounded ? movementSmoothTime : airMovementSmoothTime;

        currentPlanarVelocity = Vector3.SmoothDamp(
            currentPlanarVelocity,
            targetPlanarVelocity,
            ref currentPlanarVelocitySmoothVelocity,
            smoothTime);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        if (jumpAction != null && jumpAction.WasPressedThisFrame() && characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = currentPlanarVelocity + (Vector3.up * verticalVelocity);
        characterController.Move(motion * Time.deltaTime);
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        bool usingGamepad = playerInput.currentControlScheme == "Gamepad";

        if (!cursorLocked && !usingGamepad)
        {
            return;
        }

        float sensitivity = usingGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        targetYaw += lookInput.x * sensitivity;
        targetPitch = Mathf.Clamp(targetPitch - (lookInput.y * sensitivity), minPitch, maxPitch);

        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawSmoothVelocity, lookSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchSmoothVelocity, lookSmoothTime);

        ApplyLookRotation();
    }

    private void UpdateCursorState()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorState(false);
            return;
        }

        if (!cursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorState(true);
        }
    }

    private void EnsureCrosshair()
    {
        Canvas canvas = GetOrCreateCrosshairCanvas();
        CircleCrosshairGraphic crosshair = GetOrCreateCrosshair(canvas.transform);

        crosshair.color = Color.white;
        crosshair.raycastTarget = false;
    }

    private Canvas GetOrCreateCrosshairCanvas()
    {
        Transform existingCanvas = transform.Find(CrosshairCanvasName);
        GameObject canvasObject;

        if (existingCanvas != null)
        {
            canvasObject = existingCanvas.gameObject;
        }
        else
        {
            canvasObject = new GameObject(CrosshairCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
        }

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
        GetOrAddComponent<GraphicRaycaster>(canvasObject);

        canvasRect.localScale = Vector3.one;
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.sizeDelta = Vector2.zero;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        return canvas;
    }

    private CircleCrosshairGraphic GetOrCreateCrosshair(Transform canvasTransform)
    {
        Transform existingCrosshair = canvasTransform.Find(CrosshairName);
        GameObject crosshairObject;

        if (existingCrosshair != null)
        {
            crosshairObject = existingCrosshair.gameObject;
        }
        else
        {
            crosshairObject = new GameObject(CrosshairName, typeof(RectTransform), typeof(CanvasRenderer), typeof(CircleCrosshairGraphic));
            crosshairObject.transform.SetParent(canvasTransform, false);
        }

        RectTransform crosshairRect = crosshairObject.GetComponent<RectTransform>();
        GetOrAddComponent<CanvasRenderer>(crosshairObject);
        CircleCrosshairGraphic crosshair = GetOrAddComponent<CircleCrosshairGraphic>(crosshairObject);

        crosshairRect.localScale = Vector3.one;
        crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRect.pivot = new Vector2(0.5f, 0.5f);
        crosshairRect.anchoredPosition = Vector2.zero;
        crosshairRect.sizeDelta = new Vector2(24f, 24f);

        return crosshair;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private void ApplyLookRotation()
    {
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }

    private static float GetSignedAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void SetCursorState(bool shouldLock)
    {
        cursorLocked = shouldLock;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void ApplyLegacyTuningIfNeeded()
    {
        bool isLegacyTuning =
            Mathf.Approximately(walkSpeed, 5f) &&
            Mathf.Approximately(sprintSpeed, 8f) &&
            Mathf.Approximately(jumpHeight, 1.2f) &&
            Mathf.Approximately(gravity, -25f) &&
            Mathf.Approximately(mouseSensitivity, 0.15f) &&
            Mathf.Approximately(gamepadSensitivity, 180f) &&
            Mathf.Approximately(minPitch, -85f) &&
            Mathf.Approximately(maxPitch, 85f);

        if (!isLegacyTuning)
        {
            return;
        }

        walkSpeed = 2.1f;
        sprintSpeed = 3.2f;
        strafeSpeedMultiplier = 0.82f;
        backwardSpeedMultiplier = 0.72f;
        movementSmoothTime = 0.22f;
        airMovementSmoothTime = 0.38f;
        jumpHeight = 0.9f;
        gravity = -28f;
        groundedGravity = -2f;
        mouseSensitivity = 0.085f;
        gamepadSensitivity = 115f;
        lookSmoothTime = 0.08f;
        minPitch = -80f;
        maxPitch = 80f;
    }
}
