namespace Domain.Search;

/// <summary>
/// Upit za pretragu sadržaja (ST-018, US-5): tekst, opciono izvođenje i vrste sadržaja.
/// Prazan upit se odbija pre bilo kakvog čitanja podataka.
/// </summary>
public sealed class SearchQuery
{
    public string Text { get; }
    public Guid? OfferingId { get; }
    public IReadOnlySet<ContentType> Types { get; }

    public SearchQuery(string text, Guid? offeringId = null, IEnumerable<ContentType>? types = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Upit za pretragu ne sme biti prazan.", nameof(text));
        if (offeringId == Guid.Empty)
            throw new ArgumentException("Izvođenje mora biti zadato ili izostavljeno.", nameof(offeringId));

        var selected = (types ?? Array.Empty<ContentType>()).ToHashSet();
        if (selected.Any(t => !Enum.IsDefined(t)))
            throw new ArgumentOutOfRangeException(nameof(types), "Nepoznata vrsta sadržaja.");

        Text = text.Trim();
        OfferingId = offeringId;
        Types = selected.Count == 0 ? Enum.GetValues<ContentType>().ToHashSet() : selected;
    }

    public bool Includes(ContentType type) => Types.Contains(type);
}
