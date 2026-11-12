namespace Domain.Materials;

/// <summary>
/// Spoljna veza (URL) povezana sa nastavnim materijalom (ST-009, US-03).
/// Veza pripada tačno jednom materijalu i nasleđuje njegov kontekst izvođenja predmeta.
/// Adresa mora biti puna web adresa: šema <c>http</c> ili <c>https</c>, zatim <c>://</c> i host
/// (npr. <c>https://example.org/predavanje</c>).
/// Pravila o nazivu veze, broju veza po materijalu i ovlašćenju za dodavanje
/// nisu deo ovog modela i usaglašavaju se posebno.
/// </summary>
public sealed class MaterialExternalLink
{
    public Guid Id { get; }

    /// <summary>
    /// Identifikator materijala kojem veza pripada.
    /// </summary>
    public Guid MaterialId { get; }

    /// <summary>
    /// Apsolutna adresa spoljnog resursa.
    /// </summary>
    public Uri Url { get; }

    public MaterialExternalLink(Guid id, Guid materialId, string url)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator veze mora biti zadat.", nameof(id));
        if (materialId == Guid.Empty)
            throw new ArgumentException("Materijal mora biti zadat.", nameof(materialId));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Adresa veze je obavezna.", nameof(url));
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsedUrl))
            throw new ArgumentException(
                "Adresa veze mora biti puna adresa, npr. https://example.org/predavanje.", nameof(url));
        if (!IsWebAddress(parsedUrl))
            throw new ArgumentException(
                "Adresa veze mora počinjati sa http:// ili https:// i sadržati naziv sajta.", nameof(url));

        Id = id;
        MaterialId = materialId;
        Url = parsedUrl;
    }

    private static bool IsWebAddress(Uri url) =>
        (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrEmpty(url.Host);
}
