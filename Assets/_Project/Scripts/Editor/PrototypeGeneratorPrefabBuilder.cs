using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PrototypeGeneratorPrefabBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Interaction/PrototypeGenerator.prefab";
    private static bool buildQueued;

    [MenuItem("Tools/Build Prototype Generator Prefab")]
    public static void BuildPrefab()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueuePrefabBuild();
            return;
        }

        EnsureFolder("Assets/_Project/Prefabs", "Interaction");

        GameObject root = new GameObject("PrototypeGenerator");

        try
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1.2f, 0.8f);

            GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBlock.name = "Base";
            baseBlock.transform.SetParent(root.transform, false);
            baseBlock.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            baseBlock.transform.localScale = new Vector3(1.45f, 0.36f, 1f);

            GameObject exhaust = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exhaust.name = "Exhaust";
            exhaust.transform.SetParent(root.transform, false);
            exhaust.transform.localPosition = new Vector3(-0.36f, 1.32f, -0.18f);
            exhaust.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);

            GameObject switchBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            switchBox.name = "SwitchBox";
            switchBox.transform.SetParent(root.transform, false);
            switchBox.transform.localPosition = new Vector3(0.36f, 0.84f, 0.42f);
            switchBox.transform.localScale = new Vector3(0.32f, 0.32f, 0.12f);

            GameObject leverPivot = new GameObject("LeverPivot");
            leverPivot.transform.SetParent(switchBox.transform, false);
            leverPivot.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            leverPivot.transform.localRotation = Quaternion.Euler(-38f, 0f, 0f);

            GameObject lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = "Lever";
            lever.transform.SetParent(leverPivot.transform, false);
            lever.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            lever.transform.localScale = new Vector3(0.06f, 0.24f, 0.06f);

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "Indicator";
            indicator.transform.SetParent(root.transform, false);
            indicator.transform.localPosition = new Vector3(-0.34f, 0.88f, 0.43f);
            indicator.transform.localScale = Vector3.one * 0.18f;

            Renderer bodyRenderer = body.GetComponent<Renderer>();
            Renderer baseRenderer = baseBlock.GetComponent<Renderer>();
            Renderer exhaustRenderer = exhaust.GetComponent<Renderer>();
            Renderer switchRenderer = switchBox.GetComponent<Renderer>();
            Renderer leverRenderer = lever.GetComponent<Renderer>();
            Renderer indicatorRenderer = indicator.GetComponent<Renderer>();

            SetShadowMode(bodyRenderer);
            SetShadowMode(baseRenderer);
            SetShadowMode(exhaustRenderer);
            SetShadowMode(switchRenderer);
            SetShadowMode(leverRenderer);
            SetShadowMode(indicatorRenderer);

            SetLayerRecursively(root, interactableLayer >= 0 ? interactableLayer : root.layer);

            GeneratorInteractable interactable = root.AddComponent<GeneratorInteractable>();
            SetFieldValue(interactable, "turnOnPrompt", "Turn On Generator");
            SetFieldValue(interactable, "turnOffPrompt", "Turn Off Generator");
            SetFieldValue(interactable, "startsOn", false);
            SetFieldValue(interactable, "activeStateRenderers", new Renderer[] { indicatorRenderer });
            SetFieldValue(interactable, "leverPivot", leverPivot.transform);
            SetFieldValue(interactable, "offLeverLocalEulerAngles", new Vector3(-38f, 0f, 0f));
            SetFieldValue(interactable, "onLeverLocalEulerAngles", new Vector3(38f, 0f, 0f));
            SetFieldValue(interactable, "leverMoveSpeed", 8f);
            SetFieldValue(interactable, "inactiveColor", new Color(0.2f, 0.04f, 0.04f, 1f));
            SetFieldValue(interactable, "activeColor", new Color(0.2f, 1f, 0.35f, 1f));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Built prototype generator prefab at {PrefabPath}.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [InitializeOnLoadMethod]
    private static void EnsurePrefabExists()
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

    private static bool PrefabNeedsRebuild(GameObject prefab)
    {
        if (prefab == null)
        {
            return true;
        }

        GeneratorInteractable interactable = prefab.GetComponent<GeneratorInteractable>();
        Transform indicator = prefab.transform.Find("Indicator");
        Transform body = prefab.transform.Find("Body");
        Transform leverPivot = prefab.transform.Find("SwitchBox/LeverPivot");

        return interactable == null || indicator == null || body == null || leverPivot == null;
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    private static void SetShadowMode(Renderer targetRenderer)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.shadowCastingMode = ShadowCastingMode.On;
        targetRenderer.receiveShadows = true;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }
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
            Debug.LogWarning($"Could not find serialized field '{fieldName}' on {target.GetType().Name} while building the prototype generator prefab.");
            return;
        }

        field.SetValue(target, value);

        if (target is Object unityObject)
        {
            EditorUtility.SetDirty(unityObject);
        }
    }
}
