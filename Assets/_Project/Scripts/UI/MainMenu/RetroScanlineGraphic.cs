using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RetroScanlineGraphic : MaskableGraphic
{
    [Header("Scanlines")]
    [SerializeField] [Min(2f)] private float lineSpacing = 6f;
    [SerializeField] [Min(0.5f)] private float lineThickness = 1f;
    [SerializeField] [Range(0f, 1f)] private float scanlineAlpha = 0f;
    [SerializeField] [Min(0f)] private float screenCurveInset = 26f;

    [Header("Vignette")]
    [SerializeField] [Min(0f)] private float vignetteThickness = 54f;
    [SerializeField] [Range(0f, 1f)] private float vignetteAlpha = 0.16f;

    protected override void Awake()
    {
        EnsureCanvasRenderer();
        base.Awake();
    }

    protected override void OnEnable()
    {
        EnsureCanvasRenderer();
        base.OnEnable();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        Color lineColor = color;
        lineColor.a *= scanlineAlpha;

        float top = rect.yMax;
        float bottom = rect.yMin;
        float left = rect.xMin;
        float right = rect.xMax;
        float centerY = rect.center.y;
        float halfHeight = rect.height * 0.5f;

        for (float y = bottom; y < top; y += lineSpacing)
        {
            float normalizedY = halfHeight <= 0.001f ? 0f : Mathf.Abs((y - centerY) / halfHeight);
            float inset = screenCurveInset * normalizedY * normalizedY;
            float lineLeft = Mathf.Min(right, left + inset);
            float lineRight = Mathf.Max(lineLeft, right - inset);
            AddQuad(vh, new Rect(lineLeft, y, lineRight - lineLeft, lineThickness), lineColor);
        }

        if (vignetteThickness <= 0f || vignetteAlpha <= 0f)
        {
            return;
        }

        Color outer = color;
        outer.a *= vignetteAlpha;
        Color inner = color;
        inner.a = 0f;

        float thickness = Mathf.Min(vignetteThickness, Mathf.Min(rect.width, rect.height) * 0.5f);

        AddVerticalGradientQuad(vh, left, bottom, thickness, rect.height, outer, inner);
        AddVerticalGradientQuad(vh, right - thickness, bottom, thickness, rect.height, inner, outer);
        AddHorizontalGradientQuad(vh, left, top - thickness, rect.width, thickness, inner, outer);
        AddHorizontalGradientQuad(vh, left, bottom, rect.width, thickness, outer, inner);
    }

    private static void AddQuad(VertexHelper vh, Rect rect, Color quadColor)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = quadColor;

        vertex.position = new Vector2(rect.xMin, rect.yMin);
        vh.AddVert(vertex);
        vertex.position = new Vector2(rect.xMin, rect.yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector2(rect.xMax, rect.yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector2(rect.xMax, rect.yMin);
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    private static void AddVerticalGradientQuad(
        VertexHelper vh,
        float x,
        float y,
        float width,
        float height,
        Color leftColor,
        Color rightColor)
    {
        if (width <= 0f || height <= 0f)
        {
            return;
        }

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;

        vertex.color = leftColor;
        vertex.position = new Vector2(x, y);
        vh.AddVert(vertex);
        vertex.position = new Vector2(x, y + height);
        vh.AddVert(vertex);

        vertex.color = rightColor;
        vertex.position = new Vector2(x + width, y + height);
        vh.AddVert(vertex);
        vertex.position = new Vector2(x + width, y);
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    private static void AddHorizontalGradientQuad(
        VertexHelper vh,
        float x,
        float y,
        float width,
        float height,
        Color bottomColor,
        Color topColor)
    {
        if (width <= 0f || height <= 0f)
        {
            return;
        }

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;

        vertex.color = bottomColor;
        vertex.position = new Vector2(x, y);
        vh.AddVert(vertex);
        vertex.position = new Vector2(x + width, y);
        vh.AddVert(vertex);

        vertex.color = topColor;
        vertex.position = new Vector2(x + width, y + height);
        vh.AddVert(vertex);
        vertex.position = new Vector2(x, y + height);
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        EnsureCanvasRenderer();
        base.OnValidate();
        SetVerticesDirty();
    }
#endif

    private void EnsureCanvasRenderer()
    {
        if (GetComponent<CanvasRenderer>() == null)
        {
            gameObject.AddComponent<CanvasRenderer>();
        }
    }
}
