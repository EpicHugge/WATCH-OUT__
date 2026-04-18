using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PrototypeTrapdoorPrefabBuilder
{
    private const string PrefabFolderPath = "Assets/_Project/Prefabs/Interaction";
    private const string PrefabPath = "Assets/_Project/Prefabs/Interaction/PrototypeTrapdoor.prefab";
    private static bool buildQueued;

    [MenuItem("Tools/Build Prototype Trapdoor Prefab")]
    public static void BuildPrefab()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueuePrefabBuild();
            return;
        }

        EnsureFolder("Assets/_Project/Prefabs", "Interaction");

        GameObject root = new GameObject("PrototypeTrapdoor");

        try
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            frame.transform.localScale = new Vector3(1.4f, 0.12f, 1.4f);

            GameObject pivot = new GameObject("TrapdoorPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.01f, 0.6f);

            GameObject hatch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hatch.name = "Hatch";
            hatch.transform.SetParent(pivot.transform, false);
            hatch.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            hatch.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);

            GameObject ladderVisual = new GameObject("LadderVisual");
            ladderVisual.transform.SetParent(root.transform, false);
            ladderVisual.transform.localPosition = new Vector3(0f, -1.1f, -0.05f);

            GameObject leftRail = CreateVisualCube("LeftRail", ladderVisual.transform, new Vector3(-0.28f, 0f, 0f), new Vector3(0.08f, 2.2f, 0.08f));
            GameObject rightRail = CreateVisualCube("RightRail", ladderVisual.transform, new Vector3(0.28f, 0f, 0f), new Vector3(0.08f, 2.2f, 0.08f));
            for (int i = 0; i < 6; i++)
            {
                float rungY = 0.8f - (i * 0.34f);
                CreateVisualCube($"Rung_{i + 1}", ladderVisual.transform, new Vector3(0f, rungY, 0f), new Vector3(0.56f, 0.06f, 0.08f));
            }

            GameObject topInteractPoint = new GameObject("LadderTopInteract");
            topInteractPoint.transform.SetParent(root.transform, false);
            topInteractPoint.transform.localPosition = new Vector3(0f, 0.9f, -0.9f);
            BoxCollider topCollider = topInteractPoint.AddComponent<BoxCollider>();
            topCollider.size = new Vector3(0.7f, 1.5f, 0.5f);

            GameObject bottomInteractPoint = new GameObject("LadderBottomInteract");
            bottomInteractPoint.transform.SetParent(root.transform, false);
            bottomInteractPoint.transform.localPosition = new Vector3(0f, -1.35f, -0.85f);
            BoxCollider bottomCollider = bottomInteractPoint.AddComponent<BoxCollider>();
            bottomCollider.size = new Vector3(0.7f, 1.6f, 0.5f);

            GameObject topDestination = new GameObject("TopDestination");
            topDestination.transform.SetParent(root.transform, false);
            topDestination.transform.localPosition = new Vector3(0f, 0f, -1.35f);
            topDestination.transform.localRotation = Quaternion.identity;

            GameObject bottomDestination = new GameObject("BottomDestination");
            bottomDestination.transform.SetParent(root.transform, false);
            bottomDestination.transform.localPosition = new Vector3(0f, -2.25f, -1.1f);
            bottomDestination.transform.localRotation = Quaternion.identity;

            if (interactableLayer >= 0)
            {
                hatch.layer = interactableLayer;
                topInteractPoint.layer = interactableLayer;
                bottomInteractPoint.layer = interactableLayer;
            }

            MeshRenderer frameRenderer = frame.GetComponent<MeshRenderer>();
            if (frameRenderer != null)
            {
                frameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            MeshRenderer hatchRenderer = hatch.GetComponent<MeshRenderer>();
            if (hatchRenderer != null)
            {
                hatchRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            InteractionOutlineHighlight doorHighlight = root.AddComponent<InteractionOutlineHighlight>();
            SetFieldValue(doorHighlight, "sourceRenderers", hatchRenderer != null ? new Renderer[] { hatchRenderer } : new Renderer[0]);

            Material prototypeMaterial = frameRenderer != null ? frameRenderer.sharedMaterial : null;
            ApplyPrototypeMaterial(leftRail, prototypeMaterial);
            ApplyPrototypeMaterial(rightRail, prototypeMaterial);
            foreach (Transform child in ladderVisual.transform)
            {
                ApplyPrototypeMaterial(child.gameObject, prototypeMaterial);
            }

            Renderer[] ladderRenderers = ladderVisual.GetComponentsInChildren<Renderer>(true);

            DoorInteractable doorInteractable = root.AddComponent<DoorInteractable>();
            SetFieldValue(doorInteractable, "doorPivot", pivot.transform);
            SetFieldValue(doorInteractable, "openLocalEulerAngles", new Vector3(115f, 0f, 0f));
            SetFieldValue(doorInteractable, "openSpeed", 3.25f);
            SetFieldValue(doorInteractable, "startsOpen", false);
            SetFieldValue(doorInteractable, "openPrompt", "Open Trapdoor");
            SetFieldValue(doorInteractable, "closePrompt", "Close Trapdoor");

            TrapdoorLadderSetup ladderSetup = root.AddComponent<TrapdoorLadderSetup>();
            SetFieldValue(ladderSetup, "topDestination", topDestination.transform);
            SetFieldValue(ladderSetup, "bottomDestination", bottomDestination.transform);
            SetFieldValue(ladderSetup, "trapdoorPivot", pivot.transform);
            SetFieldValue(ladderSetup, "startsAvailable", false);
            SetFieldValue(ladderSetup, "openRequiredAngle", 45f);
            SetFieldValue(ladderSetup, "ladderInteractionColliders", new Collider[] { topCollider, bottomCollider });

            TrapdoorLadderInteractable topInteractable = topInteractPoint.AddComponent<TrapdoorLadderInteractable>();
            SetFieldValue(topInteractable, "ladderSetup", ladderSetup);
            SetFieldValue(topInteractable, "direction", TrapdoorLadderDirection.Down);
            SetFieldValue(topInteractable, "climbDownPrompt", "Climb Down");

            InteractionOutlineHighlight topHighlight = topInteractPoint.AddComponent<InteractionOutlineHighlight>();
            SetFieldValue(topHighlight, "sourceRenderers", ladderRenderers);

            TrapdoorLadderInteractable bottomInteractable = bottomInteractPoint.AddComponent<TrapdoorLadderInteractable>();
            SetFieldValue(bottomInteractable, "ladderSetup", ladderSetup);
            SetFieldValue(bottomInteractable, "direction", TrapdoorLadderDirection.Up);
            SetFieldValue(bottomInteractable, "climbUpPrompt", "Climb Up");

            InteractionOutlineHighlight bottomHighlight = bottomInteractPoint.AddComponent<InteractionOutlineHighlight>();
            SetFieldValue(bottomHighlight, "sourceRenderers", ladderRenderers);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Built prototype trapdoor prefab at {PrefabPath}.");
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

        DoorInteractable interactable = prefab.GetComponent<DoorInteractable>();
        TrapdoorLadderSetup ladderSetup = prefab.GetComponent<TrapdoorLadderSetup>();
        InteractionOutlineHighlight doorHighlight = prefab.GetComponent<InteractionOutlineHighlight>();
        Transform pivot = prefab.transform.Find("TrapdoorPivot");
        Transform hatch = pivot != null ? pivot.Find("Hatch") : null;
        BoxCollider hatchCollider = hatch != null ? hatch.GetComponent<BoxCollider>() : null;
        Transform ladderVisual = prefab.transform.Find("LadderVisual");
        Transform topInteractPoint = prefab.transform.Find("LadderTopInteract");
        Transform bottomInteractPoint = prefab.transform.Find("LadderBottomInteract");
        Transform topDestination = prefab.transform.Find("TopDestination");
        Transform bottomDestination = prefab.transform.Find("BottomDestination");
        TrapdoorLadderInteractable topInteractable = topInteractPoint != null ? topInteractPoint.GetComponent<TrapdoorLadderInteractable>() : null;
        TrapdoorLadderInteractable bottomInteractable = bottomInteractPoint != null ? bottomInteractPoint.GetComponent<TrapdoorLadderInteractable>() : null;
        InteractionOutlineHighlight topHighlight = topInteractPoint != null ? topInteractPoint.GetComponent<InteractionOutlineHighlight>() : null;
        InteractionOutlineHighlight bottomHighlight = bottomInteractPoint != null ? bottomInteractPoint.GetComponent<InteractionOutlineHighlight>() : null;
        SerializedObject ladderSetupObject = ladderSetup != null ? new SerializedObject(ladderSetup) : null;
        Object configuredPivot = ladderSetupObject != null ? ladderSetupObject.FindProperty("trapdoorPivot").objectReferenceValue : null;
        Object configuredTopDestination = ladderSetupObject != null ? ladderSetupObject.FindProperty("topDestination").objectReferenceValue : null;
        Object configuredBottomDestination = ladderSetupObject != null ? ladderSetupObject.FindProperty("bottomDestination").objectReferenceValue : null;
        SerializedProperty configuredInteractionColliders = ladderSetupObject != null ? ladderSetupObject.FindProperty("ladderInteractionColliders") : null;

        return interactable == null ||
               doorHighlight == null ||
               pivot == null ||
               hatch == null ||
               hatchCollider == null ||
               ladderSetup == null ||
               ladderVisual == null ||
               topInteractPoint == null ||
               bottomInteractPoint == null ||
               topDestination == null ||
               bottomDestination == null ||
               configuredPivot == null ||
               configuredTopDestination == null ||
               configuredBottomDestination == null ||
               configuredInteractionColliders == null ||
               configuredInteractionColliders.arraySize < 2 ||
               topInteractable == null ||
               bottomInteractable == null ||
               topHighlight == null ||
               bottomHighlight == null;
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
            Debug.LogWarning($"Could not find serialized field '{fieldName}' on {target.GetType().Name} while building the prototype trapdoor prefab.");
            return;
        }

        field.SetValue(target, value);

        if (target is Object unityObject)
        {
            EditorUtility.SetDirty(unityObject);
        }
    }

    private static GameObject CreateVisualCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return cube;
    }

    private static void ApplyPrototypeMaterial(GameObject target, Material material)
    {
        if (target == null)
        {
            return;
        }

        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
    }
}
