using System.Collections.Generic;

public static class InteractionOutlineRegistry
{
    private static readonly List<InteractionOutlineHighlight> ActiveHighlights = new List<InteractionOutlineHighlight>();

    public static IReadOnlyList<InteractionOutlineHighlight> Highlights => ActiveHighlights;
    public static bool HasHighlights => ActiveHighlights.Count > 0;

    public static void Register(InteractionOutlineHighlight highlight)
    {
        if (highlight == null || ActiveHighlights.Contains(highlight))
        {
            return;
        }

        ActiveHighlights.Add(highlight);
    }

    public static void Unregister(InteractionOutlineHighlight highlight)
    {
        if (highlight == null)
        {
            return;
        }

        ActiveHighlights.Remove(highlight);
    }
}
