namespace Domain.Search;

/// <summary>
/// Pravi kratak odlomak opisa za prikaz u rezultatima pretrage (ST-018, US-7).
/// Odlomak se pravi samo iz opisa, ima najviše <see cref="MaxLength"/> znakova,
/// ne seče reč i počinje blizu prvog pogotka.
/// </summary>
public static class SnippetBuilder
{
    public const int MaxLength = 160;
    private const int ContextBeforeMatch = 40;

    public static string Build(string? description, string? term)
    {
        var text = description?.Trim() ?? string.Empty;
        if (text.Length <= MaxLength)
            return text;

        var match = string.IsNullOrWhiteSpace(term)
            ? -1
            : text.IndexOf(term.Trim(), StringComparison.OrdinalIgnoreCase);
        if (match < 0)
            match = 0;

        var start = Math.Max(0, match - ContextBeforeMatch);
        if (start > 0)
        {
            var nextSpace = text.IndexOf(' ', start);
            if (nextSpace >= 0 && nextSpace < match)
                start = nextSpace + 1;
            else if (text[start - 1] != ' ')
                start = match;
        }

        var end = Math.Min(text.Length, start + MaxLength);
        if (end < text.Length && text[end] != ' ')
        {
            var lastSpace = text.LastIndexOf(' ', end - 1, end - start);
            if (lastSpace > start)
                end = lastSpace;
        }

        return text[start..end].Trim();
    }
}
