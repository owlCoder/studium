using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Zahtev za povlačenje materijala (ST-009, US-09).
/// </summary>
public sealed record WithdrawMaterialCommand(Guid RequestingUserId, Guid MaterialId);

/// <summary>
/// Ishod povlačenja materijala (ST-009, US-09).
/// </summary>
public enum WithdrawMaterialOutcome
{
    /// <summary>Materijal je prešao iz objavljen u povučen.</summary>
    Withdrawn,

    /// <summary>Materijal ne postoji.</summary>
    MaterialNotFound,

    /// <summary>Materijal nije objavljen (nacrt je ili je već povučen); stanje se ne menja.</summary>
    NotPublished
}

/// <summary>
/// Slučaj upotrebe: nastavnik povlači objavljen materijal (ST-009, US-09, tačka 1).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → materijal mora biti objavljen → stanje postaje povučen.
/// Stanje materijala se otkriva tek posle provere prava. Materijal se ne briše.
/// Povučen materijal ostaje vidljiv nastavniku i može se ponovo objaviti preko
/// <see cref="PublishMaterialHandler"/> (tačka 2).
/// NAPOMENA: dugme „Povuci“ sa potvrdom je tačka 3 US-09.
/// </summary>
public sealed class WithdrawMaterialHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IOfferingStaffChecker _staffChecker;

    public WithdrawMaterialHandler(IMaterialRepository materials, IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _staffChecker = staffChecker;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    public async Task<WithdrawMaterialOutcome> HandleAsync(
        WithdrawMaterialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return WithdrawMaterialOutcome.MaterialNotFound;

        var isStaff = command.RequestingUserId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može povući materijal.");
        }

        if (material.PublicationState != MaterialPublicationState.Published)
            return WithdrawMaterialOutcome.NotPublished;

        material.Withdraw();
        await _materials.UpdateAsync(material, cancellationToken);

        return WithdrawMaterialOutcome.Withdrawn;
    }
}
