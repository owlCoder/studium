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
    NotDraft
}

/// <summary>
/// Slučaj upotrebe: nastavnik objavljuje materijal (ST-009, US-08, tačka 1).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → materijal mora biti nacrt → stanje postaje objavljen.
/// Stanje materijala se otkriva tek posle provere prava.
/// NAPOMENA: odbijanje objave kada prilogu nedostaje datoteka je tačka 2 US-08.
/// </summary>
public sealed class PublishMaterialHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IOfferingStaffChecker _staffChecker;

    public PublishMaterialHandler(IMaterialRepository materials, IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _staffChecker = staffChecker;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    public async Task<PublishMaterialOutcome> HandleAsync(
        PublishMaterialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return PublishMaterialOutcome.MaterialNotFound;

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
            return PublishMaterialOutcome.NotDraft;

        material.Publish();
        await _materials.UpdateAsync(material, cancellationToken);

        return PublishMaterialOutcome.Published;
    }
}
