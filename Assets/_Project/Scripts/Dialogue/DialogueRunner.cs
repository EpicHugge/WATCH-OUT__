using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DisallowMultipleComponent]
public sealed class DialogueRunner : MonoBehaviour
{
    private const string ContinuePromptText = "Continue [Space]";
    private const string EndConversationPromptText = "End Conversation [Space]";

    [Header("References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerInteractionController interactionController;
    [SerializeField] private FirstPersonPlayerController playerController;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueTypewriter typewriter;
    [SerializeField] private DialogueVoicePlayer voicePlayer;

    [Header("Input")]
    [SerializeField] private Key keyboardAdvanceKey = Key.Space;
    [SerializeField] private Key keyboardAlternateAdvanceKey = Key.None;
    [SerializeField] private GamepadButton gamepadAdvanceButton = GamepadButton.North;

    [Header("Reveal")]
    [SerializeField] [Min(1f)] private float charactersPerSecond = 60f;

    private DialogueConversation currentConversation;
    private int currentLineIndex = -1;
    private bool restoreInteractionEnabled;
    private bool restorePlayerControllerEnabled;
    private bool waitForAdvanceRelease;

    public bool IsRunning => currentConversation != null;

    private void Awake()
    {
        EnsureReferences();

        if (dialogueUI != null)
        {
            dialogueUI.SetVisible(false);
        }
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (waitForAdvanceRelease)
        {
            if (!IsAdvanceHeld())
            {
                waitForAdvanceRelease = false;
            }

            return;
        }

        if (!WasAdvancePressedThisFrame())
        {
            return;
        }

        AdvanceOrSkip();
    }

    public bool StartConversation(DialogueConversation conversation)
    {
        if (conversation == null || conversation.LineCount == 0)
        {
            return false;
        }

        EnsureReferences();

        if (IsRunning)
        {
            EndConversation();
        }

        currentConversation = conversation;
        currentLineIndex = -1;
        waitForAdvanceRelease = true;

        if (interactionController != null)
        {
            restoreInteractionEnabled = interactionController.InteractionEnabled;
            interactionController.SetInteractionEnabled(false);
        }

        if (playerController != null)
        {
            restorePlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        dialogueUI.SetVisible(true);
        ShowNextLine();
        return true;
    }

    public void AdvanceOrSkip()
    {
        if (!IsRunning)
        {
            return;
        }

        if (typewriter != null && typewriter.IsRevealing)
        {
            SkipCurrentReveal();
            return;
        }

        ShowNextLine();
    }

    public void SkipCurrentReveal()
    {
        if (typewriter == null || !typewriter.IsRevealing)
        {
            return;
        }

        typewriter.SkipToEnd();
    }

    public void EndConversation()
    {
        currentConversation = null;
        currentLineIndex = -1;
        waitForAdvanceRelease = false;

        if (typewriter != null)
        {
            typewriter.StopReveal();
        }

        if (voicePlayer != null)
        {
            voicePlayer.StopPlayback();
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetVisible(false);
            dialogueUI.SetContinueVisible(false);
        }

        if (interactionController != null)
        {
            interactionController.SetInteractionEnabled(restoreInteractionEnabled);
            interactionController.RefreshCurrentTarget();
        }

        if (playerController != null)
        {
            playerController.enabled = restorePlayerControllerEnabled;
        }
    }

    private void EnsureReferences()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (interactionController == null)
        {
            interactionController = GetComponent<PlayerInteractionController>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<FirstPersonPlayerController>();
        }

        if (dialogueUI == null)
        {
            dialogueUI = GetComponentInChildren<DialogueUI>(true);
        }

        if (dialogueUI == null)
        {
            dialogueUI = DialogueUI.Create(transform);
        }

        if (typewriter == null)
        {
            typewriter = GetComponent<DialogueTypewriter>();
        }

        if (typewriter == null)
        {
            typewriter = gameObject.AddComponent<DialogueTypewriter>();
        }

        if (voicePlayer == null)
        {
            voicePlayer = GetComponent<DialogueVoicePlayer>();
        }

        if (voicePlayer == null)
        {
            voicePlayer = gameObject.AddComponent<DialogueVoicePlayer>();
        }
    }

    private void ShowNextLine()
    {
        if (currentConversation == null)
        {
            EndConversation();
            return;
        }

        currentLineIndex++;
        if (currentLineIndex >= currentConversation.LineCount)
        {
            EndConversation();
            return;
        }

        DialogueLine line = currentConversation.Lines[currentLineIndex];
        dialogueUI.ShowLine(line);
        dialogueUI.SetContinueText(IsShowingLastLine() ? EndConversationPromptText : ContinuePromptText);

        float revealSpeed = currentConversation.ResolveCharactersPerSecond(line, charactersPerSecond);
        DialogueVoiceProfile voiceProfile = currentConversation.ResolveVoiceProfile(line);
        voicePlayer.BeginLine(voiceProfile);
        typewriter.BeginReveal(dialogueUI.BodyText, line.DialogueText, revealSpeed, voicePlayer.ProcessRevealedCharacter, OnLineRevealCompleted);
    }

    private void OnLineRevealCompleted()
    {
        if (voicePlayer != null)
        {
            voicePlayer.StopPlayback();
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetContinueVisible(true);
        }
    }

    private bool IsShowingLastLine()
    {
        return currentConversation != null && currentLineIndex == currentConversation.LineCount - 1;
    }

    private bool WasAdvancePressedThisFrame()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current[keyboardAdvanceKey].wasPressedThisFrame)
            {
                return true;
            }

            if (keyboardAlternateAdvanceKey != Key.None &&
                keyboardAlternateAdvanceKey != keyboardAdvanceKey &&
                Keyboard.current[keyboardAlternateAdvanceKey].wasPressedThisFrame)
            {
                return true;
            }
        }

        return Gamepad.current != null && Gamepad.current[gamepadAdvanceButton].wasPressedThisFrame;
    }

    private bool IsAdvanceHeld()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current[keyboardAdvanceKey].isPressed)
            {
                return true;
            }

            if (keyboardAlternateAdvanceKey != Key.None &&
                keyboardAlternateAdvanceKey != keyboardAdvanceKey &&
                Keyboard.current[keyboardAlternateAdvanceKey].isPressed)
            {
                return true;
            }
        }

        return Gamepad.current != null && Gamepad.current[gamepadAdvanceButton].isPressed;
    }
}
