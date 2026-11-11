using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Kreira materijal u stanju nacrta (ST-009).
/// Redosled provera: prvo pravo pristupa (korisnik mora pripadati nastavnom
/// osoblju tog izvođenja, ST-005), zatim validacija podataka, pa upis.
/// Odbijen zahtev ne menja skladište.
/// </summary>
public sealed class CreateMaterialHandler
{
    private readonly IMaterialRepository _repository;
    private readonly IOfferingStaffChecker _staffChecker;

    public CreateMaterialHandler(IMaterialRepository repository, IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _repository = repository;
        _staffChecker = staffChecker;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja.
    /// </exception>
    /// <exception cref="ArgumentException">Neispravni podaci materijala (npr. prazan naziv).</exception>
    public async Task<Guid> HandleAsync(
        CreateMaterialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var isStaff = command.RequestingUserId != Guid.Empty
            && command.CourseOfferingId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                command.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može kreirati materijal.");
        }

        var material = new Material(
            Guid.NewGuid(),
            command.CourseOfferingId,
            command.Name,
            command.Description);

        await _repository.AddAsync(material, cancellationToken);

        return material.Id;
    }
}
