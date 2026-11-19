using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Zahtev za objavu materijala (ST-009, US-08).
/// </summary>
public sealed record PublishMaterialCommand(Guid RequestingUserId, Guid MaterialId);

/// <summary>
/// Ishod objave materijala (ST-009, US-08).
/// </summary>
public enum PublishMaterialOutcome
{
    /// <summary>Materijal je prešao iz nacrta u objavljen.</summary>
    Published,

    /// <summary>Materijal ne postoji.</summary>
    MaterialNotFound,

    /// <summary>Materijal nije nacrt (već je objavljen ili povučen); stanje se ne menja.</summary>
    NotDraft,

    /// <summary>
    /// Bar jedan prilog nema sačuvanu datoteku u skladištu (tačka 2);
    /// materijal ostaje nacrt.
    /// </summary>
    MissingAttachmentFiles
}

/// <summary>
/// Rezultat objave materijala (ST-009, US-08).
/// </summary>
/// <param name="Outcome">Ishod objave.</param>
/// <param name="MissingFileNames">
/// Izvorni nazivi priloga čija datoteka nije pronađena u skladištu;
/// prazno za svaki ishod osim <see cref="PublishMaterialOutcome.MissingAttachmentFiles"/>.
/// </param>
public sealed record PublishMaterialResult(
    PublishMaterialOutcome Outcome,
    IReadOnlyList<string> MissingFileNames)
{
    public static PublishMaterialResult Of(PublishMaterialOutcome outcome) => new(outcome, []);
}

/// <summary>
/// Slučaj upotrebe: nastavnik objavljuje materijal (ST-009, US-08).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → materijal mora biti nacrt → svaki prilog mora
/// imati sačuvanu datoteku u skladištu (tačka 2) → stanje postaje objavljen.
/// Stanje materijala i prilozi se otkrivaju tek posle provere prava.
/// Ako bilo kom prilogu nedostaje datoteka, objava se odbija i materijal ostaje nacrt,
/// jer neispravan prenos ne sme ostaviti objavljen materijal sa nedostajućom datotekom
/// (poslovno pravilo ST-009, ADR-0001).
/// </summary>
public sealed class PublishMaterialHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IAttachmentFileStorage _fileStorage;
    private readonly IOfferingStaffChecker _staffChecker;

    public PublishMaterialHandler(
        IMaterialRepository materials,
        IMaterialAttachmentRepository attachments,
        IAttachmentFileStorage fileStorage,
        IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(fileStorage);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _attachments = attachments;
        _fileStorage = fileStorage;
        _staffChecker = staffChecker;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    public async Task<PublishMaterialResult> HandleAsync(
        PublishMaterialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return PublishMaterialResult.Of(PublishMaterialOutcome.MaterialNotFound);

        var isStaff = command.RequestingUserId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može objaviti materijal.");
        }

        if (material.PublicationState != MaterialPublicationState.Draft)
            return PublishMaterialResult.Of(PublishMaterialOutcome.NotDraft);

        var missing = await FindAttachmentsWithoutFileAsync(material.Id, cancellationToken);
        if (missing.Count > 0)
            return new PublishMaterialResult(PublishMaterialOutcome.MissingAttachmentFiles, missing);

        material.Publish();
        await _materials.UpdateAsync(material, cancellationToken);

        return PublishMaterialResult.Of(PublishMaterialOutcome.Published);
    }

    private async Task<IReadOnlyList<string>> FindAttachmentsWithoutFileAsync(
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var attachments = await _attachments.ListByMaterialAsync(materialId, cancellationToken);
        var missing = new List<string>();

        foreach (var attachment in attachments)
        {
            // Postojanje se proverava otvaranjem za čitanje; sadržaj se odmah zatvara.
            await using var content = await _fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
            if (content is null)
                missing.Add(attachment.OriginalFileName);
        }

        return missing;
    }
}
