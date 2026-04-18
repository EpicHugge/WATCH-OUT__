using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PrototypeBedPrefabBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Interaction/PrototypeBed.prefab";
    private const string MaterialFolderPath = "Assets/_Project/Art/Materials/Prototype";
    private const string WoodMaterialPath = "Assets/_Project/Art/Materials/Prototype/PrototypeBed_Wood.mat";
    private const string MetalMaterialPath = "Assets/_Project/Art/Materials/Prototype/PrototypeBed_Metal.mat";
    private const string FabricMaterialPath = "Assets/_Project/Art/Materials/Prototype/PrototypeBed_Fabric.mat";
    private const string LinenMaterialPath = "Assets/_Project/Art/Materials/Prototype/PrototypeBed_Linen.mat";
    private static bool buildQueued;

    [MenuItem("Tools/Build Prototype Bed Prefab")]
    public static void BuildPrefab()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueuePrefabBuild();
            return;
        }

        EnsureFolder("Assets/_Project/Prefabs", "Interaction");
        EnsureFolder("Assets/_Project/Art", "Materials");
        EnsureFolder("Assets/_Project/Art/Materials", "Prototype");

        Material woodMaterial = GetOrCreateMaterial(WoodMaterialPath, new Color(0.24f, 0.19f, 0.16f));
        Material metalMaterial = GetOrCreateMaterial(MetalMaterialPath, new Color(0.18f, 0.14f, 0.11f));
        Material fabricMaterial = GetOrCreateMaterial(FabricMaterialPath, new Color(0.48f, 0.48f, 0.5f));
        Material linenMaterial = GetOrCreateMaterial(LinenMaterialPath, new Color(0.82f, 0.82f, 0.84f));

        GameObject root = new GameObject("PrototypeBed");

        try
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");

            GameObject frameBase = CreatePrimitive(root.transform, "FrameBase", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0f), new Vector3(1.95f, 0.18f, 4.15f), woodMaterial);
            GameObject mattress = CreatePrimitive(root.transform, "Mattress", PrimitiveType.Cube, new Vector3(0f, 0.43f, 0.08f), new Vector3(1.78f, 0.32f, 3.72f), fabricMaterial);
            GameObject pillow = CreatePrimitive(root.transform, "Pillow", PrimitiveType.Cube, new Vector3(0f, 0.62f, 1.38f), new Vector3(1.16f, 0.16f, 0.52f), linenMaterial);
            GameObject headboard = CreatePrimitive(root.transform, "Headboard", PrimitiveType.Cube, new Vector3(0f, 0.88f, 2.02f), new Vector3(1.92f, 1.18f, 0.16f), woodMaterial);
            GameObject footboard = CreatePrimitive(root.transform, "Footboard", PrimitiveType.Cube, new Vector3(0f, 0.58f, -2.02f), new Vector3(1.92f, 0.58f, 0.16f), woodMaterial);
            CreatePrimitive(root.transform, "LeftRail", PrimitiveType.Cube, new Vector3(-0.88f, 0.5f, 0f), new Vector3(0.14f, 0.48f, 4.04f), metalMaterial);
            CreatePrimitive(root.transform, "RightRail", PrimitiveType.Cube, new Vector3(0.88f, 0.5f, 0f), new Vector3(0.14f, 0.48f, 4.04f), metalMaterial);

            CreateLeg(root.transform, "FrontLeftLeg", new Vector3(-0.82f, 0.09f, -1.82f), metalMaterial);
            CreateLeg(root.transform, "FrontRightLeg", new Vector3(0.82f, 0.09f, -1.82f), metalMaterial);
            CreateLeg(root.transform, "BackLeftLeg", new Vector3(-0.82f, 0.09f, 1.82f), metalMaterial);
            CreateLeg(root.transform, "BackRightLeg", new Vector3(0.82f, 0.09f, 1.82f), metalMaterial);

            GameObject interactionZone = new GameObject("InteractionZone", typeof(BoxCollider));
            interactionZone.transform.SetParent(root.transform, false);
            interactionZone.transform.localPosition = new Vector3(0f, 0.7f, 0.2f);
            BoxCollider interactionCollider = interactionZone.GetComponent<BoxCollider>();
            interactionCollider.size = new Vector3(2.3f, 1.4f, 4.4f);

            if (interactableLayer >= 0)
            {
                interactionZone.layer = interactableLayer;
            }

            GameObject sleepPoint = new GameObject("SleepPoint");
            sleepPoint.transform.SetParent(root.transform, false);
            sleepPoint.transform.localPosition = new Vector3(0f, 0.92f, 1.42f);
            sleepPoint.transform.localRotation = Quaternion.Euler(-88f, 180f, 0f);

            GameObject standUpPoint = new GameObject("StandUpPoint");
            standUpPoint.transform.SetParent(root.transform, false);
            standUpPoint.transform.localPosition = new Vector3(1.35f, 0f, 0.55f);
            standUpPoint.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            SleepBedInteractable interactable = root.AddComponent<SleepBedInteractable>();
            SetFieldValue(interactable, "sleepPoint", sleepPoint.transform);
            SetFieldValue(interactable, "standUpPoint", standUpPoint.transform);
            SetFieldValue(interactable, "sleepPrompt", "Sleep");
            SetBaseFieldValue(interactable, "interactionDisplayName", "Bed");
            SetBaseFieldValue(interactable, "interactionVerb", "Use");

            SetShadowMode(frameBase);
            SetShadowMode(mattress);
            SetShadowMode(pillow);
            SetShadowMode(headboard);
            SetShadowMode(footboard);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Built prototype bed prefab at {PrefabPath}.");
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

        SleepBedInteractable interactable = prefab.GetComponent<SleepBedInteractable>();
        Transform sleepPoint = prefab.transform.Find("SleepPoint");
        Transform standUpPoint = prefab.transform.Find("StandUpPoint");
        Transform interactionZone = prefab.transform.Find("InteractionZone");
        BoxCollider interactionCollider = interactionZone != null ? interactionZone.GetComponent<BoxCollider>() : null;
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        bool hasMissingMaterials = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material[] sharedMaterials = renderers[i].sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                hasMissingMaterials = true;
                break;
            }

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                if (sharedMaterials[materialIndex] == null)
                {
                    hasMissingMaterials = true;
                    break;
                }
            }

            if (hasMissingMaterials)
            {
                break;
            }
        }

        bool invalidSleepPointPose =
            sleepPoint == null ||
            Vector3.Distance(sleepPoint.localPosition, new Vector3(0f, 0.92f, 1.42f)) > 0.01f ||
            Quaternion.Angle(sleepPoint.localRotation, Quaternion.Euler(-88f, 180f, 0f)) > 0.5f;

        return interactable == null || invalidSleepPointPose || standUpPoint == null || interactionCollider == null || hasMissingMaterials;
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material sharedMaterial)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;

        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null && sharedMaterial != null)
        {
            renderer.sharedMaterial = sharedMaterial;
        }

        return primitive;
    }

    private static void CreateLeg(Transform parent, string name, Vector3 localPosition, Material sharedMaterial)
    {
        GameObject leg = CreatePrimitive(parent, name, PrimitiveType.Cube, localPosition, new Vector3(0.14f, 0.18f, 0.14f), sharedMaterial);
        SetShadowMode(leg);
    }

    private static Material GetOrCreateMaterial(string assetPath, Color baseColor)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            Shader shader = FindSupportedLitShader();
            if (shader == null)
            {
                Debug.LogError($"Could not find a supported lit shader while building {assetPath}.");
                return null;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }

        material.shader = FindSupportedLitShader();
        ApplyMaterialColor(material, baseColor);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader FindSupportedLitShader()
    {
        string[] shaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Standard",
            "Legacy Shaders/Diffuse"
        };

        for (int i = 0; i < shaderNames.Length; i++)
        {
            Shader shader = Shader.Find(shaderNames[i]);
            if (shader != null)
            {
                return shader;
            }
        }

        return null;
    }

    private static void ApplyMaterialColor(Material material, Color baseColor)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.08f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }
    }

    private static void SetShadowMode(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
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
            Debug.LogWarning($"Could not find serialized field '{fieldName}' on {target.GetType().Name} while building the prototype bed prefab.");
            return;
        }

        field.SetValue(target, value);

        if (target is Object unityObject)
        {
            EditorUtility.SetDirty(unityObject);
        }
    }

    private static void SetBaseFieldValue<T>(T target, string fieldName, object value) where T : Object
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = typeof(InteractableBase).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogWarning($"Could not find InteractableBase field '{fieldName}' while building the prototype bed prefab.");
            return;
        }

        field.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }
}
