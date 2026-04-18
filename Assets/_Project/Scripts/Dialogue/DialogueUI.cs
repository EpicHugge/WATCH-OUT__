using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DialogueUI : MonoBehaviour
{
    private const string ProjectDialogueFontResourcePath = "Fonts/VCR_OSD_MONO_1.001";
    private const string PanelFrameName = "DialoguePanel";
    private const string PanelFillName = "DialoguePanelFill";
    private const string DistressOverlayName = "DialogueDistressOverlay";
    private const string SpeakerPlateName = "SpeakerPlate";
    private const string SpeakerDividerName = "SpeakerDivider";
    private const string SpeakerLabelName = "SpeakerLabel";
    private const string BodyLabelName = "BodyLabel";
    private const string ContinueLabelName = "ContinueLabel";
    private const string ContinueGlyphName = "ContinueGlyph";

    [Header("References")]
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image panelFill;
    [SerializeField] private Image distressOverlay;
    [SerializeField] private Image speakerPlate;
    [SerializeField] private Image speakerDivider;
    [SerializeField] private TextMeshProUGUI speakerLabel;
    [SerializeField] private TextMeshProUGUI bodyLabel;
    [SerializeField] private TextMeshProUGUI continueLabel;
    [SerializeField] private TextMeshProUGUI continueGlyph;
    [SerializeField] private DialogueTextAnimator textAnimator;
    [SerializeField] private TMP_FontAsset dialogueFont;

    [Header("Layout")]
    [SerializeField] private int sortingOrder = 120;
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 220f);
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(0f, 44f);
    [SerializeField] [Min(1f)] private float borderThickness = 2f;
    [SerializeField] [Min(24f)] private float speakerAreaHeight = 38f;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0.04f, 0.04f, 0.045f, 0.96f);
    [SerializeField] private Color borderColor = new Color(0.57f, 0.67f, 0.69f, 0.82f);
    [SerializeField] private Color speakerPlateColor = new Color(0.11f, 0.11f, 0.12f, 0.94f);
    [SerializeField] private Color distressOverlayColor = new Color(0.22f, 0.24f, 0.26f, 0.025f);
    [SerializeField] private Color continueTextColor = new Color(0.76f, 0.79f, 0.81f, 0.82f);
    [SerializeField] private Color continueGlyphColor = new Color(0.66f, 0.84f, 0.88f, 0.9f);

    [Header("Animation")]
    [SerializeField] private bool animateVisualNoise = true;
    [SerializeField] [Range(0f, 0.5f)] private float borderFlickerStrength = 0.05f;
    [SerializeField] [Min(0f)] private float borderFlickerSpeed = 1.4f;
    [SerializeField] [Range(0f, 2f)] private float overlayJitterAmount = 0.12f;
    [SerializeField] [Min(0f)] private float overlayJitterSpeed = 2.1f;
    [SerializeField] [Range(0f, 0.75f)] private float continuePulseStrength = 0.12f;
    [SerializeField] [Min(0f)] private float continuePulseSpeed = 2.2f;

    public TMP_Text BodyText => bodyLabel;

    private void Awake()
    {
        EnsureSetup();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (panelBackground == null || panelFill == null || distressOverlay == null)
        {
            return;
        }

        UpdateVisualNoise();
    }

    public static DialogueUI Create(Transform parent)
    {
        GameObject uiObject = new GameObject("DialogueUI", typeof(RectTransform), typeof(DialogueUI));
        uiObject.transform.SetParent(parent, false);
        return uiObject.GetComponent<DialogueUI>();
    }

    public void SetVisible(bool visible)
    {
        EnsureSetup();
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ShowLine(DialogueLine line, DialogueProcessedText processedText)
    {
        EnsureSetup();

        string speakerName = line != null ? line.SpeakerName : string.Empty;
        string dialogueText = processedText != null ? processedText.DisplayText : line != null ? line.DialogueText : string.Empty;

        speakerLabel.text = speakerName;
        speakerLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(speakerName));
        speakerLabel.color = line != null ? line.SpeakerNameColor : Color.white;
        bodyLabel.text = dialogueText ?? string.Empty;
        bodyLabel.color = ResolveDialogueTextColor(line);
        bodyLabel.maxVisibleCharacters = 0;
        ApplyTextEffect(line != null ? line.TextEffect : DialogueTextEffect.None, processedText != null ? processedText.EffectSpans : null);
        SetContinueVisible(false);
    }

    public void SetContinueVisible(bool visible)
    {
        EnsureSetup();
        continueLabel.gameObject.SetActive(visible);
        continueGlyph.gameObject.SetActive(visible);
    }

    public void SetContinueText(string text)
    {
        EnsureSetup();
        continueLabel.text = string.IsNullOrWhiteSpace(text) ? "Continue" : text;
    }

    private void EnsureSetup()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        overlayCanvas = GetOrAddComponent<Canvas>(gameObject, overlayCanvas);
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = sortingOrder;
        overlayCanvas.pixelPerfect = true;

        canvasScaler = GetOrAddComponent<CanvasScaler>(gameObject, canvasScaler);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasGroup = GetOrAddComponent<CanvasGroup>(gameObject, canvasGroup);
        EnsureProjectFont();

        RectTransform panelRect = EnsurePanelFrame();
        RectTransform fillRect = EnsurePanelFill(panelRect);
        RectTransform speakerPlateRect = EnsureSpeakerPlate(fillRect);
        EnsureLabels(fillRect, speakerPlateRect);
        textAnimator = GetOrAddComponent<DialogueTextAnimator>(gameObject, textAnimator);
        textAnimator.SetTarget(bodyLabel);
        ApplyStaticVisuals();
    }

    private RectTransform EnsurePanelFrame()
    {
        panelBackground = EnsureImage(transform, PanelFrameName, panelBackground);

        RectTransform panelRect = panelBackground.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = panelAnchoredPosition;
        panelRect.sizeDelta = panelSize;
        return panelRect;
    }

    private RectTransform EnsurePanelFill(RectTransform panelRect)
    {
        panelFill = EnsureImage(panelRect, PanelFillName, panelFill);

        RectTransform fillRect = panelFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(borderThickness, borderThickness);
        fillRect.offsetMax = new Vector2(-borderThickness, -borderThickness);
        fillRect.localScale = Vector3.one;

        distressOverlay = EnsureImage(fillRect, DistressOverlayName, distressOverlay);
        RectTransform distressRect = distressOverlay.rectTransform;
        distressRect.anchorMin = Vector2.zero;
        distressRect.anchorMax = Vector2.one;
        distressRect.offsetMin = new Vector2(10f, 10f);
        distressRect.offsetMax = new Vector2(-10f, -10f);
        distressRect.localScale = Vector3.one;

        return fillRect;
    }

    private RectTransform EnsureSpeakerPlate(RectTransform fillRect)
    {
        speakerPlate = EnsureImage(fillRect, SpeakerPlateName, speakerPlate);
        RectTransform plateRect = speakerPlate.rectTransform;
        plateRect.anchorMin = new Vector2(0f, 1f);
        plateRect.anchorMax = new Vector2(1f, 1f);
        plateRect.pivot = new Vector2(0.5f, 1f);
        plateRect.anchoredPosition = Vector2.zero;
        plateRect.sizeDelta = new Vector2(0f, speakerAreaHeight);
        plateRect.localScale = Vector3.one;

        speakerDivider = EnsureImage(plateRect, SpeakerDividerName, speakerDivider);
        RectTransform dividerRect = speakerDivider.rectTransform;
        dividerRect.anchorMin = new Vector2(0f, 0f);
        dividerRect.anchorMax = new Vector2(1f, 0f);
        dividerRect.pivot = new Vector2(0.5f, 0f);
        dividerRect.anchoredPosition = Vector2.zero;
        dividerRect.sizeDelta = new Vector2(0f, 1f);
        dividerRect.localScale = Vector3.one;

        return plateRect;
    }

    private void EnsureLabels(RectTransform fillRect, RectTransform speakerPlateRect)
    {
        speakerLabel = EnsureTextLabel(
            speakerPlateRect,
            SpeakerLabelName,
            speakerLabel,
            new Vector2(18f, 7f),
            new Vector2(-18f, -6f),
            26f,
            FontStyles.Bold,
            TextAlignmentOptions.Left);

        bodyLabel = EnsureTextLabel(
            fillRect,
            BodyLabelName,
            bodyLabel,
            new Vector2(24f, 28f),
            new Vector2(-24f, -(speakerAreaHeight + 16f)),
            32f,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        bodyLabel.textWrappingMode = TextWrappingModes.Normal;
        bodyLabel.overflowMode = TextOverflowModes.Overflow;

        continueLabel = EnsureTextLabel(
            fillRect,
            ContinueLabelName,
            continueLabel,
            new Vector2(-230f, 12f),
            new Vector2(-42f, 16f),
            17f,
            FontStyles.Normal,
            TextAlignmentOptions.BottomRight);
        continueLabel.text = "Continue";
        continueLabel.gameObject.SetActive(false);

        continueGlyph = EnsureTextLabel(
            fillRect,
            ContinueGlyphName,
            continueGlyph,
            new Vector2(-38f, 12f),
            new Vector2(-16f, 14f),
            18f,
            FontStyles.Bold,
            TextAlignmentOptions.BottomRight);
        continueGlyph.text = ">";
        continueGlyph.gameObject.SetActive(false);
    }

    private TextMeshProUGUI EnsureTextLabel(
        RectTransform parent,
        string objectName,
        TextMeshProUGUI existingLabel,
        Vector2 offsetMin,
        Vector2 offsetMax,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        if (existingLabel == null)
        {
            Transform labelTransform = parent.Find(objectName);
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(parent, false);
                labelTransform = labelObject.transform;
            }

            existingLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }

        RectTransform labelRect = existingLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = offsetMin;
        labelRect.offsetMax = offsetMax;
        labelRect.localScale = Vector3.one;

        existingLabel.font = dialogueFont != null ? dialogueFont : TMP_Settings.defaultFontAsset;
        existingLabel.fontSize = fontSize;
        existingLabel.fontStyle = fontStyle;
        existingLabel.alignment = alignment;
        existingLabel.raycastTarget = false;
        existingLabel.enableAutoSizing = false;

        return existingLabel;
    }

    private Image EnsureImage(Transform parent, string objectName, Image existingImage)
    {
        if (existingImage == null)
        {
            Transform imageTransform = parent.Find(objectName);
            if (imageTransform == null)
            {
                GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(parent, false);
                imageTransform = imageObject.transform;
            }

            existingImage = imageTransform.GetComponent<Image>();
        }

        existingImage.raycastTarget = false;
        return existingImage;
    }

    private void ApplyTextEffect(DialogueTextEffect textEffect, DialogueTextEffectSpan[] inlineEffectSpans)
    {
        if (bodyLabel == null)
        {
            return;
        }

        bodyLabel.fontStyle = ResolveDialogueFontStyle(textEffect);

        if (textAnimator != null)
        {
            textAnimator.SetEffect(textEffect);
            textAnimator.SetInlineEffects(inlineEffectSpans);
            textAnimator.RefreshCachedMesh();
        }
    }

    private void ApplyStaticVisuals()
    {
        panelFill.color = panelColor;
        speakerPlate.color = speakerPlateColor;
        speakerDivider.color = MultiplyAlpha(borderColor, 0.7f);
        continueLabel.color = continueTextColor;
        continueGlyph.color = continueGlyphColor;
    }

    private void UpdateVisualNoise()
    {
        float time = Time.unscaledTime;
        float borderIntensity = 1f;
        float overlayAlpha = 1f;
        float glyphPulse = 1f;

        if (animateVisualNoise)
        {
            borderIntensity = Mathf.Lerp(1f - borderFlickerStrength, 1f + (borderFlickerStrength * 0.35f), Mathf.PerlinNoise(0.19f, time * borderFlickerSpeed));
            overlayAlpha = Mathf.Lerp(1f - borderFlickerStrength, 1f + borderFlickerStrength, Mathf.PerlinNoise(0.73f, time * (borderFlickerSpeed * 0.8f)));
            glyphPulse = Mathf.Lerp(1f - continuePulseStrength, 1f, (Mathf.Sin(time * continuePulseSpeed) + 1f) * 0.5f);

            RectTransform overlayRect = distressOverlay.rectTransform;
            overlayRect.anchoredPosition = new Vector2(
                (Mathf.PerlinNoise(time * overlayJitterSpeed, 0.14f) - 0.5f) * 2f * overlayJitterAmount,
                (Mathf.PerlinNoise(0.27f, time * overlayJitterSpeed) - 0.5f) * 2f * overlayJitterAmount);

            continueGlyph.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(time * continuePulseSpeed) * 0.6f);
        }
        else
        {
            distressOverlay.rectTransform.anchoredPosition = Vector2.zero;
            continueGlyph.rectTransform.anchoredPosition = Vector2.zero;
        }

        panelBackground.color = MultiplyRgb(borderColor, borderIntensity);
        distressOverlay.color = MultiplyAlpha(distressOverlayColor, overlayAlpha);
        continueLabel.color = continueLabel.gameObject.activeSelf ? MultiplyAlpha(continueTextColor, glyphPulse) : continueTextColor;
        continueGlyph.color = continueGlyph.gameObject.activeSelf ? MultiplyAlpha(continueGlyphColor, glyphPulse) : continueGlyphColor;
    }

    private Color ResolveDialogueTextColor(DialogueLine line)
    {
        Color baseColor = line != null ? line.DialogueTextColor : Color.white;
        if (line == null)
        {
            return baseColor;
        }

        if (line.TextEffect == DialogueTextEffect.Whisper)
        {
            baseColor.a *= 0.82f;
        }

        return baseColor;
    }

    private static FontStyles ResolveDialogueFontStyle(DialogueTextEffect textEffect)
    {
        return textEffect == DialogueTextEffect.Shout ? FontStyles.Bold : FontStyles.Normal;
    }

    private static Color MultiplyAlpha(Color color, float multiplier)
    {
        return new Color(color.r, color.g, color.b, color.a * multiplier);
    }

    private static Color MultiplyRgb(Color color, float multiplier)
    {
        return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
    }

    private static T GetOrAddComponent<T>(GameObject target, T existingComponent) where T : Component
    {
        if (existingComponent != null)
        {
            return existingComponent;
        }

        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private void OnEnable()
    {
        EnsureProjectFont();
    }

    private void EnsureProjectFont()
    {
        if (dialogueFont != null)
        {
            return;
        }

        Font sourceFont = Resources.Load<Font>(ProjectDialogueFontResourcePath);
        if (sourceFont == null)
        {
            return;
        }

        dialogueFont = TMP_FontAsset.CreateFontAsset(sourceFont);
    }
}
