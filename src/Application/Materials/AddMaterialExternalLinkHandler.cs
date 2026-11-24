using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Zahtev za dodavanje spoljne veze materijalu (ST-009, US-03).
/// </summary>
public sealed record AddMaterialExternalLinkCommand(
    Guid RequestingUserId,
    Guid MaterialId,
    string Url);

/// <summary>
/// Slučaj upotrebe: nastavnik dodaje spoljnu vezu materijalu (ST-009, US-03).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → adresa mora biti puna web adresa
/// (<see cref="MaterialExternalLink"/>) → veza se upisuje.
/// Adresa se proverava tek posle provere prava, pa korisnik bez prava dobija odbijanje
/// pristupa, a ne poruku o adresi. Odbijen zahtev ne menja skladište.
/// </summary>
public sealed class AddMaterialExternalLinkHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialExternalLinkRepository _links;
    private readonly IOfferingStaffChecker _staffChecker;

    public AddMaterialExternalLinkHandler(
        IMaterialRepository materials,
        IMaterialExternalLinkRepository links,
        IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _links = links;
        _staffChecker = staffChecker;
    }

    /// <summary>
    /// Vraća identifikator nove veze ili <c>null</c> ako materijal ne postoji.
    /// </summary>
    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    /// <exception cref="ArgumentException">Adresa nije puna web adresa.</exception>
    public async Task<Guid?> HandleAsync(
        AddMaterialExternalLinkCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return null;

        var isStaff = command.RequestingUserId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može dodavati spoljne veze materijala.");
        }

        var link = new MaterialExternalLink(Guid.NewGuid(), material.Id, command.Url);

        await _links.AddAsync(link, cancellationToken);

        return link.Id;
    }
}
