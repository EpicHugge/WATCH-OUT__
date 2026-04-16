using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InteractionUI : MonoBehaviour
{
    private const string PromptRootName = "PromptRoot";
    private const string PromptLabelName = "PromptLabel";
    private const string LegacyBuiltInFontPath = "LegacyRuntime.ttf";
    private const string OldBuiltInFontPath = "Arial.ttf";

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField] private Text promptLabel;
    [SerializeField] private Font promptFont;
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, -44f);
    [SerializeField] private int promptFontSize = 16;
    [SerializeField] private int sortingOrder = 110;

    private string currentPrompt = string.Empty;
    private bool isVisible;

    private void Awake()
    {
        EnsureSetup();
        SetPrompt(string.Empty, false);
    }

    public void SetPrompt(string prompt, bool visible)
    {
        EnsureSetup();

        string nextPrompt = visible ? prompt : string.Empty;
        if (isVisible == visible && currentPrompt == nextPrompt)
        {
            return;
        }

        isVisible = visible;
        currentPrompt = nextPrompt;
        promptLabel.text = currentPrompt;
        promptGroup.alpha = isVisible ? 1f : 0f;
        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;
    }

    public static InteractionUI Create(Transform parent)
    {
        GameObject uiObject = new GameObject("InteractionUI", typeof(RectTransform), typeof(InteractionUI));
        uiObject.transform.SetParent(parent, false);
        return uiObject.GetComponent<InteractionUI>();
    }

    public void SetPromptFont(Font font)
    {
        if (font == null)
        {
            return;
        }

        promptFont = font;

        if (promptLabel != null)
        {
            promptLabel.font = promptFont;
        }
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
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasScaler.scaleFactor = 1f;
        canvasScaler.referencePixelsPerUnit = 100f;

        if (promptGroup == null)
        {
            Transform promptRoot = transform.Find(PromptRootName);
            if (promptRoot == null)
            {
                GameObject promptRootObject = new GameObject(PromptRootName, typeof(RectTransform), typeof(CanvasGroup));
                promptRootObject.transform.SetParent(transform, false);
                promptRoot = promptRootObject.transform;
            }

            RectTransform promptRect = promptRoot.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = promptOffset;
            promptRect.sizeDelta = new Vector2(460f, 36f);

            promptGroup = promptRoot.GetComponent<CanvasGroup>();
        }

        if (promptLabel == null)
        {
            Transform labelTransform = promptGroup.transform.Find(PromptLabelName);
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject(PromptLabelName, typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(promptGroup.transform, false);
                labelTransform = labelObject.transform;
            }

            RectTransform labelRect = labelTransform.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.localScale = Vector3.one;

            promptLabel = labelTransform.GetComponent<Text>();
            promptLabel.alignment = TextAnchor.MiddleCenter;
            promptLabel.alignByGeometry = true;
            promptLabel.color = Color.white;
            promptLabel.font = promptFont != null ? promptFont : GetBuiltInPromptFont();
            promptLabel.fontSize = promptFontSize;
            promptLabel.supportRichText = false;
            promptLabel.resizeTextForBestFit = false;
            promptLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptLabel.verticalOverflow = VerticalWrapMode.Truncate;
            promptLabel.raycastTarget = false;

        }
        else if (promptFont != null && promptLabel.font != promptFont)
        {
            promptLabel.font = promptFont;
        }

        if (promptLabel != null)
        {
            promptLabel.fontSize = promptFontSize;

            Shadow promptShadow = GetOrAddComponent<Shadow>(promptLabel.gameObject, null);
            promptShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            promptShadow.effectDistance = new Vector2(1f, -1f);
            promptShadow.useGraphicAlpha = true;
        }
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

    private static Font GetBuiltInPromptFont()
    {
        Font font = TryGetBuiltInFont(LegacyBuiltInFontPath);
        if (font != null)
        {
            return font;
        }

        font = TryGetBuiltInFont(OldBuiltInFontPath);
        if (font != null)
        {
            return font;
        }

        Debug.LogWarning("InteractionUI could not load a built-in runtime font. The interaction prompt may not render correctly.");
        return null;
    }

    private static Font TryGetBuiltInFont(string fontPath)
    {
        try
        {
            return Resources.GetBuiltinResource<Font>(fontPath);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
