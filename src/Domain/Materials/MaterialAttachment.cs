namespace Domain.Materials;

/// <summary>
/// Prilog nastavnog materijala (ST-009, US-04).
/// Čuva izvorni naziv, tip i veličinu datoteke, kao i ključ pod kojim je
/// sadržaj sačuvan u skladištu datoteka. Prilog nema sopstvena prava pristupa:
/// nasleđuje pravo pristupa materijala kome pripada.
/// Ograničenja dozvoljenih tipova i maksimalne veličine nisu deo ovog modela (US-05).
/// </summary>
public sealed class MaterialAttachment
{
    public Guid Id { get; }

    public Guid MaterialId { get; }

    /// <summary>
    /// Naziv datoteke kakav je poslao nastavnik; koristi se za prikaz i preuzimanje,
    /// a ne kao putanja u skladištu.
    /// </summary>
    public string OriginalFileName { get; }

    /// <summary>
    /// Tip sadržaja (MIME), npr. <c>application/pdf</c>.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Veličina sadržaja u bajtovima.
    /// </summary>
    public long SizeInBytes { get; }

    /// <summary>
    /// Ključ sadržaja u skladištu datoteka (<c>IAttachmentFileStorage</c>).
    /// </summary>
    public string StorageKey { get; }

    public MaterialAttachment(
        Guid id,
        Guid materialId,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string storageKey)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator priloga mora biti zadat.", nameof(id));
        if (materialId == Guid.Empty)
            throw new ArgumentException("Materijal mora biti zadat.", nameof(materialId));
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Izvorni naziv priloga je obavezan.", nameof(originalFileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Tip priloga je obavezan.", nameof(contentType));
        if (sizeInBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Veličina priloga ne može biti negativna.");
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Ključ sadržaja u skladištu je obavezan.", nameof(storageKey));

        Id = id;
        MaterialId = materialId;
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        SizeInBytes = sizeInBytes;
        StorageKey = storageKey;
    }
}
