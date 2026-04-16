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

            if (interactableLayer >= 0)
            {
                hatch.layer = interactableLayer;
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

            DoorInteractable doorInteractable = root.AddComponent<DoorInteractable>();

            SetFieldValue(doorInteractable, "doorPivot", pivot.transform);
            SetFieldValue(doorInteractable, "openLocalEulerAngles", new Vector3(115f, 0f, 0f));
            SetFieldValue(doorInteractable, "openSpeed", 3.25f);
            SetFieldValue(doorInteractable, "startsOpen", false);
            SetFieldValue(doorInteractable, "openPrompt", "Open Trapdoor");
            SetFieldValue(doorInteractable, "closePrompt", "Close Trapdoor");

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
        Transform pivot = prefab.transform.Find("TrapdoorPivot");
        Transform hatch = pivot != null ? pivot.Find("Hatch") : null;
        BoxCollider hatchCollider = hatch != null ? hatch.GetComponent<BoxCollider>() : null;

        return interactable == null || pivot == null || hatch == null || hatchCollider == null;
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
}
