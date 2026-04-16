using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class FirstPersonPlayerPrefabBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Player/FirstPersonPlayer.prefab";
    private const string InputActionsPath = "Assets/_Project/Input/InputSystem_Actions.inputactions";
    private const string ProjectFontPath = "Assets/_Project/UI/Fonts/VCR_OSD_MONO_1.001.ttf";
    private static bool buildQueued;

    [MenuItem("Tools/Build First Person Player Prefab")]
    public static void BuildPrefab()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueuePrefabBuild();
            return;
        }

        EnsureFolder("Assets/_Project/Prefabs", "Player");

        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions == null)
        {
            Debug.LogError($"First person player prefab build failed. Missing input actions at {InputActionsPath}.");
            return;
        }

        Font projectFont = AssetDatabase.LoadAssetAtPath<Font>(ProjectFontPath);

        GameObject root = new GameObject("FirstPersonPlayer");

        try
        {
            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;
            characterController.minMoveDistance = 0f;
            characterController.skinWidth = 0.08f;

            PlayerInput playerInput = root.AddComponent<PlayerInput>();

            GameObject cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform, false);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(cameraPivot.transform, false);
            Camera playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.tag = "MainCamera";
            playerCamera.fieldOfView = 68f;
            playerCamera.nearClipPlane = 0.03f;
            playerCamera.farClipPlane = 1000f;
            cameraObject.AddComponent<AudioListener>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null)
            {
                Object.DestroyImmediate(bodyCollider);
            }

            MeshRenderer bodyRenderer = body.GetComponent<MeshRenderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                bodyRenderer.receiveShadows = false;
            }

            GameObject canvasObject = new GameObject("CrosshairCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one;
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            GameObject crosshairObject = new GameObject("CircleCrosshair", typeof(RectTransform), typeof(CanvasRenderer), typeof(CircleCrosshairGraphic));
            crosshairObject.transform.SetParent(canvasObject.transform, false);

            RectTransform crosshairRect = crosshairObject.GetComponent<RectTransform>();
            crosshairRect.localScale = Vector3.one;
            crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.pivot = new Vector2(0.5f, 0.5f);
            crosshairRect.anchoredPosition = Vector2.zero;
            crosshairRect.sizeDelta = new Vector2(24f, 24f);

            CircleCrosshairGraphic crosshairGraphic = crosshairObject.GetComponent<CircleCrosshairGraphic>();
            crosshairGraphic.color = Color.white;
            crosshairGraphic.raycastTarget = false;

            FirstPersonPlayerController controller = root.GetComponent<FirstPersonPlayerController>();
            if (controller == null)
            {
                controller = root.AddComponent<FirstPersonPlayerController>();
            }

            PlayerInteractionController interactionController = root.GetComponent<PlayerInteractionController>();
            if (interactionController == null)
            {
                interactionController = root.AddComponent<PlayerInteractionController>();
            }

            if (controller == null || interactionController == null)
            {
                Debug.LogError("First person player prefab build failed because one or more gameplay components could not be created. Unity may still be compiling scripts.");
                QueuePrefabBuild();
                return;
            }

            SerializedObject playerInputObject = new SerializedObject(playerInput);
            playerInputObject.FindProperty("m_Actions").objectReferenceValue = inputActions;
            playerInputObject.FindProperty("m_DefaultActionMap").stringValue = "Player";
            playerInputObject.FindProperty("m_NeverAutoSwitchControlSchemes").boolValue = false;
            playerInputObject.FindProperty("m_SplitScreenIndex").intValue = -1;
            playerInputObject.ApplyModifiedPropertiesWithoutUndo();

            SetFieldValue(controller, "cameraPivot", cameraPivot.transform);
            SetFieldValue(controller, "walkSpeed", 2.1f);
            SetFieldValue(controller, "sprintSpeed", 3.2f);
            SetFieldValue(controller, "strafeSpeedMultiplier", 0.82f);
            SetFieldValue(controller, "backwardSpeedMultiplier", 0.72f);
            SetFieldValue(controller, "movementSmoothTime", 0.22f);
            SetFieldValue(controller, "airMovementSmoothTime", 0.38f);
            SetFieldValue(controller, "jumpHeight", 0.9f);
            SetFieldValue(controller, "gravity", -28f);
            SetFieldValue(controller, "groundedGravity", -2f);
            SetFieldValue(controller, "mouseSensitivity", 0.085f);
            SetFieldValue(controller, "gamepadSensitivity", 115f);
            SetFieldValue(controller, "lookSmoothTime", 0.08f);
            SetFieldValue(controller, "minPitch", -80f);
            SetFieldValue(controller, "maxPitch", 80f);

            SetFieldValue(interactionController, "interactionCamera", playerCamera);
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                LayerMask interactableMask = 1 << interactableLayer;
                SetFieldValue(interactionController, "interactableLayers", interactableMask);
            }
            SetFieldValue(interactionController, "interactionPromptFont", projectFont);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Built first person player prefab at {PrefabPath}.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [InitializeOnLoadMethod]
    private static void EnsurePrefabIsUpToDate()
    {
        QueuePrefabBuild();
    }

    private static void QueuePrefabBuild()
    {
        if (buildQueued)
        {
            return;
        }

        buildQueued = true;
        EditorApplication.delayCall += () =>
        {
            buildQueued = false;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                QueuePrefabBuild();
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || PrefabNeedsRebuild(prefab))
            {
                BuildPrefab();
            }
        };
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    private static bool PrefabNeedsRebuild(GameObject prefab)
    {
        Transform canvasTransform = prefab.transform.Find("CrosshairCanvas");
        if (canvasTransform == null)
        {
            return true;
        }

        Canvas canvas = canvasTransform.GetComponent<Canvas>();
        RectTransform canvasRect = canvasTransform.GetComponent<RectTransform>();
        CircleCrosshairGraphic crosshair = canvasTransform.GetComponentInChildren<CircleCrosshairGraphic>(true);
        PlayerInteractionController interactionController = prefab.GetComponent<PlayerInteractionController>();
        Font projectFont = AssetDatabase.LoadAssetAtPath<Font>(ProjectFontPath);

        if (canvas == null || canvasRect == null || crosshair == null || interactionController == null)
        {
            return true;
        }

        bool zeroScaleCanvas = canvasRect.localScale.sqrMagnitude < 0.99f;
        bool missingRenderer = crosshair.GetComponent<CanvasRenderer>() == null;
        bool wrongRenderMode = canvas.renderMode != RenderMode.ScreenSpaceOverlay;
        Font assignedInteractionFont = GetFieldValue<Font>(interactionController, "interactionPromptFont");
        bool missingProjectFontAssignment = projectFont != null && assignedInteractionFont != projectFont;

        return zeroScaleCanvas || missingRenderer || wrongRenderMode || missingProjectFontAssignment;
    }

    private static void SetFieldValue<T>(object target, string fieldName, T value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogWarning($"Could not find serialized field '{fieldName}' on {target.GetType().Name} while building the first person player prefab.");
            return;
        }

        field.SetValue(target, value);

        if (target is Object unityObject)
        {
            EditorUtility.SetDirty(unityObject);
        }
    }

    private static T GetFieldValue<T>(object target, string fieldName) where T : class
    {
        if (target == null)
        {
            return null;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            return null;
        }

        return field.GetValue(target) as T;
    }
}
