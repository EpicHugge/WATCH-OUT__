using System.Reflection;
using LineworkLite.Common.Utils;
using LineworkLite.FreeOutline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OutlineAsset = LineworkLite.FreeOutline.Outline;
using OutlineResolution = LineworkLite.FreeOutline.Resolution;

public static class InteractionSystemAssetBuilder
{
    private const uint HoverRenderingLayerMask = 1u << 1;
    private static readonly Color HoverOutlineColor = Color.white;
    private const float HoverOutlineWidth = 4f;

    private const string SettingsAssetPath = "Assets/_Project/Settings/Rendering/InteractionFreeOutlineSettings.asset";
    private const string PcRendererPath = "Assets/_Project/Settings/Rendering/PC_Renderer.asset";
    private const string MobileRendererPath = "Assets/_Project/Settings/Rendering/Mobile_Renderer.asset";
    private const string HoverOutlineName = "InteractableHoverOutline";

    private static bool installQueued;

    [MenuItem("Tools/Build Interaction System Assets")]
    public static void EnsureAssets()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueInstall();
            return;
        }

        FreeOutlineSettings settings = EnsureOutlineSettings();
        if (settings == null)
        {
            return;
        }

        EnsureRendererFeature(PcRendererPath, settings);
        EnsureRendererFeature(MobileRendererPath, settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [InitializeOnLoadMethod]
    private static void EnsureAssetsOnLoad()
    {
        QueueInstall();
    }

    private static void QueueInstall()
    {
        if (installQueued)
        {
            return;
        }

        installQueued = true;
        EditorApplication.delayCall += () =>
        {
            installQueued = false;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                QueueInstall();
                return;
            }

            EnsureAssets();
        };
    }

    private static FreeOutlineSettings EnsureOutlineSettings()
    {
        EnsureFolder("Assets", "_Project");
        EnsureFolder("Assets/_Project", "Settings");
        EnsureFolder("Assets/_Project/Settings", "Rendering");

        FreeOutlineSettings settings = AssetDatabase.LoadAssetAtPath<FreeOutlineSettings>(SettingsAssetPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<FreeOutlineSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        }

        SetFieldValue(settings, "injectionPoint", InjectionPoint.AfterRenderingPostProcessing);
        SetFieldValue(settings, "showInSceneView", true);

        OutlineAsset outline = EnsureHoverOutline(settings);
        outline.RenderingLayer = HoverRenderingLayerMask;
        outline.layerMask = ~0;
        outline.renderQueue = OutlineRenderQueue.OpaqueAndTransparent;
        outline.occlusion = Occlusion.WhenNotOccluded;
        outline.maskingStrategy = MaskingStrategy.Stencil;
        outline.blendMode = BlendingMode.Alpha;
        outline.color = HoverOutlineColor;
        outline.enableOcclusion = false;
        outline.occludedColor = HoverOutlineColor;
        outline.extrusionMethod = ExtrusionMethod.ClipSpaceNormalVector;
        outline.scaling = Scaling.ConstantScreenSize;
        outline.width = HoverOutlineWidth;
        outline.minWidth = 0f;
        outline.scaleWithResolution = true;
        outline.referenceResolution = OutlineResolution._1080;
        outline.customResolution = 1080f;
        outline.materialType = MaterialType.Basic;
        outline.customMaterial = null;
        outline.SetActive(true);

        EditorUtility.SetDirty(outline);
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static OutlineAsset EnsureHoverOutline(FreeOutlineSettings settings)
    {
        OutlineAsset outline = null;

        for (int i = 0; i < settings.Outlines.Count; i++)
        {
            if (settings.Outlines[i] != null && settings.Outlines[i].name == HoverOutlineName)
            {
                outline = settings.Outlines[i];
                break;
            }
        }

        if (outline == null)
        {
            outline = ScriptableObject.CreateInstance<OutlineAsset>();
            outline.name = HoverOutlineName;
            AssetDatabase.AddObjectToAsset(outline, settings);
            settings.Outlines.Add(outline);
        }

        return outline;
    }

    private static void EnsureRendererFeature(string rendererAssetPath, FreeOutlineSettings settings)
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererAssetPath);
        if (rendererData == null)
        {
            return;
        }

        RemoveMissingRendererFeatures(rendererData);
        DisableLegacyInteractionFeature(rendererData);

        FreeOutline feature = FindFeature<FreeOutline>(rendererData);
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<FreeOutline>();
            feature.name = "InteractionHoverFreeOutline";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
        }

        SetFieldValue(feature, "settings", settings);
        feature.SetActive(true);
        feature.Create();

        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
    }

    private static void RemoveMissingRendererFeatures(UniversalRendererData rendererData)
    {
        SerializedObject serializedRendererData = new SerializedObject(rendererData);
        SerializedProperty rendererFeaturesProperty = serializedRendererData.FindProperty("m_RendererFeatures");
        bool removedMissingFeature = false;

        if (rendererFeaturesProperty != null)
        {
            for (int i = rendererFeaturesProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty featureProperty = rendererFeaturesProperty.GetArrayElementAtIndex(i);
                if (featureProperty.objectReferenceValue != null)
                {
                    continue;
                }

                rendererFeaturesProperty.DeleteArrayElementAtIndex(i);
                removedMissingFeature = true;
            }
        }

        if (!removedMissingFeature)
        {
            return;
        }

        SerializedProperty rendererFeatureMapProperty = serializedRendererData.FindProperty("m_RendererFeatureMap");
        if (rendererFeatureMapProperty != null)
        {
            rendererFeatureMapProperty.ClearArray();
        }

        serializedRendererData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DisableLegacyInteractionFeature(UniversalRendererData rendererData)
    {
        ScriptableRendererFeature legacyFeature = FindLegacyInteractionFeature(rendererData);
        if (legacyFeature == null)
        {
            return;
        }

        legacyFeature.SetActive(false);
        EditorUtility.SetDirty(legacyFeature);
        EditorUtility.SetDirty(rendererData);
    }

    private static ScriptableRendererFeature FindLegacyInteractionFeature(UniversalRendererData rendererData)
    {
        for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
        {
            ScriptableRendererFeature feature = rendererData.rendererFeatures[i];
            if (feature == null)
            {
                continue;
            }

            if (feature.name == "InteractionSilhouetteOutline" || feature.GetType().Name == "InteractionSilhouetteOutlineFeature")
            {
                return feature;
            }
        }

        return null;
    }

    private static TFeature FindFeature<TFeature>(UniversalRendererData rendererData) where TFeature : ScriptableRendererFeature
    {
        for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
        {
            if (rendererData.rendererFeatures[i] is TFeature feature)
            {
                return feature;
            }
        }

        return null;
    }

    private static void EnsureFolder(string parentFolderPath, string folderName)
    {
        string combinedPath = $"{parentFolderPath}/{folderName}";
        if (AssetDatabase.IsValidFolder(combinedPath))
        {
            return;
        }

        AssetDatabase.CreateFolder(parentFolderPath, folderName);
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
            return;
        }

        field.SetValue(target, value);
    }
}
