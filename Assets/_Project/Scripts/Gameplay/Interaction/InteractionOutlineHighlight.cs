using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionOutlineHighlight : MonoBehaviour
{
    private const uint HoverRenderingLayerMask = 1u << 1;

    [Tooltip("Optional manual renderer list. Leaving this empty outlines all child renderers. For noisy multipart props, assign only the renderers that best represent the object's readable outer form.")]
    [SerializeField] private Renderer[] sourceRenderers;
    [Tooltip("When source renderers are not assigned manually, this controls whether disabled child renderers are included in auto-discovery.")]
    [SerializeField] private bool includeInactiveChildren = true;

    private bool isHighlighted;
    private readonly List<RendererState> highlightedRenderers = new List<RendererState>();

    public IReadOnlyList<Renderer> SourceRenderers => sourceRenderers;

    private void Awake()
    {
        RefreshSourceRenderers();
    }

    private void OnEnable()
    {
        if (isHighlighted)
        {
            ApplyHighlight();
        }
    }

    private void OnDisable()
    {
        RestoreOriginalRenderingLayers();
    }

    private void OnDestroy()
    {
        RestoreOriginalRenderingLayers();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;

        if (isHighlighted && isActiveAndEnabled)
        {
            ApplyHighlight();
        }
        else
        {
            RestoreOriginalRenderingLayers();
        }
    }

    public void RefreshSourceRenderers()
    {
        bool reapplyHighlight = isHighlighted && isActiveAndEnabled;
        if (reapplyHighlight)
        {
            RestoreOriginalRenderingLayers();
        }

        RebuildSourceRenderers();

        if (reapplyHighlight)
        {
            ApplyHighlight();
        }
    }

    private void ApplyHighlight()
    {
        RefreshSourceRenderersIfNeeded();
        RestoreOriginalRenderingLayers();

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer sourceRenderer = sourceRenderers[i];
            if (sourceRenderer == null)
            {
                continue;
            }

            highlightedRenderers.Add(new RendererState(sourceRenderer, sourceRenderer.renderingLayerMask));
            sourceRenderer.renderingLayerMask |= HoverRenderingLayerMask;
        }
    }

    private void RestoreOriginalRenderingLayers()
    {
        for (int i = 0; i < highlightedRenderers.Count; i++)
        {
            RendererState rendererState = highlightedRenderers[i];
            if (rendererState.Renderer != null)
            {
                rendererState.Renderer.renderingLayerMask = rendererState.OriginalRenderingLayerMask;
            }
        }

        highlightedRenderers.Clear();
    }

    private void RefreshSourceRenderersIfNeeded()
    {
        if (sourceRenderers == null || sourceRenderers.Length == 0)
        {
            RebuildSourceRenderers();
        }
    }

    private void RebuildSourceRenderers()
    {
        if (sourceRenderers == null || sourceRenderers.Length == 0)
        {
            // Auto-discovery keeps setup fast, but manual assignment is available when a
            // complex prop needs a deliberately simplified silhouette source.
            sourceRenderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        }

        if (sourceRenderers == null)
        {
            sourceRenderers = new Renderer[0];
            return;
        }

        List<Renderer> filteredRenderers = new List<Renderer>(sourceRenderers.Length);
        HashSet<Renderer> seenRenderers = new HashSet<Renderer>();

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer sourceRenderer = sourceRenderers[i];
            if (sourceRenderer == null ||
                sourceRenderer is ParticleSystemRenderer ||
                sourceRenderer is TrailRenderer ||
                !seenRenderers.Add(sourceRenderer))
            {
                continue;
            }

            filteredRenderers.Add(sourceRenderer);
        }

        sourceRenderers = filteredRenderers.ToArray();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshSourceRenderers();
    }
#endif

    private readonly struct RendererState
    {
        public RendererState(Renderer renderer, uint originalRenderingLayerMask)
        {
            Renderer = renderer;
            OriginalRenderingLayerMask = originalRenderingLayerMask;
        }

        public Renderer Renderer { get; }
        public uint OriginalRenderingLayerMask { get; }
    }
}
