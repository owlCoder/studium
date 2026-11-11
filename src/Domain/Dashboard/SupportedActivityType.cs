namespace Domain.Dashboard;

public sealed class SupportedActivityType
{
    public string Code { get; }
    public FunctionalArea Area { get; }
    public string LinkTemplate { get; }

    public SupportedActivityType(string code, FunctionalArea area, string linkTemplate)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Oznaka vrste aktivnosti je obavezna.", nameof(code));
        }
        if (!Enum.IsDefined(area))
        {
            throw new ArgumentException("Funkcionalna oblast nije poznata.", nameof(area));
        }
        if (string.IsNullOrWhiteSpace(linkTemplate) || !linkTemplate.Trim().StartsWith('/'))
        {
            throw new ArgumentException("Šablon veze mora biti relativna putanja koja počinje sa '/'.", nameof(linkTemplate));
        }

        Code = Normalize(code)!;
        Area = area;
        LinkTemplate = linkTemplate.Trim();
    }

    public string BuildLink(Guid offeringId, Guid itemId)
    {
        return LinkTemplate
            .Replace("{offeringId}", offeringId.ToString(), StringComparison.Ordinal)
            .Replace("{itemId}", itemId.ToString(), StringComparison.Ordinal);
    }

    internal static string? Normalize(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToLowerInvariant();
    }
}
