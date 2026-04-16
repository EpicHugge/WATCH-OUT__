using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CircleCrosshairGraphic : MaskableGraphic
{
    [SerializeField] [Min(6)] private int segments = 48;
    [SerializeField] [Min(1f)] private float radius = 10f;
    [SerializeField] [Min(0.5f)] private float thickness = 2f;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(1f, value);
            SetVerticesDirty();
        }
    }

    public float Thickness
    {
        get => thickness;
        set
        {
            thickness = Mathf.Max(0.5f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float outerRadius = radius;
        float innerRadius = Mathf.Max(0f, radius - thickness);
        Vector2 center = rectTransform.rect.center;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angleA = Mathf.Deg2Rad * (i * angleStep);
            float angleB = Mathf.Deg2Rad * ((i + 1) * angleStep);

            Vector2 outerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * outerRadius;
            Vector2 outerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * outerRadius;
            Vector2 innerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * innerRadius;
            Vector2 innerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * innerRadius;

            int index = vh.currentVertCount;

            vh.AddVert(outerA, color, Vector2.zero);
            vh.AddVert(outerB, color, Vector2.zero);
            vh.AddVert(innerB, color, Vector2.zero);
            vh.AddVert(innerA, color, Vector2.zero);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        segments = Mathf.Max(6, segments);
        radius = Mathf.Max(1f, radius);
        thickness = Mathf.Clamp(thickness, 0.5f, radius);
        SetVerticesDirty();
    }
#endif
}
