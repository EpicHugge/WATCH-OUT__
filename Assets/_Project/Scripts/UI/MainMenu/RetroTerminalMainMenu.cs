using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RetroTerminalMainMenu : MonoBehaviour
{
    private const string ProjectMenuFontResourcePath = "Fonts/VCR_OSD_MONO_1.001";

    private enum ViewState
    {
        Main,
        Settings,
        Credits
    }

    private enum MenuAction
    {
        Play,
        Settings,
        Credits,
        Quit
    }

    [Serializable]
    private sealed class MenuItemView
    {
        public MenuAction action;
        public RectTransform root;
        public UnityEngine.UI.Image frame;
        public UnityEngine.UI.Image fill;
        public TextMeshProUGUI label;
        public string displayText = "ITEM";
        [TextArea(2, 4)] public string description = string.Empty;
    }

    [Header("References")]
    [SerializeField] private CanvasGroup mainPageGroup;
    [SerializeField] private CanvasGroup settingsPageGroup;
    [SerializeField] private CanvasGroup creditsPageGroup;
    [SerializeField] private TextMeshProUGUI pathLabel;
    [SerializeField] private TextMeshProUGUI headerStateLabel;
    [SerializeField] private TextMeshProUGUI headerStatusLabel;
    [SerializeField] private TextMeshProUGUI pageTitleLabel;
    [SerializeField] private TextMeshProUGUI pageSubtitleLabel;
    [SerializeField] private TextMeshProUGUI footerLabel;
    [SerializeField] private TextMeshProUGUI mainDescriptionLabel;
    [SerializeField] private TMP_FontAsset menuFont;
    [SerializeField] private MenuItemView[] menuItems = Array.Empty<MenuItemView>();

    [Header("Scene Flow")]
    [SerializeField] private string playSceneName = "GameJamWatchOut";

    [Header("Colors")]
    [SerializeField] private Color normalFrameColor = new Color32(0x7A, 0x44, 0x17, 0xD0);
    [SerializeField] private Color selectedFrameColor = new Color32(0xFF, 0xA1, 0x3D, 0xFF);
    [SerializeField] private Color normalFillColor = new Color32(0x1A, 0x0D, 0x06, 0x00);
    [SerializeField] private Color selectedFillColor = new Color32(0xFF, 0x8B, 0x2A, 0x2E);
    [SerializeField] private Color normalTextColor = new Color32(0xE8, 0x8B, 0x33, 0xFF);
    [SerializeField] private Color selectedTextColor = new Color32(0xFF, 0xC1, 0x7A, 0xFF);

    [Header("Selection")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 selectedScale = new Vector3(1.018f, 1.018f, 1f);

    private int selectedIndex;
    private ViewState currentViewState;

    private void Awake()
    {
        EnsureFont();
        ApplyFontToMenu();
        currentViewState = ViewState.Main;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, menuItems.Length - 1));
        ShowMainMenu();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        switch (currentViewState)
        {
            case ViewState.Main:
                HandleMainMenuInput(keyboard);
                break;
            case ViewState.Settings:
            case ViewState.Credits:
                HandlePanelInput(keyboard);
                break;
        }
    }

    private void OnValidate()
    {
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, menuItems.Length - 1));

        if (!gameObject.scene.IsValid())
        {
            return;
        }

        EnsureFont();
        ApplyFontToMenu();
        ApplyCurrentView();
    }

    private void HandleMainMenuInput(Keyboard keyboard)
    {
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            MoveSelection(-1);
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            MoveSelection(1);
        }
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            ConfirmSelection();
        }
    }

    private void HandlePanelInput(Keyboard keyboard)
    {
        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
        {
            ShowMainMenu();
        }
    }

    private void MoveSelection(int direction)
    {
        if (menuItems == null || menuItems.Length == 0)
        {
            return;
        }

        selectedIndex = (selectedIndex + direction + menuItems.Length) % menuItems.Length;
        ApplyCurrentView();
    }

    private void ConfirmSelection()
    {
        if (menuItems == null || menuItems.Length == 0 || selectedIndex < 0 || selectedIndex >= menuItems.Length)
        {
            return;
        }

        switch (menuItems[selectedIndex].action)
        {
            case MenuAction.Play:
                ExecutePlay();
                break;
            case MenuAction.Settings:
                ShowSettings();
                break;
            case MenuAction.Credits:
                ShowCredits();
                break;
            case MenuAction.Quit:
                ExecuteQuit();
                break;
        }
    }

    private void ExecutePlay()
    {
        if (!CanLoadScene(playSceneName))
        {
            SetText(footerLabel, "BOOT TARGET MISSING // PLAY ACTION LEFT AS PLACEHOLDER");
            return;
        }

        SceneManager.LoadScene(playSceneName);
    }

    private void ExecuteQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowMainMenu()
    {
        currentViewState = ViewState.Main;
        ApplyCurrentView();
    }

    private void ShowSettings()
    {
        currentViewState = ViewState.Settings;
        ApplyCurrentView();
    }

    private void ShowCredits()
    {
        currentViewState = ViewState.Credits;
        ApplyCurrentView();
    }

    private void ApplyCurrentView()
    {
        SetGroupVisible(mainPageGroup, currentViewState == ViewState.Main);
        SetGroupVisible(settingsPageGroup, currentViewState == ViewState.Settings);
        SetGroupVisible(creditsPageGroup, currentViewState == ViewState.Credits);

        SetText(pathLabel, ResolvePathLabel());
        SetText(headerStateLabel, ResolveHeaderStateLabel());
        SetText(headerStatusLabel, ResolveHeaderStatusLabel());
        SetText(pageTitleLabel, ResolvePageTitle());
        SetText(pageSubtitleLabel, ResolvePageSubtitle());
        SetText(footerLabel, ResolveFooterLabel());
        SetText(mainDescriptionLabel, currentViewState == ViewState.Main ? ResolveSelectedDescription() : string.Empty);

        ApplyMenuSelectionState();
    }

    private string ResolvePathLabel()
    {
        return currentViewState switch
        {
            ViewState.Settings => "ROOT://SETTINGS_PLACEHOLDER",
            ViewState.Credits => "ROOT://CREDITS_ARCHIVE",
            _ => "ROOT://MAIN_DIRECTORY"
        };
    }

    private string ResolveHeaderStateLabel()
    {
        return currentViewState switch
        {
            ViewState.Settings => "ACTIVE PAGE // SETTINGS",
            ViewState.Credits => "ACTIVE PAGE // CREDITS",
            _ => "ACTIVE PAGE // MAIN MENU"
        };
    }

    private string ResolveHeaderStatusLabel()
    {
        return currentViewState switch
        {
            ViewState.Settings => "CONFIG NODE ONLINE",
            ViewState.Credits => "ARCHIVE LINK READY",
            _ => string.Empty
        };
    }

    private string ResolvePageTitle()
    {
        return currentViewState switch
        {
            ViewState.Settings => "SETTINGS",
            ViewState.Credits => "CREDITS",
            _ => "WATCH OUT"
        };
    }

    private string ResolvePageSubtitle()
    {
        return currentViewState switch
        {
            ViewState.Settings => "SYSTEM CONFIGURATION PLACEHOLDER",
            ViewState.Credits => "PROJECT CONTRIBUTORS",
            _ => "EMERGENCY ACCESS NODE // PROTOTYPE MENU"
        };
    }

    private string ResolveFooterLabel()
    {
        return currentViewState == ViewState.Main
            ? string.Empty
            : "PRESS ESC TO RETURN TO THE ROOT DIRECTORY";
    }

    private string ResolveSelectedDescription()
    {
        if (menuItems == null || menuItems.Length == 0 || selectedIndex < 0 || selectedIndex >= menuItems.Length)
        {
            return string.Empty;
        }

        return menuItems[selectedIndex].description ?? string.Empty;
    }

    private void ApplyMenuSelectionState()
    {
        bool isMainView = currentViewState == ViewState.Main;

        for (int i = 0; i < menuItems.Length; i++)
        {
            MenuItemView item = menuItems[i];
            if (item == null)
            {
                continue;
            }

            bool isSelected = isMainView && i == selectedIndex;

            if (item.root != null)
            {
                item.root.localScale = isSelected ? selectedScale : normalScale;
            }

            if (item.frame != null)
            {
                item.frame.color = isSelected ? selectedFrameColor : normalFrameColor;
            }

            if (item.fill != null)
            {
                item.fill.color = isSelected ? selectedFillColor : normalFillColor;
            }

            if (item.label != null)
            {
                item.label.text = string.Concat(isSelected ? "> " : "  ", item.displayText);
                item.label.color = isSelected ? selectedTextColor : normalTextColor;
            }
        }
    }

    private void SetGroupVisible(CanvasGroup group, bool isVisible)
    {
        if (group == null)
        {
            return;
        }

        group.gameObject.SetActive(isVisible);
        group.alpha = isVisible ? 1f : 0f;
        group.interactable = isVisible;
        group.blocksRaycasts = isVisible;
    }

    private bool CanLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string buildSceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (string.Equals(sceneName, buildSceneName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sceneName, scenePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureFont()
    {
        if (menuFont != null)
        {
            return;
        }

        Font sourceFont = Resources.Load<Font>(ProjectMenuFontResourcePath);
        if (sourceFont == null)
        {
            return;
        }

        menuFont = TMP_FontAsset.CreateFontAsset(sourceFont);
    }

    private void ApplyFontToMenu()
    {
        if (menuFont == null)
        {
            return;
        }

        ApplyFont(pathLabel);
        ApplyFont(headerStateLabel);
        ApplyFont(headerStatusLabel);
        ApplyFont(pageTitleLabel);
        ApplyFont(pageSubtitleLabel);
        ApplyFont(footerLabel);
        ApplyFont(mainDescriptionLabel);

        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] != null && menuItems[i].label != null)
            {
                menuItems[i].label.font = menuFont;
            }
        }

        ApplyFontToChildren(mainPageGroup);
        ApplyFontToChildren(settingsPageGroup);
        ApplyFontToChildren(creditsPageGroup);
    }

    private void ApplyFont(TextMeshProUGUI label)
    {
        if (label != null)
        {
            label.font = menuFont;
        }
    }

    private void ApplyFontToChildren(CanvasGroup group)
    {
        if (group == null || menuFont == null)
        {
            return;
        }

        TextMeshProUGUI[] labels = group.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].font = menuFont;
        }
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
        {
            label.text = value ?? string.Empty;
        }
    }
}
