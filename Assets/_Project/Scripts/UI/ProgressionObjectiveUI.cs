using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WatchOut;

[DisallowMultipleComponent]
public sealed class ProgressionObjectiveUI : MonoBehaviour
{
    private const string ProjectFontResourcePath = "Fonts/VCR_OSD_MONO_1.001";
    private const string LabelName = "ObjectiveLabel";
    private const string DebugLabelName = "DebugLabel";

    [Header("References")]
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private RadioSystem radioSystem;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI objectiveLabel;
    [SerializeField] private TextMeshProUGUI debugLabel;
    [SerializeField] private TMP_FontAsset objectiveFont;

    [Header("Layout")]
    [SerializeField] private int sortingOrder = 115;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 anchoredPosition = new Vector2(22f, -22f);
    [SerializeField] private Vector2 size = new Vector2(520f, 110f);

    [Header("Style")]
    [SerializeField] private Color textColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField] private float fontSize = 28f;
    [SerializeField] private float debugFontSize = 18f;

    private string lastObjectiveText = string.Empty;
    private string lastDebugText = string.Empty;

    private void Awake()
    {
        ResolveReferences();
        EnsureSetup();
        RefreshText();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (progressionManager != null)
        {
            progressionManager.StateChanged += RefreshText;
        }

        RefreshText();
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.StateChanged -= RefreshText;
        }
    }

    public static ProgressionObjectiveUI Create(Transform parent)
    {
        GameObject uiObject = new GameObject("ProgressionObjectiveUI", typeof(RectTransform), typeof(ProgressionObjectiveUI));
        uiObject.transform.SetParent(parent, false);
        return uiObject.GetComponent<ProgressionObjectiveUI>();
    }

    private void ResolveReferences()
    {
        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }

        if (radioSystem == null)
        {
            radioSystem = FindAnyObjectByType<RadioSystem>();
        }
    }

    private void RefreshText()
    {
        EnsureSetup();

        string objectiveText = progressionManager != null
            ? progressionManager.CurrentObjectiveText
            : string.Empty;
        string debugText = BuildDebugText();

        if (objectiveText != lastObjectiveText)
        {
            objectiveLabel.text = objectiveText;
            lastObjectiveText = objectiveText;
        }

        if (debugText != lastDebugText)
        {
            debugLabel.text = debugText;
            lastDebugText = debugText;
        }

        objectiveLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(objectiveText));
        debugLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(debugText));
        canvasGroup.alpha = objectiveLabel.gameObject.activeSelf ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void EnsureSetup()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        overlayCanvas = GetOrAddComponent(gameObject, overlayCanvas);
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = sortingOrder;
        overlayCanvas.pixelPerfect = true;

        canvasScaler = GetOrAddComponent(gameObject, canvasScaler);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasGroup = GetOrAddComponent(gameObject, canvasGroup);
        EnsureProjectFont();
        EnsureLabel();
        EnsureDebugLabel();
    }

    private void EnsureLabel()
    {
        if (objectiveLabel == null)
        {
            Transform labelTransform = transform.Find(LabelName);
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject(LabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transform, false);
                labelTransform = labelObject.transform;
            }

            objectiveLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }

        RectTransform labelRect = objectiveLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;
        labelRect.localScale = Vector3.one;

        objectiveLabel.font = objectiveFont != null ? objectiveFont : TMP_Settings.defaultFontAsset;
        objectiveLabel.fontSize = fontSize;
        objectiveLabel.alignment = TextAlignmentOptions.TopLeft;
        objectiveLabel.color = textColor;
        objectiveLabel.textWrappingMode = TextWrappingModes.Normal;
        objectiveLabel.raycastTarget = false;

        TMP_Text outlineTarget = objectiveLabel;
        outlineTarget.outlineWidth = 0.18f;
        outlineTarget.outlineColor = outlineColor;
    }

    private void EnsureDebugLabel()
    {
        if (debugLabel == null)
        {
            Transform labelTransform = transform.Find(DebugLabelName);
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject(DebugLabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transform, false);
                labelTransform = labelObject.transform;
            }

            debugLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }

        RectTransform labelRect = debugLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y - 96f);
        labelRect.sizeDelta = new Vector2(560f, 280f);
        labelRect.localScale = Vector3.one;

        debugLabel.font = objectiveFont != null ? objectiveFont : TMP_Settings.defaultFontAsset;
        debugLabel.fontSize = debugFontSize;
        debugLabel.alignment = TextAlignmentOptions.TopLeft;
        debugLabel.color = new Color(textColor.r, textColor.g, textColor.b, 0.92f);
        debugLabel.textWrappingMode = TextWrappingModes.Normal;
        debugLabel.raycastTarget = false;
        debugLabel.outlineWidth = 0.14f;
        debugLabel.outlineColor = outlineColor;
    }

    private void EnsureProjectFont()
    {
        if (objectiveFont != null)
        {
            return;
        }

        Font sourceFont = Resources.Load<Font>(ProjectFontResourcePath);
        if (sourceFont == null)
        {
            return;
        }

        objectiveFont = TMP_FontAsset.CreateFontAsset(sourceFont);
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

    private string BuildDebugText()
    {
        return string.Empty;
    }
}
