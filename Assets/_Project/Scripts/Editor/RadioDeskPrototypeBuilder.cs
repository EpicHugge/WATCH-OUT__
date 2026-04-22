using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class RadioDeskPrototypeBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Interaction/Radio/RadioDeskPrototype.prefab";
    private const string MaterialFolder = "Assets/_Project/Art/Materials/RadioPrototype";
    private const string PendingBuildSessionKey = "RadioDeskPrototypeBuilder.PendingBuild";

    [InitializeOnLoadMethod]
    private static void BuildIfMissingOnLoad()
    {
        if (Application.isBatchMode ||
            SessionState.GetBool(PendingBuildSessionKey, false) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        SessionState.SetBool(PendingBuildSessionKey, true);
        EditorApplication.delayCall += () =>
        {
            SessionState.SetBool(PendingBuildSessionKey, false);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                BuildPrefab();
            }
        };
    }

    [MenuItem("Tools/Radio/Build Desk Prototype")]
    public static void BuildPrefab()
    {
        EnsureFolder("Assets/_Project/Art/Materials", "RadioPrototype");

        Material bodyMaterial = GetOrCreateMaterial("RadioPrototypeBody", new Color(0.14f, 0.12f, 0.1f));
        Material panelMaterial = GetOrCreateMaterial("RadioPrototypePanel", new Color(0.53f, 0.46f, 0.35f));
        Material bezelMaterial = GetOrCreateMaterial("RadioPrototypeBezel", new Color(0.21f, 0.18f, 0.14f));
        Material accentMaterial = GetOrCreateMaterial("RadioPrototypeAccent", new Color(0.36f, 0.22f, 0.12f));
        Material lampMaterial = GetOrCreateMaterial("RadioPrototypeLamp", new Color(0.28f, 0.11f, 0.03f));

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject root = new GameObject("RadioDeskPrototype");
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
        {
            SetLayerRecursively(root, interactableLayer);
        }

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(1.6f, 1.1f, 0.95f);
        collider.center = new Vector3(0f, 0.25f, 0f);

        RadioDeskOperationController controller = root.AddComponent<RadioDeskOperationController>();
        root.AddComponent<RadioDeskInteractable>();
        root.AddComponent<InteractionOutlineHighlight>();

        CreateCube("Body", root.transform, bodyMaterial, new Vector3(0f, 0.18f, 0f), new Vector3(1.55f, 0.5f, 0.78f));
        CreateCube("LowerBody", root.transform, bodyMaterial, new Vector3(0f, -0.02f, 0.08f), new Vector3(1.42f, 0.22f, 0.7f));
        CreateCube("FrontPanel", root.transform, panelMaterial, new Vector3(0f, 0.28f, 0.35f), new Vector3(1.42f, 0.72f, 0.08f));
        CreateCube("TopCap", root.transform, bezelMaterial, new Vector3(0f, 0.5f, 0.03f), new Vector3(1.32f, 0.12f, 0.64f));
        CreateCube("Trim", root.transform, accentMaterial, new Vector3(0f, 0.05f, 0.37f), new Vector3(1.46f, 0.06f, 0.11f));

        CreateCube("FrequencyHousing", root.transform, bezelMaterial, new Vector3(-0.34f, 0.42f, 0.4f), new Vector3(0.62f, 0.2f, 0.06f));
        CreateCube("FrequencyGlass", root.transform, panelMaterial, new Vector3(-0.34f, 0.42f, 0.435f), new Vector3(0.55f, 0.13f, 0.02f));
        CreateText("FrequencyLabel", root.transform, "FREQ", new Vector3(-0.57f, 0.56f, 0.44f), 2.8f, Color.black, FontStyles.Bold);
        TMP_Text frequencyText = CreateText("DeskFrequencyReadout", root.transform, "99.7", new Vector3(-0.34f, 0.42f, 0.45f), 4.2f, new Color(0.08f, 0.05f, 0.02f), FontStyles.Bold);

        CreateCube("MeterHousing", root.transform, bezelMaterial, new Vector3(0.36f, 0.42f, 0.4f), new Vector3(0.54f, 0.3f, 0.06f));
        GameObject meterFace = CreateCube("MeterFace", root.transform, panelMaterial, new Vector3(0.36f, 0.42f, 0.436f), new Vector3(0.46f, 0.2f, 0.015f));
        CreateText("MeterLabel", root.transform, "SIGNAL", new Vector3(0.36f, 0.56f, 0.448f), 2.4f, new Color(0.09f, 0.06f, 0.03f), FontStyles.Bold);

        GameObject strengthFill = CreateCube("SignalStrengthFill", root.transform, accentMaterial, new Vector3(0.36f, 0.39f, 0.447f), new Vector3(0.33f, 0.025f, 0.01f));
        CreateCube("MeterCenterMarker", root.transform, accentMaterial, new Vector3(0.36f, 0.42f, 0.448f), new Vector3(0.01f, 0.16f, 0.01f));
        GameObject needlePivot = new GameObject("MeterNeedlePivot");
        needlePivot.transform.SetParent(root.transform, false);
        needlePivot.transform.localPosition = new Vector3(0.36f, 0.345f, 0.448f);
        CreateCube("MeterNeedle", needlePivot.transform, accentMaterial, new Vector3(0f, 0.1f, 0f), new Vector3(0.014f, 0.2f, 0.012f));

        GameObject mainKnob = CreateCylinder("MainTuningKnob", root.transform, bezelMaterial, new Vector3(-0.5f, 0.1f, 0.44f), new Vector3(0.16f, 0.055f, 0.16f), new Vector3(90f, 0f, 0f));
        CreateCylinder("MainTuningCap", mainKnob.transform, accentMaterial, new Vector3(0f, 0f, 0.19f), new Vector3(0.35f, 0.12f, 0.35f), Vector3.zero);
        CreateText("MainTuningLabel", root.transform, "TUNE", new Vector3(-0.5f, -0.08f, 0.44f), 2.2f, new Color(0.09f, 0.06f, 0.03f), FontStyles.Bold);

        GameObject fineKnob = CreateCylinder("FineTuneKnob", root.transform, bezelMaterial, new Vector3(-0.16f, 0.1f, 0.44f), new Vector3(0.13f, 0.05f, 0.13f), new Vector3(90f, 0f, 0f));
        CreateCylinder("FineTuneCap", fineKnob.transform, accentMaterial, new Vector3(0f, 0f, 0.18f), new Vector3(0.34f, 0.11f, 0.34f), Vector3.zero);
        CreateText("FineTuneLabel", root.transform, "LOCK", new Vector3(-0.16f, -0.08f, 0.44f), 2.2f, new Color(0.09f, 0.06f, 0.03f), FontStyles.Bold);

        CreateCylinder("LockLampBase", root.transform, bezelMaterial, new Vector3(0.23f, 0.1f, 0.44f), new Vector3(0.06f, 0.02f, 0.06f), new Vector3(90f, 0f, 0f));
        GameObject lockLamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lockLamp.name = "LockLamp";
        lockLamp.transform.SetParent(root.transform, false);
        lockLamp.transform.localPosition = new Vector3(0.23f, 0.125f, 0.455f);
        lockLamp.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
        SetMaterial(lockLamp, lampMaterial);
        Object.DestroyImmediate(lockLamp.GetComponent<Collider>());

        CreateCube("ToggleBase", root.transform, bezelMaterial, new Vector3(0.58f, 0.08f, 0.44f), new Vector3(0.12f, 0.03f, 0.08f));
        GameObject toggleSwitch = CreateCube("ToggleSwitch", root.transform, accentMaterial, new Vector3(0.58f, 0.16f, 0.445f), new Vector3(0.02f, 0.14f, 0.02f));
        toggleSwitch.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        CreateText("PowerLabel", root.transform, "POWER", new Vector3(0.58f, -0.08f, 0.44f), 2.0f, new Color(0.09f, 0.06f, 0.03f), FontStyles.Bold);
        CreateCube("ModeButtonA", root.transform, bezelMaterial, new Vector3(0.39f, 0.1f, 0.44f), new Vector3(0.08f, 0.04f, 0.06f));
        CreateCube("ModeButtonB", root.transform, bezelMaterial, new Vector3(0.49f, 0.1f, 0.44f), new Vector3(0.08f, 0.04f, 0.06f));

        GameObject cameraObject = new GameObject("OperationCamera");
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.28f, 1.05f);
        cameraObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Camera operationCamera = cameraObject.AddComponent<Camera>();
        operationCamera.enabled = false;
        operationCamera.fieldOfView = 34f;
        operationCamera.nearClipPlane = 0.01f;
        operationCamera.farClipPlane = 20f;
        operationCamera.clearFlags = CameraClearFlags.Skybox;

        SerializedObject controllerSerializedObject = new SerializedObject(controller);
        controllerSerializedObject.FindProperty("radioSystem").objectReferenceValue = null;
        controllerSerializedObject.FindProperty("operationCamera").objectReferenceValue = operationCamera;
        controllerSerializedObject.FindProperty("frequencyDisplayText").objectReferenceValue = frequencyText;
        controllerSerializedObject.FindProperty("coarseTuneKnob").objectReferenceValue = mainKnob.transform;
        controllerSerializedObject.FindProperty("fineTuneKnob").objectReferenceValue = fineKnob.transform;
        controllerSerializedObject.FindProperty("signalNeedle").objectReferenceValue = needlePivot.transform;
        controllerSerializedObject.FindProperty("signalStrengthFill").objectReferenceValue = strengthFill.transform;
        controllerSerializedObject.FindProperty("meterFaceRenderer").objectReferenceValue = meterFace.GetComponent<Renderer>();
        controllerSerializedObject.FindProperty("lockLampRenderer").objectReferenceValue = lockLamp.GetComponent<Renderer>();
        controllerSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string parent, string child)
    {
        string folderPath = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Material GetOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = materialName;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    private static GameObject CreateCube(string name, Transform parent, Material material, Vector3 localPosition, Vector3 localScale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        SetMaterial(cube, material);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static GameObject CreateCylinder(string name, Transform parent, Material material, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localScale = localScale;
        cylinder.transform.localEulerAngles = localEulerAngles;
        SetMaterial(cylinder, material);
        Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        return cylinder;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, Vector3 localPosition, float fontSize, Color color, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one * 0.08f;

        TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.font = TMP_Settings.defaultFontAsset;
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableAutoSizing = false;
        textMesh.fontStyle = fontStyle;
        textMesh.color = color;
        textMesh.raycastTarget = false;
        textMesh.sortingOrder = 2;
        return textMesh;
    }

    private static void SetMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
