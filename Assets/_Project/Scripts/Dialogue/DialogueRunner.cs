using System;
using System.Collections;
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
    [SerializeField] private DialogueCameraShake cameraShake;

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
    private Coroutine autoAdvanceRoutine;

    public bool IsRunning => currentConversation != null;
    public DialogueConversation CurrentConversation => currentConversation;
    public event Action<DialogueConversation> ConversationStarted;
    public event Action<DialogueConversation> ConversationEnded;

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
        ConversationStarted?.Invoke(currentConversation);
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

        CancelAutoAdvance();
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
        DialogueConversation endedConversation = currentConversation;
        currentConversation = null;
        currentLineIndex = -1;
        waitForAdvanceRelease = false;
        CancelAutoAdvance();

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

        if (endedConversation != null)
        {
            ConversationEnded?.Invoke(endedConversation);
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

        if (cameraShake == null)
        {
            cameraShake = GetComponent<DialogueCameraShake>();
        }

        if (cameraShake == null)
        {
            cameraShake = gameObject.AddComponent<DialogueCameraShake>();
        }

        if (cameraShake != null)
        {
            Transform shakeTarget = playerController != null ? playerController.CameraPivot : null;
            if (shakeTarget == null && Camera.main != null)
            {
                shakeTarget = Camera.main.transform;
            }

            cameraShake.SetTarget(shakeTarget);
        }
    }

    private void ShowNextLine()
    {
        if (currentConversation == null)
        {
            EndConversation();
            return;
        }

        CancelAutoAdvance();
        currentLineIndex++;
        if (currentLineIndex >= currentConversation.LineCount)
        {
            EndConversation();
            return;
        }

        DialogueLine line = currentConversation.Lines[currentLineIndex];
        DialogueProcessedText processedText = DialogueTextProcessor.Process(line.DialogueText);
        dialogueUI.ShowLine(line, processedText);
        dialogueUI.SetContinueText(IsShowingLastLine() ? EndConversationPromptText : ContinuePromptText);

        float revealSpeed = currentConversation.ResolveCharactersPerSecond(line, charactersPerSecond);
        DialogueVoiceProfile voiceProfile = currentConversation.ResolveVoiceProfile(line);
        voicePlayer.BeginLine(voiceProfile);
        RunLineEvent(line, DialogueLineEventTiming.OnLineStart);
        typewriter.BeginReveal(dialogueUI.BodyText, processedText, revealSpeed, voicePlayer.ProcessRevealedCharacter, OnLineRevealCompleted);
    }

    private void OnLineRevealCompleted()
    {
        if (voicePlayer != null)
        {
            voicePlayer.StopPlayback();
        }

        DialogueLine currentLine = GetCurrentLine();
        RunLineEvent(currentLine, DialogueLineEventTiming.OnLineComplete);

        if (dialogueUI != null)
        {
            dialogueUI.SetContinueVisible(currentLine != null && !currentLine.AutoAdvance);
        }

        if (currentLine != null && currentLine.AutoAdvance)
        {
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceAfterDelay(currentLine.AutoAdvanceDelay));
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

    private DialogueLine GetCurrentLine()
    {
        if (currentConversation == null || currentLineIndex < 0 || currentLineIndex >= currentConversation.LineCount)
        {
            return null;
        }

        return currentConversation.Lines[currentLineIndex];
    }

    private void RunLineEvent(DialogueLine line, DialogueLineEventTiming triggerTiming)
    {
        if (line == null || line.LineEvent == null || !line.LineEvent.TriggersAt(triggerTiming))
        {
            return;
        }

        if (line.LineEvent.EventType == DialogueLineEventType.CameraShake && cameraShake != null)
        {
            cameraShake.PlayShake(line.LineEvent.Duration, line.LineEvent.Magnitude, line.LineEvent.Frequency);
        }
    }

    private IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        float remainingDelay = Mathf.Max(0f, delay);
        while (remainingDelay > 0f)
        {
            remainingDelay -= Time.unscaledDeltaTime;
            yield return null;
        }

        autoAdvanceRoutine = null;
        ShowNextLine();
    }

    private void CancelAutoAdvance()
    {
        if (autoAdvanceRoutine == null)
        {
            return;
        }

        StopCoroutine(autoAdvanceRoutine);
        autoAdvanceRoutine = null;
    }
}
