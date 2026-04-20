using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SleepTransitionUI : MonoBehaviour
{
    private const string ProjectFontResourcePath = "Fonts/VCR_OSD_MONO_1.001";
    private const string CanvasName = "SleepTransitionCanvas";
    private const string TopEyelidName = "TopEyelid";
    private const string BottomEyelidName = "BottomEyelid";
    private const string FadeOverlayName = "FadeOverlay";
    private const string NextDayLabelName = "NextDayLabel";

    [Header("References")]
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image topEyelid;
    [SerializeField] private Image bottomEyelid;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private TextMeshProUGUI nextDayLabel;
    [SerializeField] private TMP_FontAsset transitionFont;

    [Header("Layout")]
    [SerializeField] private int sortingOrder = 180;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] [Min(0f)] private float textLift = 10f;

    [Header("Style")]
    [SerializeField] private Color eyelidColor = Color.black;
    [SerializeField] private Color fadeColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private Color nextDayTextColor = new Color(0.88f, 0.88f, 0.88f, 1f);

    public static SleepTransitionUI Create(Transform parent)
    {
        GameObject uiObject = new GameObject("SleepTransitionUI", typeof(RectTransform), typeof(SleepTransitionUI));
        uiObject.transform.SetParent(parent, false);
        return uiObject.GetComponent<SleepTransitionUI>();
    }

    private void Awake()
    {
        EnsureSetup();
        SetVisible(false);
        SetClosedAmount(0f);
        ShowNextDayText(false);
    }

    public void SetVisible(bool visible)
    {
        EnsureSetup();
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void SetClosedAmount(float closedAmount)
    {
        EnsureSetup();

        float normalized = Mathf.Clamp01(closedAmount);
        float lidHeight = referenceResolution.y * 0.5f * normalized;
        float fadeAlpha = Mathf.Lerp(0f, fadeColor.a, normalized);

        if (topEyelid != null)
        {
            topEyelid.rectTransform.sizeDelta = new Vector2(0f, lidHeight);
            topEyelid.color = eyelidColor;
        }

        if (bottomEyelid != null)
        {
            bottomEyelid.rectTransform.sizeDelta = new Vector2(0f, lidHeight);
            bottomEyelid.color = eyelidColor;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha);
        }
    }

    public void ShowNextDayText(bool visible)
    {
        EnsureSetup();
        nextDayLabel.gameObject.SetActive(visible);
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
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasGroup = GetOrAddComponent<CanvasGroup>(gameObject, canvasGroup);
        EnsureProjectFont();
        EnsureCanvasChildren();
    }

    private void EnsureCanvasChildren()
    {
        fadeOverlay = EnsureImage(transform, FadeOverlayName, fadeOverlay);
        RectTransform fadeRect = fadeOverlay.rectTransform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        topEyelid = EnsureImage(transform, TopEyelidName, topEyelid);
        RectTransform topRect = topEyelid.rectTransform;
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = Vector2.zero;

        bottomEyelid = EnsureImage(transform, BottomEyelidName, bottomEyelid);
        RectTransform bottomRect = bottomEyelid.rectTransform;
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = Vector2.zero;

        nextDayLabel = EnsureText(transform, NextDayLabelName, nextDayLabel);
        RectTransform labelRect = nextDayLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, textLift);
        labelRect.sizeDelta = new Vector2(720f, 90f);

        nextDayLabel.font = transitionFont != null ? transitionFont : TMP_Settings.defaultFontAsset;
        nextDayLabel.fontSize = 42f;
        nextDayLabel.fontStyle = FontStyles.Bold;
        nextDayLabel.alignment = TextAlignmentOptions.Center;
        nextDayLabel.text = "NEXT DAY";
        nextDayLabel.color = nextDayTextColor;
        nextDayLabel.raycastTarget = false;
    }

    private static Image EnsureImage(Transform parent, string name, Image existingImage)
    {
        if (existingImage == null)
        {
            Transform imageTransform = parent.Find(name);
            if (imageTransform == null)
            {
                GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(parent, false);
                imageTransform = imageObject.transform;
            }

            existingImage = imageTransform.GetComponent<Image>();
        }

        existingImage.raycastTarget = false;
        return existingImage;
    }

    private TextMeshProUGUI EnsureText(Transform parent, string name, TextMeshProUGUI existingLabel)
    {
        if (existingLabel == null)
        {
            Transform labelTransform = parent.Find(name);
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(parent, false);
                labelTransform = labelObject.transform;
            }

            existingLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }

        return existingLabel;
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
        if (transitionFont != null)
        {
            return;
        }

        Font sourceFont = Resources.Load<Font>(ProjectFontResourcePath);
        if (sourceFont == null)
        {
            return;
        }

        transitionFont = TMP_FontAsset.CreateFontAsset(sourceFont);
    }
}
