using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionOutlineHighlight : MonoBehaviour
{
    [Tooltip("Optional manual renderer list. Leaving this empty outlines all child renderers. For noisy multipart props, assign only the renderers that best represent the object's readable outer form.")]
    [SerializeField] private Renderer[] sourceRenderers;
    [Tooltip("When source renderers are not assigned manually, this controls whether disabled child renderers are included in auto-discovery.")]
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private Color outlineColor = Color.white;

    private bool isHighlighted;

    public IReadOnlyList<Renderer> SourceRenderers => sourceRenderers;
    public Color OutlineColor => outlineColor;

    private void Awake()
    {
        RefreshSourceRenderers();
    }

    private void OnEnable()
    {
        if (isHighlighted)
        {
            InteractionOutlineRegistry.Register(this);
        }
    }

    private void OnDisable()
    {
        InteractionOutlineRegistry.Unregister(this);
    }

    private void OnDestroy()
    {
        InteractionOutlineRegistry.Unregister(this);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        RefreshSourceRenderers();

        if (isHighlighted && isActiveAndEnabled)
        {
            InteractionOutlineRegistry.Register(this);
        }
        else
        {
            InteractionOutlineRegistry.Unregister(this);
        }
    }

    public void RefreshSourceRenderers()
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

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer sourceRenderer = sourceRenderers[i];
            if (sourceRenderer == null || sourceRenderer is ParticleSystemRenderer || sourceRenderer is TrailRenderer)
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
}
