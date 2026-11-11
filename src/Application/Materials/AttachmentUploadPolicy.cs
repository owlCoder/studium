namespace Application.Materials;

/// <summary>
/// Pravila za priloge nastavnog materijala (ST-009, US-05): dozvoljene ekstenzije
/// datoteka i maksimalna veličina jednog priloga. Vrednosti se zadaju konfiguracijom
/// u spoljnom sloju (<c>Api</c>). Klasa proverava naziv i veličinu konkretne datoteke,
/// a slučaj upotrebe otpremanja odlučuje šta se dešava sa odbijenom datotekom.
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
        AllowedExtensionsDisplay = string.Join(", ", normalized.Order(StringComparer.Ordinal));
        MaxSizeDisplay = FormatSize(maxSizeInBytes);
    }

    /// <summary>
    /// Dozvoljene ekstenzije za prikaz korisniku, azbučno i odvojene zarezom
    /// (npr. <c>.docx, .pdf, .zip</c>).
    /// </summary>
    public string AllowedExtensionsDisplay { get; }

    /// <summary>
    /// Maksimalna veličina za prikaz korisniku (npr. <c>20 MB</c>), gde je 1 MB = 1024 × 1024 bajtova.
    /// </summary>
    public string MaxSizeDisplay { get; }

    private static string FormatSize(long bytes)
    {
        const long kilobyte = 1024;
        const long megabyte = kilobyte * 1024;

        if (bytes >= megabyte)
            return OneDecimalRoundedDown(bytes, megabyte) + " MB";
        if (bytes >= kilobyte)
            return OneDecimalRoundedDown(bytes, kilobyte) + " KB";
        return bytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " B";
    }

    // Zaokružuje se naniže, da prikazana granica nikad ne bude veća od stvarne.
    private static string OneDecimalRoundedDown(long bytes, long unit) =>
        (Math.Floor(bytes * 10.0 / unit) / 10).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Da li je tip datoteke dozvoljen, prema ekstenziji iz izvornog naziva
    /// (bez obzira na velika i mala slova). Proverava se samo poslednja ekstenzija,
    /// pa se npr. <c>skripta.pdf.exe</c> odbija. Naziv bez ekstenzije nije dozvoljen.
    /// </summary>
    public bool IsAllowedFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName.Trim());
        return extension.Length > 1 && AllowedExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Da li veličina u bajtovima ne prelazi dozvoljeni maksimum (granica je uključena).
    /// </summary>
    public bool IsWithinMaxSize(long sizeInBytes) =>
        sizeInBytes >= 0 && sizeInBytes <= MaxSizeInBytes;

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
