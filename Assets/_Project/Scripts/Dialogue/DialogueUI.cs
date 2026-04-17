using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DialogueUI : MonoBehaviour
{
    private const string PanelName = "DialoguePanel";
    private const string SpeakerLabelName = "SpeakerLabel";
    private const string BodyLabelName = "BodyLabel";
    private const string ContinueLabelName = "ContinueLabel";

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image panelBackground;
    [SerializeField] private TextMeshProUGUI speakerLabel;
    [SerializeField] private TextMeshProUGUI bodyLabel;
    [SerializeField] private TextMeshProUGUI continueLabel;
    [SerializeField] private TMP_FontAsset dialogueFont;
    [SerializeField] private int sortingOrder = 120;

    public TMP_Text BodyText => bodyLabel;

    private void Awake()
    {
        EnsureSetup();
        SetVisible(false);
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

    public void ShowLine(DialogueLine line)
    {
        EnsureSetup();

        string speakerName = line != null ? line.SpeakerName : string.Empty;
        string dialogueText = line != null ? line.DialogueText : string.Empty;

        speakerLabel.text = speakerName;
        speakerLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(speakerName));
        bodyLabel.text = dialogueText ?? string.Empty;
        bodyLabel.maxVisibleCharacters = 0;
        ApplyTextEffect(line != null ? line.TextEffect : DialogueTextEffect.None);
        SetContinueVisible(false);
    }

    public void SetContinueVisible(bool visible)
    {
        EnsureSetup();
        continueLabel.gameObject.SetActive(visible);
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

        RectTransform panelRect = EnsurePanel();
        EnsureLabels(panelRect);
    }

    private RectTransform EnsurePanel()
    {
        if (panelBackground == null)
        {
            Transform panelTransform = transform.Find(PanelName);
            if (panelTransform == null)
            {
                GameObject panelObject = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(transform, false);
                panelTransform = panelObject.transform;
            }

            panelBackground = panelTransform.GetComponent<Image>();
        }

        RectTransform panelRect = panelBackground.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 48f);
        panelRect.sizeDelta = new Vector2(980f, 220f);

        panelBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
        return panelRect;
    }

    private void EnsureLabels(RectTransform panelRect)
    {
        speakerLabel = EnsureTextLabel(panelRect, SpeakerLabelName, speakerLabel, new Vector2(24f, -18f), new Vector2(-24f, -62f), 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        bodyLabel = EnsureTextLabel(panelRect, BodyLabelName, bodyLabel, new Vector2(24f, -64f), new Vector2(-24f, -42f), 36f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        bodyLabel.textWrappingMode = TextWrappingModes.Normal;
        bodyLabel.overflowMode = TextOverflowModes.Overflow;

        continueLabel = EnsureTextLabel(panelRect, ContinueLabelName, continueLabel, new Vector2(-180f, 18f), new Vector2(-24f, 18f), 24f, FontStyles.Normal, TextAlignmentOptions.BottomRight);
        continueLabel.text = "Continue";
        continueLabel.gameObject.SetActive(false);
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
        existingLabel.color = Color.white;
        existingLabel.alignment = alignment;
        existingLabel.raycastTarget = false;

        return existingLabel;
    }

    private void ApplyTextEffect(DialogueTextEffect textEffect)
    {
        if (bodyLabel == null)
        {
            return;
        }

        bodyLabel.fontStyle = FontStyles.Normal;

        switch (textEffect)
        {
            case DialogueTextEffect.None:
            case DialogueTextEffect.Shake:
            case DialogueTextEffect.Wave:
            case DialogueTextEffect.Glitch:
            default:
                break;
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
}
