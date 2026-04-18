using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class DialogueTextProcessor
{
    public static DialogueProcessedText Process(string rawText)
    {
        string sourceText = rawText ?? string.Empty;
        if (sourceText.Length == 0)
        {
            return DialogueProcessedText.Empty;
        }

        StringBuilder builder = new StringBuilder(sourceText.Length);
        List<DialogueTextEffectSpan> effectSpans = new List<DialogueTextEffectSpan>();
        List<DialoguePauseMarker> pauseMarkers = new List<DialoguePauseMarker>();
        List<DialogueTextEffect> effectStack = new List<DialogueTextEffect>();

        int visibleCharacterIndex = 0;
        int activeEffectStart = 0;
        DialogueTextEffect activeEffect = DialogueTextEffect.None;

        for (int i = 0; i < sourceText.Length; i++)
        {
            if (sourceText[i] == '[' && TryParsePauseTag(sourceText, i, out float pauseDuration, out int pauseTagLength))
            {
                pauseMarkers.Add(new DialoguePauseMarker(visibleCharacterIndex, pauseDuration));
                i += pauseTagLength - 1;
                continue;
            }

            if (sourceText[i] == '<' && TryReadTag(sourceText, i, out string tagContent, out int tagLength))
            {
                if (TryApplyEffectTag(tagContent, effectStack, effectSpans, visibleCharacterIndex, ref activeEffect, ref activeEffectStart))
                {
                    i += tagLength - 1;
                    continue;
                }

                builder.Append(sourceText, i, tagLength);
                i += tagLength - 1;
                continue;
            }

            builder.Append(sourceText[i]);
            visibleCharacterIndex++;
        }

        CloseActiveSpan(effectSpans, visibleCharacterIndex, ref activeEffect, ref activeEffectStart, DialogueTextEffect.None);
        return new DialogueProcessedText(builder.ToString(), effectSpans.ToArray(), pauseMarkers.ToArray());
    }

    private static bool TryParsePauseTag(string sourceText, int startIndex, out float duration, out int tagLength)
    {
        duration = 0f;
        tagLength = 0;

        int endIndex = sourceText.IndexOf(']', startIndex);
        if (endIndex < 0)
        {
            return false;
        }

        string tagContent = sourceText.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
        if (!tagContent.StartsWith("pause=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string durationText = tagContent.Substring("pause=".Length);
        if (!float.TryParse(durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out duration))
        {
            return false;
        }

        duration = Math.Max(0f, duration);
        tagLength = endIndex - startIndex + 1;
        return true;
    }

    private static bool TryReadTag(string sourceText, int startIndex, out string tagContent, out int tagLength)
    {
        tagContent = string.Empty;
        tagLength = 0;

        int endIndex = sourceText.IndexOf('>', startIndex);
        if (endIndex < 0)
        {
            return false;
        }

        tagContent = sourceText.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
        tagLength = endIndex - startIndex + 1;
        return true;
    }

    private static bool TryApplyEffectTag(
        string tagContent,
        List<DialogueTextEffect> effectStack,
        List<DialogueTextEffectSpan> effectSpans,
        int visibleCharacterIndex,
        ref DialogueTextEffect activeEffect,
        ref int activeEffectStart)
    {
        bool isClosingTag = tagContent.StartsWith("/", StringComparison.Ordinal);
        string tagName = isClosingTag ? tagContent.Substring(1).Trim() : tagContent;
        DialogueTextEffect effect = ParseEffectTag(tagName);
        if (effect == DialogueTextEffect.None)
        {
            return false;
        }

        if (!isClosingTag)
        {
            effectStack.Add(effect);
            CloseActiveSpan(effectSpans, visibleCharacterIndex, ref activeEffect, ref activeEffectStart, effect);
            return true;
        }

        for (int i = effectStack.Count - 1; i >= 0; i--)
        {
            if (effectStack[i] == effect)
            {
                effectStack.RemoveAt(i);
                DialogueTextEffect nextEffect = effectStack.Count > 0 ? effectStack[effectStack.Count - 1] : DialogueTextEffect.None;
                CloseActiveSpan(effectSpans, visibleCharacterIndex, ref activeEffect, ref activeEffectStart, nextEffect);
                return true;
            }
        }

        return false;
    }

    private static void CloseActiveSpan(
        List<DialogueTextEffectSpan> effectSpans,
        int visibleCharacterIndex,
        ref DialogueTextEffect activeEffect,
        ref int activeEffectStart,
        DialogueTextEffect nextEffect)
    {
        if (activeEffect != DialogueTextEffect.None && visibleCharacterIndex > activeEffectStart)
        {
            effectSpans.Add(new DialogueTextEffectSpan(activeEffectStart, visibleCharacterIndex, activeEffect));
        }

        activeEffect = nextEffect;
        activeEffectStart = visibleCharacterIndex;
    }

    private static DialogueTextEffect ParseEffectTag(string tagName)
    {
        if (string.Equals(tagName, "shake", StringComparison.OrdinalIgnoreCase))
        {
            return DialogueTextEffect.Shake;
        }

        if (string.Equals(tagName, "wave", StringComparison.OrdinalIgnoreCase))
        {
            return DialogueTextEffect.Wave;
        }

        if (string.Equals(tagName, "glitch", StringComparison.OrdinalIgnoreCase))
        {
            return DialogueTextEffect.Glitch;
        }

        return DialogueTextEffect.None;
    }
}

public sealed class DialogueProcessedText
{
    public static readonly DialogueProcessedText Empty =
        new DialogueProcessedText(string.Empty, Array.Empty<DialogueTextEffectSpan>(), Array.Empty<DialoguePauseMarker>());

    public DialogueProcessedText(string displayText, DialogueTextEffectSpan[] effectSpans, DialoguePauseMarker[] pauseMarkers)
    {
        DisplayText = displayText ?? string.Empty;
        EffectSpans = effectSpans ?? Array.Empty<DialogueTextEffectSpan>();
        PauseMarkers = pauseMarkers ?? Array.Empty<DialoguePauseMarker>();
    }

    public string DisplayText { get; }
    public DialogueTextEffectSpan[] EffectSpans { get; }
    public DialoguePauseMarker[] PauseMarkers { get; }
}

public readonly struct DialogueTextEffectSpan
{
    public DialogueTextEffectSpan(int startIndex, int endIndex, DialogueTextEffect effect)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
        Effect = effect;
    }

    public int StartIndex { get; }
    public int EndIndex { get; }
    public DialogueTextEffect Effect { get; }

    public bool Contains(int characterIndex)
    {
        return characterIndex >= StartIndex && characterIndex < EndIndex;
    }
}

public readonly struct DialoguePauseMarker
{
    public DialoguePauseMarker(int visibleCharacterIndex, float duration)
    {
        VisibleCharacterIndex = visibleCharacterIndex;
        Duration = duration;
    }

    public int VisibleCharacterIndex { get; }
    public float Duration { get; }
}
