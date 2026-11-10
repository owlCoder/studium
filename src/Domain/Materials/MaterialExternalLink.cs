namespace Domain.Materials;

/// <summary>
/// Spoljna veza (URL) povezana sa nastavnim materijalom (ST-009, US-03).
/// Veza pripada tačno jednom materijalu i nasleđuje njegov kontekst izvođenja predmeta.
/// Pravila o dozvoljenim šemama adrese, nazivu veze, broju veza po materijalu
/// i ovlašćenju za dodavanje nisu deo ovog modela i usaglašavaju se posebno.
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
            throw new ArgumentException("Adresa veze mora biti ispravna apsolutna adresa.", nameof(url));

        Id = id;
        MaterialId = materialId;
        Url = parsedUrl;
    }
}
