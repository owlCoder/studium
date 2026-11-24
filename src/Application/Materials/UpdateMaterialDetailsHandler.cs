namespace Application.Materials;

/// <summary>
/// Slučaj upotrebe: izmena naziva i opisa postojećeg materijala (ST-009, US-02).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → naziv i opis se proveravaju i menjaju.
/// Vraća <c>false</c> ako materijal ne postoji; nevažeći naziv izaziva
/// <see cref="ArgumentException"/> bez promene sačuvanog materijala.
/// Podaci se proveravaju tek posle provere prava, pa korisnik bez prava dobija
/// odbijanje pristupa, a ne poruku o nazivu. Odbijen zahtev ne menja materijal.
/// Uspešna izmena beleži vreme poslednje izmene iz zadatog izvora vremena.
/// </summary>
public sealed class UpdateMaterialDetailsHandler
{
    private readonly IMaterialRepository _repository;
    private readonly IOfferingStaffChecker _staffChecker;
    private readonly TimeProvider _timeProvider;

    public UpdateMaterialDetailsHandler(
        IMaterialRepository repository,
        IOfferingStaffChecker staffChecker,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(staffChecker);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repository = repository;
        _staffChecker = staffChecker;
        _timeProvider = timeProvider;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    public async Task<bool> HandleAsync(
        UpdateMaterialDetailsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _repository.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return false;

        var isStaff = command.RequestingUserId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može menjati materijal.");
        }

        material.UpdateDetails(command.Name, command.Description, _timeProvider.GetUtcNow());
        await _repository.UpdateAsync(material, cancellationToken);

        return true;
    }
}
