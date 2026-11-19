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
/// Slučaj upotrebe: nastavnik uklanja prilog iz materijala (ST-009, US-07, tačka 1).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → prilog mora postojati i pripadati tom materijalu →
/// uklanja se zapis priloga.
/// Prilog se traži tek posle provere prava, pa korisnik bez prava ne može da sazna
/// koji prilozi postoje. Prilog drugog materijala se ne može ukloniti preko ovog materijala.
/// NAPOMENA: brisanje sadržaja datoteke iz skladišta je tačka 2 US-07 i nije u ovom koraku.
/// </summary>
public sealed class RemoveMaterialAttachmentHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IOfferingStaffChecker _staffChecker;

    public RemoveMaterialAttachmentHandler(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _attachments = attachments;
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
        return removed
            ? RemoveMaterialAttachmentOutcome.Removed
            : RemoveMaterialAttachmentOutcome.AttachmentNotFound;
    }
}
