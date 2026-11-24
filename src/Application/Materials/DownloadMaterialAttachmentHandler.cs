using Application.Access;
using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Zahtev za preuzimanje jednog priloga materijala (ST-009, zaštita pristupa, tačka 1).
/// </summary>
public sealed record DownloadMaterialAttachmentCommand(
    Guid RequestingUserId,
    Guid MaterialId,
    Guid AttachmentId);

/// <summary>
/// Ishod preuzimanja priloga.
/// </summary>
public enum DownloadMaterialAttachmentOutcome
{
    /// <summary>Pristup je dozvoljen i sadržaj je otvoren.</summary>
    Found,

    /// <summary>Materijal ne postoji.</summary>
    MaterialNotFound,

    /// <summary>Prilog ne postoji ili pripada drugom materijalu.</summary>
    AttachmentNotFound,

    /// <summary>Zapis priloga postoji, ali sadržaj datoteke nije u skladištu.</summary>
    ContentMissing
}

/// <summary>
/// Rezultat preuzimanja. <see cref="Attachment"/> i <see cref="Content"/> su zadati
/// samo za <see cref="DownloadMaterialAttachmentOutcome.Found"/>; pozivalac zatvara tok.
/// </summary>
public sealed record DownloadMaterialAttachmentResult(
    DownloadMaterialAttachmentOutcome Outcome,
    MaterialAttachment? Attachment = null,
    Stream? Content = null);

/// <summary>
/// Slučaj upotrebe: preuzimanje priloga materijala (ST-009, priča „Zaštita pristupa
/// materijalu i prilozima”, tačka 1: provera članstva pri svakom preuzimanju).
/// Prilog nema sopstvena prava: nasleđuje pravo materijala, a materijal pripada izvođenju.
/// Pravo ima korisnik koji je nastavno osoblje tog izvođenja (ST-005) ili aktivan student
/// tog izvođenja (ST-006). Provera se radi pri svakom pozivu, bez pamćenja ranijeg ishoda,
/// pa prekinuto članstvo odmah zatvara pristup.
/// Tačka 2: student preuzima samo iz objavljenog materijala. Za nacrt i povučen materijal
/// student dobija isti ishod kao za nepostojeći materijal (<see cref="DownloadMaterialAttachmentOutcome.MaterialNotFound"/>),
/// pa ni direktnom adresom ne saznaje da takav materijal postoji. Osoblje izvođenja
/// pristupa u svakom stanju.
/// Redosled: materijal mora postojati → pravo nad izvođenjem materijala → za studenta
/// materijal mora biti objavljen → prilog mora postojati i pripadati tom materijalu →
/// otvara se sadržaj. Prilog se traži tek posle provere prava, pa korisnik bez prava
/// ne može da sazna koji prilozi postoje.
/// </summary>
public sealed class DownloadMaterialAttachmentHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IAttachmentFileStorage _storage;
    private readonly IOfferingStaffChecker _staffChecker;
    private readonly IMembershipReader _memberships;

    public DownloadMaterialAttachmentHandler(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IAttachmentFileStorage storage,
        IOfferingStaffChecker staffChecker,
        IMembershipReader memberships)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(staffChecker);
        ArgumentNullException.ThrowIfNull(memberships);

        _materials = materials;
        _attachments = attachments;
        _storage = storage;
        _staffChecker = staffChecker;
        _memberships = memberships;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat, niti je osoblje, niti aktivan student izvođenja materijala.
    /// </exception>
    public async Task<DownloadMaterialAttachmentResult> HandleAsync(
        DownloadMaterialAttachmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return new(DownloadMaterialAttachmentOutcome.MaterialNotFound);

        var access = await GetAccessAsync(command.RequestingUserId, material, cancellationToken);
        if (access == MaterialAccess.None)
        {
            throw new MaterialAccessDeniedException(
                "Prilog mogu preuzeti samo nastavno osoblje i upisani studenti izvođenja materijala.");
        }

        // Tačka 2: nacrt i povučen materijal su za studenta kao da ne postoje.
        if (access == MaterialAccess.Student && !material.PublicationState.IsVisibleToStudents())
            return new(DownloadMaterialAttachmentOutcome.MaterialNotFound);

        var attachment = await _attachments.GetByIdAsync(command.AttachmentId, cancellationToken);
        if (attachment is null || attachment.MaterialId != material.Id)
            return new(DownloadMaterialAttachmentOutcome.AttachmentNotFound);

        var content = await _storage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        if (content is null)
            return new(DownloadMaterialAttachmentOutcome.ContentMissing);

        return new(DownloadMaterialAttachmentOutcome.Found, attachment, content);
    }

    private async Task<MaterialAccess> GetAccessAsync(
        Guid userId,
        Material material,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            return MaterialAccess.None;

        if (await _staffChecker.IsStaffOnOfferingAsync(userId, material.CourseOfferingId, cancellationToken))
            return MaterialAccess.Staff;

        return await _memberships.IsActiveStudentAsync(userId, material.CourseOfferingId, cancellationToken)
            ? MaterialAccess.Student
            : MaterialAccess.None;
    }

    /// <summary>
    /// Osnov po kom korisnik pristupa materijalu; osoblje ima prednost nad članstvom.
    /// </summary>
    private enum MaterialAccess
    {
        None,
        Staff,
        Student
    }
}
