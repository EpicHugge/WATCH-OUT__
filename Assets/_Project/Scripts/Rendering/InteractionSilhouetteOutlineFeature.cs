using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

#pragma warning disable CS0618
#pragma warning disable CS0672

// This feature intentionally uses URP's compatibility-mode ScriptableRenderPass path.
// For this project that keeps the outline modular and easy to iterate on without coupling
// the interaction highlight to object materials or per-mesh clone renderers.
public sealed class InteractionSilhouetteOutlineFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader maskShader;
    [SerializeField] private Shader processShader;
    [SerializeField] private Color outlineColor = Color.white;
    [FormerlySerializedAs("silhouettePadding")]
    [SerializeField] [Range(0, 6)] private int silhouetteClosingIterations = 2;
    [SerializeField] [Range(1, 6)] private int outlineThickness = 2;
    [SerializeField] [Range(1, 4)] private int maskDownsample = 2;
    [SerializeField] [Range(0f, 1f)] private float opacity = 1f;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    [SerializeField] private bool renderInSceneView = true;

    private InteractionSilhouetteOutlinePass pass;

    public override void Create()
    {
        pass ??= new InteractionSilhouetteOutlinePass();
        pass.renderPassEvent = renderPassEvent;
        pass.UpdateResources(maskShader, processShader, outlineColor, silhouetteClosingIterations, outlineThickness, maskDownsample, opacity, renderInSceneView);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!pass.IsReadyForCamera(ref renderingData))
        {
            return;
        }

        pass.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }

    private sealed class InteractionSilhouetteOutlinePass : ScriptableRenderPass
    {
        private const int DilatePassIndex = 0;
        private const int ErodePassIndex = 1;
        private const int CompositePassIndex = 2;
        private static readonly int InnerMaskId = Shader.PropertyToID("_InnerMask");
        private static readonly int OuterMaskId = Shader.PropertyToID("_OuterMask");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        private readonly List<InteractionOutlineHighlight> visibleHighlights = new List<InteractionOutlineHighlight>();
        private readonly List<Renderer> visibleRenderers = new List<Renderer>();
        private readonly ProfilingSampler outlineProfilingSampler = new ProfilingSampler("InteractionSilhouetteOutline");

        private Material maskMaterial;
        private Material processMaterial;
        private Shader currentMaskShader;
        private Shader currentProcessShader;
        private Color currentOutlineColor;
        private int currentSilhouetteClosingIterations;
        private int currentOutlineThickness;
        private int currentMaskDownsample;
        private float currentOpacity;
        private bool renderInSceneView;

        private RTHandle cameraColorHandle;
        private RTHandle cameraDepthHandle;
        private RTHandle rawMaskHandle;
        private RTHandle workingMaskAHandle;
        private RTHandle workingMaskBHandle;
        private RTHandle colorCopyHandle;

        public void UpdateResources(
            Shader newMaskShader,
            Shader newProcessShader,
            Color newOutlineColor,
            int newSilhouetteClosingIterations,
            int newOutlineThickness,
            int newMaskDownsample,
            float newOpacity,
            bool renderSceneView)
        {
            currentMaskShader = newMaskShader;
            currentProcessShader = newProcessShader;
            currentOutlineColor = newOutlineColor;
            currentSilhouetteClosingIterations = Mathf.Max(0, newSilhouetteClosingIterations);
            currentOutlineThickness = Mathf.Max(1, newOutlineThickness);
            currentMaskDownsample = Mathf.Max(1, newMaskDownsample);
            currentOpacity = Mathf.Clamp01(newOpacity);
            renderInSceneView = renderSceneView;

            if (maskMaterial == null || maskMaterial.shader != currentMaskShader)
            {
                CoreUtils.Destroy(maskMaterial);
                maskMaterial = currentMaskShader != null ? CoreUtils.CreateEngineMaterial(currentMaskShader) : null;
            }

            if (processMaterial == null || processMaterial.shader != currentProcessShader)
            {
                CoreUtils.Destroy(processMaterial);
                processMaterial = currentProcessShader != null ? CoreUtils.CreateEngineMaterial(currentProcessShader) : null;
            }
        }

        public bool IsReadyForCamera(ref RenderingData renderingData)
        {
            if (maskMaterial == null || processMaterial == null || !InteractionOutlineRegistry.HasHighlights)
            {
                return false;
            }

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            {
                return false;
            }

            if (renderingData.cameraData.isSceneViewCamera)
            {
                return renderInSceneView;
            }

            return cameraType == CameraType.Game;
        }

        public void Setup(RTHandle colorHandle, RTHandle depthHandle)
        {
            cameraColorHandle = colorHandle;
            cameraDepthHandle = depthHandle;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor maskDescriptor = cameraTextureDescriptor;
            maskDescriptor.depthBufferBits = 0;
            maskDescriptor.msaaSamples = 1;
            maskDescriptor.colorFormat = RenderTextureFormat.R8;
            maskDescriptor.width = Mathf.Max(1, maskDescriptor.width / currentMaskDownsample);
            maskDescriptor.height = Mathf.Max(1, maskDescriptor.height / currentMaskDownsample);

            RenderTextureDescriptor colorCopyDescriptor = cameraTextureDescriptor;
            colorCopyDescriptor.depthBufferBits = 0;
            colorCopyDescriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateHandleIfNeeded(ref rawMaskHandle, maskDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_InteractionOutlineRawMask");
            RenderingUtils.ReAllocateHandleIfNeeded(ref workingMaskAHandle, maskDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_InteractionOutlineMaskA");
            RenderingUtils.ReAllocateHandleIfNeeded(ref workingMaskBHandle, maskDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_InteractionOutlineMaskB");
            RenderingUtils.ReAllocateHandleIfNeeded(ref colorCopyHandle, colorCopyDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_InteractionOutlineColorCopy");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!CollectVisibleRenderers(renderingData.cameraData.camera))
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, outlineProfilingSampler))
            {
                // Step 1:
                // Render highlighted objects into a flat white mask using the existing camera depth.
                // Because this is a filled 2D mask, not a per-mesh shell, internal mesh detail disappears.
                CoreUtils.SetRenderTarget(cmd, rawMaskHandle, cameraDepthHandle, ClearFlag.Color, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                for (int i = 0; i < visibleRenderers.Count; i++)
                {
                    Renderer targetRenderer = visibleRenderers[i];
                    if (targetRenderer == null)
                    {
                        continue;
                    }

                    Material[] sharedMaterials = targetRenderer.sharedMaterials;
                    int submeshCount = Mathf.Max(1, sharedMaterials != null ? sharedMaterials.Length : 0);

                    for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
                    {
                        cmd.DrawRenderer(targetRenderer, maskMaterial, submeshIndex, 0);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Step 2:
                // A morphological closing pass cleans the silhouette before we outline it.
                // Dilate merges nearby parts and fills small interior gaps, then erode trims the
                // shape back down so the final border hugs the perceived outer form instead of
                // simply making everything fatter.
                RTHandle silhouetteMask = BuildSilhouetteMask(cmd);
                RTHandle expandedMask = BuildExpandedOutlineMask(cmd, silhouetteMask);

                processMaterial.SetTexture(InnerMaskId, silhouetteMask);
                processMaterial.SetTexture(OuterMaskId, expandedMask);
                processMaterial.SetColor(OutlineColorId, new Color(currentOutlineColor.r, currentOutlineColor.g, currentOutlineColor.b, currentOpacity));

                Blitter.BlitCameraTexture(cmd, cameraColorHandle, colorCopyHandle);
                Blitter.BlitCameraTexture(cmd, colorCopyHandle, cameraColorHandle, processMaterial, CompositePassIndex);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private RTHandle BuildSilhouetteMask(CommandBuffer cmd)
        {
            if (currentSilhouetteClosingIterations <= 0)
            {
                return rawMaskHandle;
            }

            RTHandle dilatedMask = ApplyMorphology(cmd, rawMaskHandle, currentSilhouetteClosingIterations, DilatePassIndex);
            return ApplyMorphology(cmd, dilatedMask, currentSilhouetteClosingIterations, ErodePassIndex);
        }

        private RTHandle BuildExpandedOutlineMask(CommandBuffer cmd, RTHandle silhouetteMask)
        {
            return ApplyMorphology(cmd, silhouetteMask, currentOutlineThickness, DilatePassIndex);
        }

        private RTHandle ApplyMorphology(CommandBuffer cmd, RTHandle source, int iterations, int passIndex)
        {
            RTHandle currentSource = source;
            RTHandle currentDestination = ReferenceEquals(source, workingMaskAHandle) ? workingMaskBHandle : workingMaskAHandle;

            for (int i = 0; i < iterations; i++)
            {
                Blitter.BlitCameraTexture(cmd, currentSource, currentDestination, processMaterial, passIndex);
                Swap(ref currentSource, ref currentDestination);
            }

            return currentSource;
        }

        private bool CollectVisibleRenderers(Camera targetCamera)
        {
            visibleHighlights.Clear();
            visibleRenderers.Clear();

            if (targetCamera == null)
            {
                return false;
            }

            GeometryUtility.CalculateFrustumPlanes(targetCamera, FrustumPlanesCache.Planes);
            IReadOnlyList<InteractionOutlineHighlight> activeHighlights = InteractionOutlineRegistry.Highlights;

            for (int i = 0; i < activeHighlights.Count; i++)
            {
                InteractionOutlineHighlight highlight = activeHighlights[i];
                if (highlight == null || !highlight.isActiveAndEnabled)
                {
                    continue;
                }

                IReadOnlyList<Renderer> sourceRenderers = highlight.SourceRenderers;
                if (sourceRenderers == null || sourceRenderers.Count == 0)
                {
                    continue;
                }

                bool addedRenderer = false;

                for (int rendererIndex = 0; rendererIndex < sourceRenderers.Count; rendererIndex++)
                {
                    Renderer targetRenderer = sourceRenderers[rendererIndex];
                    if (!IsRenderable(targetRenderer))
                    {
                        continue;
                    }

                    if (!GeometryUtility.TestPlanesAABB(FrustumPlanesCache.Planes, targetRenderer.bounds))
                    {
                        continue;
                    }

                    visibleRenderers.Add(targetRenderer);
                    addedRenderer = true;
                }

                if (addedRenderer)
                {
                    visibleHighlights.Add(highlight);
                }
            }

            return visibleRenderers.Count > 0;
        }

        private static bool IsRenderable(Renderer targetRenderer)
        {
            return targetRenderer != null &&
                   targetRenderer.enabled &&
                   targetRenderer.gameObject.activeInHierarchy &&
                   targetRenderer is not ParticleSystemRenderer &&
                   targetRenderer is not TrailRenderer;
        }

        private static void Swap(ref RTHandle left, ref RTHandle right)
        {
            RTHandle temporary = left;
            left = right;
            right = temporary;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(processMaterial);
            rawMaskHandle?.Release();
            workingMaskAHandle?.Release();
            workingMaskBHandle?.Release();
            colorCopyHandle?.Release();
        }

        private static class FrustumPlanesCache
        {
            public static readonly Plane[] Planes = new Plane[6];
        }
    }
}

#pragma warning restore CS0672
#pragma warning restore CS0618
