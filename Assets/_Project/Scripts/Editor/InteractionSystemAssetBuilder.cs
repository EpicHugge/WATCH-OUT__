using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class InteractionSystemAssetBuilder
{
    private const string MaskShaderPath = "Assets/_Project/Shaders/InteractionSilhouetteMask.shader";
    private const string ProcessShaderPath = "Assets/_Project/Shaders/InteractionSilhouetteProcess.shader";
    private const string PcRendererPath = "Assets/_Project/Settings/Rendering/PC_Renderer.asset";
    private const string MobileRendererPath = "Assets/_Project/Settings/Rendering/Mobile_Renderer.asset";
    private static bool installQueued;

    [MenuItem("Tools/Build Interaction System Assets")]
    public static void EnsureAssets()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueInstall();
            return;
        }

        Shader maskShader = AssetDatabase.LoadAssetAtPath<Shader>(MaskShaderPath);
        Shader processShader = AssetDatabase.LoadAssetAtPath<Shader>(ProcessShaderPath);
        if (maskShader == null || processShader == null)
        {
            return;
        }

        EnsureRendererFeature(PcRendererPath, maskShader, processShader);
        EnsureRendererFeature(MobileRendererPath, maskShader, processShader);
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

    private static void EnsureRendererFeature(string rendererAssetPath, Shader maskShader, Shader processShader)
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererAssetPath);
        if (rendererData == null)
        {
            return;
        }

        InteractionSilhouetteOutlineFeature feature = FindFeature(rendererData);
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<InteractionSilhouetteOutlineFeature>();
            feature.name = "InteractionSilhouetteOutline";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
        }

        SetFieldValue(feature, "maskShader", maskShader);
        SetFieldValue(feature, "processShader", processShader);
        SetFieldValue(feature, "outlineColor", Color.white);
        SetFieldValue(feature, "silhouetteClosingIterations", 2);
        SetFieldValue(feature, "outlineThickness", 2);
        SetFieldValue(feature, "maskDownsample", 2);
        SetFieldValue(feature, "opacity", 1f);
        SetFieldValue(feature, "renderPassEvent", RenderPassEvent.AfterRenderingTransparents);
        SetFieldValue(feature, "renderInSceneView", true);

        feature.SetActive(true);
        feature.Create();
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
    }

    private static InteractionSilhouetteOutlineFeature FindFeature(UniversalRendererData rendererData)
    {
        for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
        {
            if (rendererData.rendererFeatures[i] is InteractionSilhouetteOutlineFeature feature)
            {
                return feature;
            }
        }

        return null;
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
