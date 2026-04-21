using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class DialogueTestSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Debug_Scenes/DialogueTestScene.unity";

    [InitializeOnLoadMethod]
    private static void BuildOnLoadIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            return;
        }

        EditorApplication.delayCall += BuildIfStillMissing;
    }

    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();
        Canvas canvas = CreateCanvas();
        DialogueTestMenu menu = CreateMenu(canvas.transform);
        DialogueRunner runner = CreateDialogueRunner();

        ConfigureMenu(menu, runner);
        ConfigureRunner(runner);

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Dialogue test scene created at {ScenePath}");
    }

    [MenuItem("Tools/Dialogue/Rebuild Dialogue Test Scene")]
    private static void BuildFromMenu()
    {
        Build();
    }

    private static void BuildIfStillMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            return;
        }

        try
        {
            Build();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Dialogue Test Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        StretchToParent(canvasObject.GetComponent<RectTransform>());
        return canvas;
    }

    private static DialogueTestMenu CreateMenu(Transform canvasTransform)
    {
        GameObject controllerObject = new GameObject("Dialogue Test Menu", typeof(RectTransform), typeof(DialogueTestMenu));
        controllerObject.transform.SetParent(canvasTransform, false);
        StretchToParent(controllerObject.GetComponent<RectTransform>());

        GameObject dimmer = CreateImage("Backdrop", controllerObject.transform, new Color(0.04f, 0.05f, 0.07f, 0.9f));
        StretchToParent(dimmer.GetComponent<RectTransform>());

        GameObject panel = CreateImage("Menu Panel", controllerObject.transform, new Color(0.1f, 0.11f, 0.14f, 0.96f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 860f);
        panelRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(36, 36, 36, 36);
        panelLayout.spacing = 20f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        GameObject titleObject = CreateTmpText(
            "Title",
            panel.transform,
            "Choose a Conversation",
            46f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            Color.white);
        titleObject.AddComponent<LayoutElement>().preferredHeight = 52f;

        TMP_DefaultControls.Resources tmpResources = GetTmpResources();
        GameObject searchObject = TMP_DefaultControls.CreateInputField(tmpResources);
        searchObject.name = "Search Field";
        searchObject.transform.SetParent(panel.transform, false);

        LayoutElement searchLayout = searchObject.AddComponent<LayoutElement>();
        searchLayout.minHeight = 58f;
        searchLayout.preferredHeight = 58f;

        RectTransform searchRect = searchObject.GetComponent<RectTransform>();
        searchRect.sizeDelta = new Vector2(0f, 58f);

        TMP_InputField searchField = searchObject.GetComponent<TMP_InputField>();
        searchField.textViewport.offsetMin = new Vector2(18f, 10f);
        searchField.textViewport.offsetMax = new Vector2(-18f, -10f);
        searchField.placeholder.GetComponent<TMP_Text>().text = "Filter conversations by asset name...";
        searchField.placeholder.GetComponent<TMP_Text>().fontSize = 22f;
        searchField.placeholder.GetComponent<TMP_Text>().color = new Color(0.23f, 0.28f, 0.34f, 0.78f);
        searchField.textComponent.fontSize = 25f;
        searchField.textComponent.color = new Color(0.08f, 0.11f, 0.16f, 1f);
        searchObject.GetComponent<Image>().color = new Color(0.93f, 0.95f, 0.98f, 1f);

        GameObject scrollObject = new GameObject(
            "Conversation Scroll View",
            typeof(RectTransform),
            typeof(Image),
            typeof(ScrollRect),
            typeof(LayoutElement));
        scrollObject.transform.SetParent(panel.transform, false);

        LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
        scrollLayout.preferredHeight = 700f;
        scrollLayout.minHeight = 460f;
        scrollLayout.flexibleHeight = 1f;

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0.5f);
        scrollRectTransform.anchorMax = new Vector2(1f, 0.5f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.anchoredPosition = Vector2.zero;
        scrollRectTransform.sizeDelta = new Vector2(0f, 700f);

        Image scrollBackground = scrollObject.GetComponent<Image>();
        scrollBackground.color = new Color(0.06f, 0.08f, 0.11f, 0.96f);
        scrollBackground.type = Image.Type.Sliced;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.localScale = Vector3.one;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = true;
        viewportImage.type = Image.Type.Sliced;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
        contentRect.offsetMax = new Vector2(0f, contentRect.offsetMax.y);
        contentRect.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        VerticalLayoutGroup contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        ContentSizeFitter contentSizeFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        GameObject emptyStateObject = CreateTmpText(
            "Empty State",
            contentRect,
            string.Empty,
            24f,
            FontStyles.Italic,
            TextAlignmentOptions.Center,
            new Color(0.82f, 0.84f, 0.88f, 0.92f));
        emptyStateObject.AddComponent<LayoutElement>().preferredHeight = 60f;
        emptyStateObject.SetActive(false);

        GameObject buttonObject = TMP_DefaultControls.CreateButton(tmpResources);
        buttonObject.name = "Conversation Button Template";
        buttonObject.transform.SetParent(contentRect, false);
        buttonObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        buttonObject.GetComponent<LayoutElement>().minHeight = 72f;
        buttonObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(0f, 72f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(1f, 0.78f, 0.22f, 1f);
        buttonImage.type = Image.Type.Sliced;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 0.78f, 0.22f, 1f);
        colors.highlightedColor = new Color(1f, 0.86f, 0.4f, 1f);
        colors.pressedColor = new Color(0.9f, 0.63f, 0.08f, 1f);
        colors.selectedColor = new Color(1f, 0.86f, 0.4f, 1f);
        colors.disabledColor = new Color(0.4f, 0.45f, 0.5f, 0.6f);
        button.colors = colors;

        TMP_Text buttonLabel = buttonObject.GetComponentInChildren<TMP_Text>(true);
        RectTransform buttonLabelRect = buttonLabel.rectTransform;
        buttonLabelRect.anchorMin = Vector2.zero;
        buttonLabelRect.anchorMax = Vector2.one;
        buttonLabelRect.offsetMin = new Vector2(18f, 8f);
        buttonLabelRect.offsetMax = new Vector2(-18f, -8f);
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.fontSize = 30f;
        buttonLabel.fontStyle = FontStyles.Bold;
        buttonLabel.color = new Color(0.1f, 0.08f, 0.04f, 1f);
        buttonLabel.margin = new Vector4(18f, 10f, 18f, 10f);
        buttonObject.SetActive(false);

        DialogueTestMenu menu = controllerObject.GetComponent<DialogueTestMenu>();
        SerializedObject serializedObject = new SerializedObject(menu);
        serializedObject.FindProperty("menuRoot").objectReferenceValue = panel;
        serializedObject.FindProperty("searchField").objectReferenceValue = searchField;
        serializedObject.FindProperty("conversationScrollRect").objectReferenceValue = scrollRect;
        serializedObject.FindProperty("conversationListContent").objectReferenceValue = contentRect;
        serializedObject.FindProperty("buttonTemplate").objectReferenceValue = button;
        serializedObject.FindProperty("emptyStateLabel").objectReferenceValue = emptyStateObject.GetComponent<TMP_Text>();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        return menu;
    }

    private static DialogueRunner CreateDialogueRunner()
    {
        GameObject runnerObject = new GameObject(
            "Dialogue Test Runner",
            typeof(DialogueRunner),
            typeof(DialogueTypewriter),
            typeof(DialogueVoicePlayer),
            typeof(DialogueCameraShake));

        GameObject dialogueUiObject = new GameObject("DialogueUI", typeof(RectTransform), typeof(DialogueUI));
        dialogueUiObject.transform.SetParent(runnerObject.transform, false);

        return runnerObject.GetComponent<DialogueRunner>();
    }

    private static void ConfigureMenu(DialogueTestMenu menu, DialogueRunner runner)
    {
        SerializedObject serializedObject = new SerializedObject(menu);
        serializedObject.FindProperty("dialogueRunner").objectReferenceValue = runner;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureRunner(DialogueRunner runner)
    {
        SerializedObject serializedObject = new SerializedObject(runner);
        serializedObject.FindProperty("dialogueUI").objectReferenceValue = runner.GetComponentInChildren<DialogueUI>(true);
        serializedObject.FindProperty("typewriter").objectReferenceValue = runner.GetComponent<DialogueTypewriter>();
        serializedObject.FindProperty("voicePlayer").objectReferenceValue = runner.GetComponent<DialogueVoicePlayer>();
        serializedObject.FindProperty("cameraShake").objectReferenceValue = runner.GetComponent<DialogueCameraShake>();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static DefaultControls.Resources GetUiResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    private static TMP_DefaultControls.Resources GetTmpResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private static GameObject CreateTmpText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return textObject;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
