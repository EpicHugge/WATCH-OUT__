using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RetroTerminalScreenEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private Graphic glowGraphic;
    [SerializeField] private Graphic sweepGraphic;

    [Header("Flicker")]
    [SerializeField] [Range(0f, 0.2f)] private float overlayFlickerStrength = 0.035f;
    [SerializeField] [Min(0f)] private float overlayFlickerSpeed = 1.8f;
    [SerializeField] [Range(0f, 0.2f)] private float glowPulseStrength = 0.05f;
    [SerializeField] [Min(0f)] private float glowPulseSpeed = 1.2f;

    [Header("Drift")]
    [SerializeField] private Vector2 contentJitterAmount = new Vector2(0.6f, 0.35f);
    [SerializeField] [Min(0f)] private float contentJitterSpeed = 1.5f;

    [Header("Sweep")]
    [SerializeField] [Min(0f)] private float sweepSpeed = 42f;
    [SerializeField] [Range(0f, 1f)] private float sweepAlpha = 0.08f;

    private Vector2 contentStartPosition;
    private Vector3 contentStartScale = Vector3.one;
    private Color glowBaseColor = Color.white;
    private Color sweepBaseColor = Color.white;
    private float overlayBaseAlpha = 1f;

    private void Awake()
    {
        CacheBaseState();
        ApplyInstantState();
    }

    private void OnEnable()
    {
        CacheBaseState();
        ApplyInstantState();
    }

    private void LateUpdate()
    {
        float time = Time.unscaledTime;

        if (contentRoot != null)
        {
            float xNoise = (Mathf.PerlinNoise(time * contentJitterSpeed, 0.13f) - 0.5f) * 2f;
            float yNoise = (Mathf.PerlinNoise(0.27f, time * contentJitterSpeed) - 0.5f) * 2f;
            contentRoot.anchoredPosition = contentStartPosition + new Vector2(
                xNoise * contentJitterAmount.x,
                yNoise * contentJitterAmount.y);

            float scalePulse = 1f + ((Mathf.PerlinNoise(0.81f, time * (contentJitterSpeed * 0.6f)) - 0.5f) * 0.004f);
            contentRoot.localScale = contentStartScale * scalePulse;
        }

        if (overlayGroup != null)
        {
            float overlayNoise = Mathf.PerlinNoise(0.41f, time * overlayFlickerSpeed);
            overlayGroup.alpha = overlayBaseAlpha * Mathf.Lerp(1f - overlayFlickerStrength, 1f + overlayFlickerStrength, overlayNoise);
        }

        if (glowGraphic != null)
        {
            float glowNoise = Mathf.PerlinNoise(time * glowPulseSpeed, 0.62f);
            glowGraphic.color = MultiplyAlpha(glowBaseColor, Mathf.Lerp(1f - glowPulseStrength, 1f + glowPulseStrength, glowNoise));
        }

        if (sweepGraphic != null)
        {
            RectTransform sweepRect = sweepGraphic.rectTransform;
            RectTransform parentRect = sweepRect.parent as RectTransform;
            if (parentRect != null)
            {
                float travel = parentRect.rect.height + sweepRect.rect.height;
                float y = Mathf.Repeat(time * sweepSpeed, travel) - (travel * 0.5f);
                sweepRect.anchoredPosition = new Vector2(sweepRect.anchoredPosition.x, y);
            }

            sweepGraphic.color = MultiplyAlpha(sweepBaseColor, sweepAlpha);
        }
    }

    private void CacheBaseState()
    {
        if (contentRoot != null)
        {
            contentStartPosition = contentRoot.anchoredPosition;
            contentStartScale = contentRoot.localScale;
        }

        if (overlayGroup != null)
        {
            overlayBaseAlpha = overlayGroup.alpha <= 0f ? 1f : overlayGroup.alpha;
        }

        if (glowGraphic != null)
        {
            glowBaseColor = glowGraphic.color;
        }

        if (sweepGraphic != null)
        {
            sweepBaseColor = sweepGraphic.color;
        }
    }

    private void ApplyInstantState()
    {
        if (sweepGraphic != null)
        {
            sweepGraphic.color = MultiplyAlpha(sweepBaseColor, sweepAlpha);
        }
    }

    private static Color MultiplyAlpha(Color source, float alphaMultiplier)
    {
        return new Color(source.r, source.g, source.b, source.a * alphaMultiplier);
    }
}
