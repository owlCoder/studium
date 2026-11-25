using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Zahtev nastavnika za listu materijala jednog izvođenja predmeta (ST-009, US-08, tačka 3).
/// </summary>
public sealed record ListMaterialsForStaffQuery(
    Guid RequestingUserId,
    Guid CourseOfferingId);

/// <summary>
/// Jedna stavka liste materijala koju vidi nastavnik: identifikator, naziv i stanje objave.
/// </summary>
public sealed record StaffMaterialListItem(
    Guid Id,
    string Name,
    MaterialPublicationState PublicationState);

/// <summary>
/// Slučaj upotrebe: nastavnik vidi sve materijale svog izvođenja sa stanjem objave
/// (ST-009, US-08, tačka 3), da bi mogao da objavi nacrt ili ponovo objavi povučen materijal (US-09).
/// Pravo ima samo nastavno osoblje izvođenja (ST-005, <see cref="IOfferingStaffChecker"/>).
/// Redosled: korisnik mora pripadati nastavnom osoblju izvođenja → čitaju se materijali izvođenja.
/// Materijali se čitaju tek posle provere prava, pa korisnik bez prava ne saznaje ni da nacrti postoje.
/// Vraćaju se materijali u svim stanjima (nacrt, objavljen, povučen), uređeni po nazivu.
/// </summary>
public sealed class ListMaterialsForStaffHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IOfferingStaffChecker _staffChecker;

    public ListMaterialsForStaffHandler(
        IMaterialRepository materials,
        IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _staffChecker = staffChecker;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja.
    /// </exception>
    public async Task<IReadOnlyList<StaffMaterialListItem>> HandleAsync(
        ListMaterialsForStaffQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var isStaff = query.RequestingUserId != Guid.Empty
            && query.CourseOfferingId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                query.RequestingUserId,
                query.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Listu materijala sa stanjem objave vidi samo nastavno osoblje izvođenja.");
        }

        var materials = await _materials.ListByOfferingAsync(query.CourseOfferingId, cancellationToken);

        return materials
            .Where(m => m.CourseOfferingId == query.CourseOfferingId)
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id)
            .Select(m => new StaffMaterialListItem(m.Id, m.Name, m.PublicationState))
            .ToArray();
    }
}
