using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class DialogueTestMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private ScrollRect conversationScrollRect;
    [SerializeField] private RectTransform conversationListContent;
    [SerializeField] private Button buttonTemplate;
    [SerializeField] private TMP_Text emptyStateLabel;

    private readonly List<ConversationButtonBinding> buttonBindings = new List<ConversationButtonBinding>();
    private DialogueRunner subscribedRunner;
    private string currentFilter = string.Empty;

    private void Awake()
    {
        EnsureReferences();
        EnsureRuntimeLayout();
        if (buttonTemplate != null)
        {
            buttonTemplate.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        EnsureReferences();
        RefreshRunnerSubscription();

        if (searchField != null)
        {
            searchField.onValueChanged.RemoveListener(HandleSearchChanged);
            searchField.onValueChanged.AddListener(HandleSearchChanged);
        }

        RefreshConversationList();
        SetMenuVisible(dialogueRunner == null || !dialogueRunner.IsRunning);
    }

    private void OnDisable()
    {
        if (searchField != null)
        {
            searchField.onValueChanged.RemoveListener(HandleSearchChanged);
        }

        RefreshRunnerSubscription(clearOnly: true);
    }

    private void Start()
    {
        RefreshConversationList();
        StartCoroutine(LogRuntimeDiagnosticsNextFrame());
    }

    public void RefreshConversationList()
    {
        EnsureReferences();
        EnsureRuntimeLayout();
        ClearButtons();

        List<DialogueConversation> conversations = LoadConversations();
        for (int i = 0; i < conversations.Count; i++)
        {
            CreateConversationButton(conversations[i]);
        }

        ApplyFilter(conversations.Count);
        ResetScrollPosition();
    }

    private void HandleSearchChanged(string searchText)
    {
        currentFilter = searchText ?? string.Empty;
        ApplyFilter(buttonBindings.Count);
    }

    private void PlayConversation(DialogueConversation conversation)
    {
        if (conversation == null || dialogueRunner == null)
        {
            return;
        }

        SetMenuVisible(false);
        bool started = dialogueRunner.StartConversation(conversation);
        if (!started)
        {
            SetMenuVisible(true);
        }
    }

    private void HandleConversationEnded(DialogueConversation conversation)
    {
        SetMenuVisible(true);
        ResetScrollPosition();
    }

    private void EnsureReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (searchField == null && menuRoot != null)
        {
            Transform searchTransform = menuRoot.transform.Find("Search Field");
            if (searchTransform != null)
            {
                searchField = searchTransform.GetComponent<TMP_InputField>();
            }
        }
    }

    private void EnsureRuntimeLayout()
    {
        if (menuRoot != null && menuRoot.TryGetComponent(out VerticalLayoutGroup panelLayout))
        {
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.spacing = 20f;
            panelLayout.padding = new RectOffset(36, 36, 36, 36);
        }

        HideUnusedMenuElement("Subtitle");

        TMP_Text titleLabel = FindMenuText("Title");
        if (titleLabel != null)
        {
            titleLabel.text = "Choose a Conversation";
            titleLabel.fontSize = 46f;
            titleLabel.color = Color.white;
        }

        if (searchField != null)
        {
            searchField.gameObject.SetActive(true);

            RectTransform searchRect = searchField.GetComponent<RectTransform>();
            if (searchRect != null)
            {
                searchRect.sizeDelta = new Vector2(0f, 58f);
            }

            LayoutElement searchLayout = searchField.GetComponent<LayoutElement>();
            if (searchLayout == null)
            {
                searchLayout = searchField.gameObject.AddComponent<LayoutElement>();
            }

            searchLayout.minHeight = 58f;
            searchLayout.preferredHeight = 58f;

            Image searchImage = searchField.GetComponent<Image>();
            if (searchImage != null)
            {
                searchImage.color = new Color(0.93f, 0.95f, 0.98f, 1f);
            }

            if (searchField.textComponent != null)
            {
                searchField.textComponent.fontSize = 25f;
                searchField.textComponent.color = new Color(0.08f, 0.11f, 0.16f, 1f);
            }

            if (searchField.placeholder is TMP_Text placeholderLabel)
            {
                placeholderLabel.fontSize = 22f;
                placeholderLabel.color = new Color(0.23f, 0.28f, 0.34f, 0.78f);
            }
        }

        if (conversationScrollRect != null)
        {
            conversationScrollRect.horizontal = false;
            conversationScrollRect.vertical = true;
            conversationScrollRect.movementType = ScrollRect.MovementType.Clamped;
            conversationScrollRect.horizontalScrollbar = null;
            conversationScrollRect.verticalScrollbar = null;

            RectTransform scrollRectTransform = conversationScrollRect.GetComponent<RectTransform>();
            if (scrollRectTransform != null)
            {
                scrollRectTransform.sizeDelta = new Vector2(0f, Mathf.Max(scrollRectTransform.sizeDelta.y, 700f));
            }

            LayoutElement scrollLayout = conversationScrollRect.GetComponent<LayoutElement>();
            if (scrollLayout == null)
            {
                scrollLayout = conversationScrollRect.gameObject.AddComponent<LayoutElement>();
            }

            scrollLayout.minHeight = 460f;
            scrollLayout.preferredHeight = 700f;
            scrollLayout.flexibleHeight = 1f;

            Image scrollImage = conversationScrollRect.GetComponent<Image>();
            if (scrollImage != null)
            {
                scrollImage.color = new Color(0.06f, 0.08f, 0.11f, 0.96f);
            }

            Transform horizontalScrollbar = conversationScrollRect.transform.Find("Scrollbar Horizontal");
            if (horizontalScrollbar != null)
            {
                horizontalScrollbar.gameObject.SetActive(false);
            }

            Transform verticalScrollbar = conversationScrollRect.transform.Find("Scrollbar Vertical");
            if (verticalScrollbar != null)
            {
                verticalScrollbar.gameObject.SetActive(false);
            }

            RectTransform viewportRect = conversationScrollRect.viewport;
            if (viewportRect != null)
            {
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.pivot = new Vector2(0.5f, 0.5f);
                viewportRect.anchoredPosition = Vector2.zero;
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;
                viewportRect.localScale = Vector3.one;

                Image viewportImage = viewportRect.GetComponent<Image>();
                if (viewportImage != null)
                {
                    viewportImage.color = new Color(0f, 0f, 0f, 0f);
                }

                RectMask2D rectMask = viewportRect.GetComponent<RectMask2D>();
                if (rectMask == null)
                {
                    rectMask = viewportRect.gameObject.AddComponent<RectMask2D>();
                }

                rectMask.enabled = true;

                Mask viewportMask = viewportRect.GetComponent<Mask>();
                if (viewportMask != null)
                {
                    viewportMask.enabled = false;
                }
            }
        }

        if (conversationListContent != null)
        {
            conversationListContent.anchorMin = new Vector2(0f, 1f);
            conversationListContent.anchorMax = new Vector2(1f, 1f);
            conversationListContent.pivot = new Vector2(0.5f, 1f);
            conversationListContent.anchoredPosition = Vector2.zero;
            conversationListContent.offsetMin = new Vector2(0f, conversationListContent.offsetMin.y);
            conversationListContent.offsetMax = new Vector2(0f, conversationListContent.offsetMax.y);
            conversationListContent.localScale = Vector3.one;

            if (conversationListContent.TryGetComponent(out VerticalLayoutGroup contentLayout))
            {
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandHeight = false;
                contentLayout.spacing = 14f;
                contentLayout.padding = new RectOffset(20, 20, 20, 20);
            }
        }

        if (buttonTemplate != null)
        {
            RectTransform buttonRect = buttonTemplate.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0f, 0.5f);
                buttonRect.anchorMax = new Vector2(1f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.sizeDelta = new Vector2(0f, 72f);
            }

            LayoutElement buttonLayout = buttonTemplate.GetComponent<LayoutElement>();
            if (buttonLayout == null)
            {
                buttonLayout = buttonTemplate.gameObject.AddComponent<LayoutElement>();
            }

            buttonLayout.minHeight = 72f;
            buttonLayout.preferredHeight = 72f;
            buttonLayout.flexibleWidth = 1f;
            buttonLayout.minWidth = 0f;

            Image buttonImage = buttonTemplate.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.enabled = true;
                buttonImage.color = new Color(1f, 0.78f, 0.22f, 1f);
                buttonImage.type = Image.Type.Sliced;
                buttonImage.raycastTarget = true;
            }

            ColorBlock colors = buttonTemplate.colors;
            colors.normalColor = new Color(1f, 0.78f, 0.22f, 1f);
            colors.highlightedColor = new Color(1f, 0.86f, 0.4f, 1f);
            colors.pressedColor = new Color(0.9f, 0.63f, 0.08f, 1f);
            colors.selectedColor = new Color(1f, 0.86f, 0.4f, 1f);
            colors.disabledColor = new Color(0.4f, 0.45f, 0.5f, 0.6f);
            colors.colorMultiplier = 1f;
            buttonTemplate.colors = colors;

            TMP_Text templateLabel = buttonTemplate.GetComponentInChildren<TMP_Text>(true);
            if (templateLabel != null)
            {
                RectTransform labelRect = templateLabel.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(18f, 8f);
                labelRect.offsetMax = new Vector2(-18f, -8f);
                templateLabel.alignment = TextAlignmentOptions.Center;
                templateLabel.fontSize = 30f;
                templateLabel.fontStyle = FontStyles.Bold;
                templateLabel.color = new Color(0.1f, 0.08f, 0.04f, 1f);
                templateLabel.margin = new Vector4(18f, 10f, 18f, 10f);
            }
        }

        if (emptyStateLabel != null)
        {
            RectTransform emptyRect = emptyStateLabel.rectTransform;
            emptyRect.anchorMin = new Vector2(0f, 0.5f);
            emptyRect.anchorMax = new Vector2(1f, 0.5f);
            emptyRect.sizeDelta = new Vector2(0f, 60f);

            LayoutElement emptyLayout = emptyStateLabel.GetComponent<LayoutElement>();
            if (emptyLayout == null)
            {
                emptyLayout = emptyStateLabel.gameObject.AddComponent<LayoutElement>();
            }

            emptyLayout.preferredHeight = 60f;
            emptyStateLabel.fontSize = 28f;
            emptyStateLabel.color = new Color(0.93f, 0.95f, 0.98f, 1f);
        }
    }

    private void RefreshRunnerSubscription(bool clearOnly = false)
    {
        if (subscribedRunner != null)
        {
            subscribedRunner.ConversationEnded -= HandleConversationEnded;
            subscribedRunner = null;
        }

        if (clearOnly || !isActiveAndEnabled || dialogueRunner == null)
        {
            return;
        }

        dialogueRunner.ConversationEnded += HandleConversationEnded;
        subscribedRunner = dialogueRunner;
    }

    private void CreateConversationButton(DialogueConversation conversation)
    {
        if (conversation == null || conversationListContent == null || buttonTemplate == null)
        {
            return;
        }

        Button buttonInstance = Instantiate(buttonTemplate, conversationListContent);
        buttonInstance.gameObject.name = conversation.name + "_Button";
        buttonInstance.gameObject.SetActive(true);

        RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(0f, 72f);
        }

        TMP_Text label = buttonInstance.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 8f);
            labelRect.offsetMax = new Vector2(-18f, -8f);
            label.text = conversation.name;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.1f, 0.08f, 0.04f, 1f);
        }

        Image buttonImage = buttonInstance.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.enabled = true;
            buttonImage.color = new Color(1f, 0.78f, 0.22f, 1f);
            buttonImage.type = Image.Type.Sliced;
            buttonImage.raycastTarget = true;
        }

        DialogueConversation capturedConversation = conversation;
        buttonInstance.onClick.AddListener(() => PlayConversation(capturedConversation));
        buttonBindings.Add(new ConversationButtonBinding(capturedConversation, buttonInstance));
    }

    private void ClearButtons()
    {
        for (int i = 0; i < buttonBindings.Count; i++)
        {
            if (buttonBindings[i].Button != null)
            {
                Destroy(buttonBindings[i].Button.gameObject);
            }
        }

        buttonBindings.Clear();
    }

    private void ApplyFilter(int conversationCount)
    {
        string filterText = currentFilter.Trim();
        int visibleCount = 0;

        for (int i = 0; i < buttonBindings.Count; i++)
        {
            ConversationButtonBinding binding = buttonBindings[i];
            bool isVisible = string.IsNullOrEmpty(filterText) ||
                             binding.Conversation.name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;

            if (binding.Button != null)
            {
                binding.Button.gameObject.SetActive(isVisible);
            }

            if (isVisible)
            {
                visibleCount++;
            }
        }

        if (emptyStateLabel != null)
        {
            bool hasNoConversations = conversationCount == 0;
            bool hasNoMatches = conversationCount > 0 && visibleCount == 0;
            emptyStateLabel.text = hasNoConversations
                ? "No DialogueConversation assets were found."
                : hasNoMatches
                    ? "No conversations match the search text."
                    : string.Empty;
            emptyStateLabel.gameObject.SetActive(hasNoConversations || hasNoMatches);
        }
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(visible);
        }
    }

    private void ResetScrollPosition()
    {
        if (conversationScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        conversationScrollRect.verticalNormalizedPosition = 1f;
    }

    private static List<DialogueConversation> LoadConversations()
    {
        List<DialogueConversation> conversations = new List<DialogueConversation>();
        HashSet<DialogueConversation> seenConversations = new HashSet<DialogueConversation>();

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:DialogueConversation");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            DialogueConversation conversation = AssetDatabase.LoadAssetAtPath<DialogueConversation>(assetPath);
            if (conversation != null && seenConversations.Add(conversation))
            {
                conversations.Add(conversation);
            }
        }

        string[] fallbackFolders =
        {
            "Assets/_Project/Dialogue/Conversations",
            "Assets/_Project/Dialogue"
        };

        for (int folderIndex = 0; folderIndex < fallbackFolders.Length; folderIndex++)
        {
            string folderPath = fallbackFolders[folderIndex];
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                continue;
            }

            string absoluteFolderPath = Path.GetFullPath(folderPath);
            if (!Directory.Exists(absoluteFolderPath))
            {
                continue;
            }

            string[] assetPaths = Directory.GetFiles(absoluteFolderPath, "*.asset", SearchOption.AllDirectories);
            for (int assetIndex = 0; assetIndex < assetPaths.Length; assetIndex++)
            {
                string normalizedPath = assetPaths[assetIndex].Replace('\\', '/');
                int assetsIndex = normalizedPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (assetsIndex < 0)
                {
                    continue;
                }

                string projectRelativePath = normalizedPath.Substring(assetsIndex);
                DialogueConversation conversation = AssetDatabase.LoadAssetAtPath<DialogueConversation>(projectRelativePath);
                if (conversation != null && seenConversations.Add(conversation))
                {
                    conversations.Add(conversation);
                }
            }
        }
#else
        DialogueConversation[] loadedConversations = Resources.FindObjectsOfTypeAll<DialogueConversation>();
        for (int i = 0; i < loadedConversations.Length; i++)
        {
            DialogueConversation conversation = loadedConversations[i];
            if (conversation != null && seenConversations.Add(conversation))
            {
                conversations.Add(conversation);
            }
        }
#endif

        conversations.Sort(static (left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        return conversations;
    }

    private System.Collections.IEnumerator LogRuntimeDiagnosticsNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[DialogueTestMenu] Runtime diagnostics");

        if (menuRoot != null)
        {
            builder.AppendLine($"menuRoot active={menuRoot.activeInHierarchy}");
        }

        if (conversationScrollRect != null)
        {
            RectTransform scrollRectTransform = conversationScrollRect.GetComponent<RectTransform>();
            builder.AppendLine($"scrollRect rect={scrollRectTransform.rect.size} anchoredPos={scrollRectTransform.anchoredPosition} scale={scrollRectTransform.lossyScale}");

            if (conversationScrollRect.viewport != null)
            {
                RectTransform viewportRect = conversationScrollRect.viewport;
                builder.AppendLine($"viewport rect={viewportRect.rect.size} anchoredPos={viewportRect.anchoredPosition} offsetMin={viewportRect.offsetMin} offsetMax={viewportRect.offsetMax} scale={viewportRect.lossyScale}");
            }
        }

        if (conversationListContent != null)
        {
            builder.AppendLine($"content rect={conversationListContent.rect.size} anchoredPos={conversationListContent.anchoredPosition} offsetMin={conversationListContent.offsetMin} offsetMax={conversationListContent.offsetMax} children={conversationListContent.childCount}");
        }

        int loggedCount = 0;
        for (int i = 0; i < buttonBindings.Count && loggedCount < 3; i++)
        {
            Button button = buttonBindings[i].Button;
            if (button == null)
            {
                continue;
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            Image buttonImage = button.GetComponent<Image>();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            bool insideViewport = conversationScrollRect != null &&
                                  conversationScrollRect.viewport != null &&
                                  RectTransformUtility.RectangleContainsScreenPoint(
                                      conversationScrollRect.viewport,
                                      RectTransformUtility.WorldToScreenPoint(null, buttonRect.position),
                                      null);

            string imageColor = buttonImage != null ? buttonImage.color.ToString() : "null";
            string labelText = label != null ? label.text : "null";
            string textColor = label != null ? label.color.ToString() : "null";
            string fontName = label != null && label.font != null ? label.font.name : "null";

            builder.AppendLine($"button[{loggedCount}] name={button.name} active={button.gameObject.activeInHierarchy} path={GetHierarchyPath(button.transform)}");
            builder.AppendLine($"  rect={buttonRect.rect.size} anchoredPos={buttonRect.anchoredPosition} worldPos={buttonRect.position} scale={buttonRect.lossyScale}");
            builder.AppendLine($"  imageEnabled={(buttonImage != null && buttonImage.enabled)} imageColor={imageColor}");
            builder.AppendLine($"  text={labelText} textEnabled={(label != null && label.enabled)} textColor={textColor} font={fontName}");
            builder.AppendLine($"  insideViewport={insideViewport} canvasGroupAlphaMin={GetCanvasGroupMinAlpha(button.transform)}");
            loggedCount++;
        }

        if (loggedCount == 0)
        {
            builder.AppendLine("No spawned active buttons were found to log.");
        }

        Debug.Log(builder.ToString());
    }

    private static string GetHierarchyPath(Transform current)
    {
        StringBuilder builder = new StringBuilder(current.name);
        while (current.parent != null)
        {
            current = current.parent;
            builder.Insert(0, current.name + "/");
        }

        return builder.ToString();
    }

    private static float GetCanvasGroupMinAlpha(Transform current)
    {
        float minAlpha = 1f;
        while (current != null)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group != null)
            {
                minAlpha = Mathf.Min(minAlpha, group.alpha);
            }

            current = current.parent;
        }

        return minAlpha;
    }

    private void HideUnusedMenuElement(string childName)
    {
        if (menuRoot == null)
        {
            return;
        }

        Transform child = menuRoot.transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    private TMP_Text FindMenuText(string childName)
    {
        if (menuRoot == null)
        {
            return null;
        }

        Transform child = menuRoot.transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private readonly struct ConversationButtonBinding
    {
        public ConversationButtonBinding(DialogueConversation conversation, Button button)
        {
            Conversation = conversation;
            Button = button;
        }

        public DialogueConversation Conversation { get; }
        public Button Button { get; }
    }
}
