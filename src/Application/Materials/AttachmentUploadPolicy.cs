namespace Application.Materials;

/// <summary>
/// Pravila za priloge nastavnog materijala (ST-009, US-05): dozvoljene ekstenzije
/// datoteka i maksimalna veličina jednog priloga. Vrednosti se zadaju konfiguracijom
/// u spoljnom sloju (<c>Api</c>). Provera konkretne datoteke nije deo ove klase.
/// </summary>
public sealed class AttachmentUploadPolicy
{
    /// <summary>
    /// Dozvoljene ekstenzije, malim slovima i sa vodećom tačkom (npr. <c>.pdf</c>).
    /// </summary>
    public IReadOnlySet<string> AllowedExtensions { get; }

    /// <summary>
    /// Maksimalna veličina jednog priloga u bajtovima.
    /// </summary>
    public long MaxSizeInBytes { get; }

    public AttachmentUploadPolicy(IEnumerable<string> allowedExtensions, long maxSizeInBytes)
    {
        ArgumentNullException.ThrowIfNull(allowedExtensions);

        if (maxSizeInBytes <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxSizeInBytes), "Maksimalna veličina priloga mora biti veća od nule.");

        var normalized = new HashSet<string>(StringComparer.Ordinal);
        foreach (var extension in allowedExtensions)
        {
            normalized.Add(NormalizeExtension(extension));
        }

        if (normalized.Count == 0)
            throw new ArgumentException(
                "Mora biti zadata bar jedna dozvoljena ekstenzija priloga.", nameof(allowedExtensions));

        AllowedExtensions = normalized;
        MaxSizeInBytes = maxSizeInBytes;
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Dozvoljena ekstenzija ne sme biti prazna.", "allowedExtensions");

        var trimmed = extension.Trim().ToLowerInvariant();

        if (trimmed.Length < 2 || trimmed[0] != '.' || trimmed.Any(char.IsWhiteSpace))
            throw new ArgumentException(
                $"Ekstenzija '{extension}' nije ispravna; očekuje se oblik '.pdf'.", "allowedExtensions");

        return trimmed;
    }
}
