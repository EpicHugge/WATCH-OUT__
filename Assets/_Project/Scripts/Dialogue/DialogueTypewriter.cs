using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueTypewriter : MonoBehaviour
{
    [Header("Punctuation Pauses")]
    [SerializeField] [Min(0f)] private float commaPause = 0.04f;
    [SerializeField] [Min(0f)] private float sentencePause = 0.08f;
    [SerializeField] [Min(0f)] private float ellipsisPause = 0.16f;

    private Coroutine revealRoutine;
    private TMP_Text activeTextLabel;
    private DialogueProcessedText activeProcessedText = DialogueProcessedText.Empty;
    private Action<char> onCharacterRevealed;
    private Action onRevealCompleted;
    private int visibleCharacterCount;
    private int totalCharacterCount;
    private int nextPauseIndex;
    private bool isRevealing;

    public bool IsRevealing => isRevealing;

    public void BeginReveal(TMP_Text textLabel, DialogueProcessedText processedText, float charactersPerSecond, Action<char> characterCallback, Action completedCallback)
    {
        StopReveal();

        activeTextLabel = textLabel;
        activeProcessedText = processedText ?? DialogueProcessedText.Empty;
        onCharacterRevealed = characterCallback;
        onRevealCompleted = completedCallback;

        if (activeTextLabel == null)
        {
            CompleteReveal();
            return;
        }

        activeTextLabel.text = activeProcessedText.DisplayText;
        activeTextLabel.maxVisibleCharacters = 0;
        activeTextLabel.ForceMeshUpdate();

        visibleCharacterCount = 0;
        totalCharacterCount = activeTextLabel.textInfo.characterCount;
        nextPauseIndex = 0;

        if (totalCharacterCount == 0 || charactersPerSecond <= 0f)
        {
            ShowAllCharacters();
            return;
        }

        isRevealing = true;
        revealRoutine = StartCoroutine(RevealRoutine(charactersPerSecond));
    }

    public void SkipToEnd()
    {
        if (!isRevealing)
        {
            return;
        }

        ShowAllCharacters();
    }

    public void StopReveal()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        isRevealing = false;
        nextPauseIndex = 0;
    }

    private IEnumerator RevealRoutine(float charactersPerSecond)
    {
        float revealProgress = 0f;
        float pendingDelay = 0f;

        while (visibleCharacterCount < totalCharacterCount)
        {
            if (pendingDelay > 0f)
            {
                pendingDelay = Mathf.Max(0f, pendingDelay - Time.unscaledDeltaTime);
                yield return null;
                continue;
            }

            if (TryConsumeInlinePause(out float inlinePause))
            {
                pendingDelay = inlinePause;
                yield return null;
                continue;
            }

            revealProgress += charactersPerSecond * Time.unscaledDeltaTime;
            int targetVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(revealProgress), 0, totalCharacterCount);

            while (visibleCharacterCount < targetVisibleCharacters)
            {
                RevealNextCharacter();
                pendingDelay = GetPunctuationPause(visibleCharacterCount - 1);
                if (pendingDelay > 0f)
                {
                    break;
                }
            }

            yield return null;
        }

        CompleteReveal();
    }

    private void ShowAllCharacters()
    {
        while (visibleCharacterCount < totalCharacterCount)
        {
            visibleCharacterCount++;
        }

        if (activeTextLabel != null)
        {
            activeTextLabel.maxVisibleCharacters = totalCharacterCount;
        }

        CompleteReveal();
    }

    private void RevealNextCharacter()
    {
        if (activeTextLabel == null || visibleCharacterCount >= totalCharacterCount)
        {
            return;
        }

        TMP_CharacterInfo characterInfo = activeTextLabel.textInfo.characterInfo[visibleCharacterCount];
        visibleCharacterCount++;
        activeTextLabel.maxVisibleCharacters = visibleCharacterCount;
        onCharacterRevealed?.Invoke(characterInfo.character);
    }

    private bool TryConsumeInlinePause(out float duration)
    {
        duration = 0f;
        DialoguePauseMarker[] pauseMarkers = activeProcessedText.PauseMarkers;
        if (pauseMarkers == null || nextPauseIndex >= pauseMarkers.Length)
        {
            return false;
        }

        DialoguePauseMarker pauseMarker = pauseMarkers[nextPauseIndex];
        if (pauseMarker.VisibleCharacterIndex != visibleCharacterCount)
        {
            return false;
        }

        nextPauseIndex++;
        duration = Mathf.Max(0f, pauseMarker.Duration);
        return duration > 0f;
    }

    private float GetPunctuationPause(int revealedCharacterIndex)
    {
        if (activeTextLabel == null || revealedCharacterIndex < 0 || revealedCharacterIndex >= totalCharacterCount)
        {
            return 0f;
        }

        TMP_CharacterInfo[] characters = activeTextLabel.textInfo.characterInfo;
        char currentCharacter = characters[revealedCharacterIndex].character;

        if (currentCharacter == '\u2026')
        {
            return ellipsisPause;
        }

        if (currentCharacter == '.')
        {
            bool previousIsDot = revealedCharacterIndex > 0 && characters[revealedCharacterIndex - 1].character == '.';
            bool nextIsDot = revealedCharacterIndex + 1 < totalCharacterCount && characters[revealedCharacterIndex + 1].character == '.';

            if (previousIsDot)
            {
                return nextIsDot ? 0f : ellipsisPause;
            }

            return nextIsDot ? 0f : sentencePause;
        }

        if (currentCharacter == ',' || currentCharacter == ';' || currentCharacter == ':')
        {
            return commaPause;
        }

        if (currentCharacter == '!' || currentCharacter == '?')
        {
            return sentencePause;
        }

        return 0f;
    }

    private void CompleteReveal()
    {
        StopReveal();

        if (activeTextLabel != null)
        {
            activeTextLabel.maxVisibleCharacters = totalCharacterCount;
        }

        Action completedCallback = onRevealCompleted;
        onRevealCompleted = null;
        completedCallback?.Invoke();
    }
}
