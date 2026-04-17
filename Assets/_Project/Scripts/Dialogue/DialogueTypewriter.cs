using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueTypewriter : MonoBehaviour
{
    private Coroutine revealRoutine;
    private TMP_Text activeTextLabel;
    private Action<char> onCharacterRevealed;
    private Action onRevealCompleted;
    private int visibleCharacterCount;
    private int totalCharacterCount;
    private bool isRevealing;

    public bool IsRevealing => isRevealing;

    public void BeginReveal(TMP_Text textLabel, string fullText, float charactersPerSecond, Action<char> characterCallback, Action completedCallback)
    {
        StopReveal();

        activeTextLabel = textLabel;
        onCharacterRevealed = characterCallback;
        onRevealCompleted = completedCallback;

        if (activeTextLabel == null)
        {
            CompleteReveal();
            return;
        }

        activeTextLabel.text = fullText ?? string.Empty;
        activeTextLabel.maxVisibleCharacters = 0;
        activeTextLabel.ForceMeshUpdate();

        visibleCharacterCount = 0;
        totalCharacterCount = activeTextLabel.textInfo.characterCount;

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
    }

    private IEnumerator RevealRoutine(float charactersPerSecond)
    {
        float revealProgress = 0f;

        while (visibleCharacterCount < totalCharacterCount)
        {
            revealProgress += charactersPerSecond * Time.unscaledDeltaTime;
            int targetVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(revealProgress), 0, totalCharacterCount);

            while (visibleCharacterCount < targetVisibleCharacters)
            {
                RevealNextCharacter();
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
