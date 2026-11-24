namespace Application.Materials;

/// <summary>
/// Zahtev za uklanjanje jednog priloga iz materijala (ST-009, US-07).
/// </summary>
public sealed record RemoveMaterialAttachmentCommand(
    Guid RequestingUserId,
    Guid MaterialId,
    Guid AttachmentId);

/// <summary>
/// Ishod uklanjanja priloga (ST-009, US-07).
/// </summary>
public enum RemoveMaterialAttachmentOutcome
{
    /// <summary>Zapis priloga je uklonjen iz materijala.</summary>
    Removed,

    /// <summary>Materijal ne postoji.</summary>
    MaterialNotFound,

    /// <summary>Prilog ne postoji, pripada drugom materijalu ili je već uklonjen.</summary>
    AttachmentNotFound
}

/// <summary>
/// Slučaj upotrebe: nastavnik uklanja prilog iz materijala (ST-009, US-07, tačke 1 i 2).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → prilog mora postojati i pripadati tom materijalu →
/// uklanja se zapis priloga → briše se sadržaj datoteke iz skladišta.
/// Prilog se traži tek posle provere prava, pa korisnik bez prava ne može da sazna
/// koji prilozi postoje. Prilog drugog materijala se ne može ukloniti preko ovog materijala.
/// Zapis se uklanja pre datoteke: ako brisanje datoteke ne uspe, ostaje samo sadržaj bez
/// zapisa, koji niko ne vidi niti može da preuzme, a nikad zapis priloga bez datoteke
/// (poslovno pravilo ST-009).
/// </summary>
public sealed class RemoveMaterialAttachmentHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IAttachmentFileStorage _storage;
    private readonly IOfferingStaffChecker _staffChecker;

    public RemoveMaterialAttachmentHandler(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IAttachmentFileStorage storage,
        IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _attachments = attachments;
        _storage = storage;
        _staffChecker = staffChecker;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    public async Task<RemoveMaterialAttachmentOutcome> HandleAsync(
        RemoveMaterialAttachmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return RemoveMaterialAttachmentOutcome.MaterialNotFound;

        var isStaff = command.RequestingUserId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može uklanjati priloge materijala.");
        }

        var attachment = await _attachments.GetByIdAsync(command.AttachmentId, cancellationToken);
        if (attachment is null || attachment.MaterialId != material.Id)
            return RemoveMaterialAttachmentOutcome.AttachmentNotFound;

        // Istovremeni zahtev je mogao da ukloni isti prilog između čitanja i uklanjanja.
        var removed = await _attachments.RemoveAsync(attachment.Id, cancellationToken);
        if (!removed)
            return RemoveMaterialAttachmentOutcome.AttachmentNotFound;

        await TryDeleteStoredContentAsync(attachment.StorageKey);
        return RemoveMaterialAttachmentOutcome.Removed;
    }

    private async Task TryDeleteStoredContentAsync(string storageKey)
    {
        try
        {
            // Bez tokena otkazivanja: zapis je već uklonjen, pa datoteka treba da se obriše
            // i kada je zahtev u međuvremenu otkazan.
            await _storage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception)
        {
            // Prilog je uklonjen iz materijala; zaostali sadržaj nema zapis, pa ga korisnik
            // ne vidi niti može da preuzme. Greška brisanja ne sme da poništi uspešno uklanjanje.
        }
    }
}
