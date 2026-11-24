using Application.Access;
using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Zahtev studenta za listu materijala jednog izvođenja predmeta (ST-009, US-10).
/// </summary>
public sealed record ListPublishedMaterialsForStudentQuery(
    Guid RequestingUserId,
    Guid CourseOfferingId);

/// <summary>
/// Jedna stavka liste materijala koju vidi student: identifikator, naziv i opis.
/// </summary>
public sealed record StudentMaterialListItem(Guid Id, string Name, string Description);

/// <summary>
/// Slučaj upotrebe: student vidi listu objavljenih materijala izvođenja predmeta
/// na koje je upisan (ST-009, US-10, tačka 1).
/// Pravo ima samo aktivan student izvođenja (ST-006, <see cref="IMembershipReader.IsActiveStudentAsync"/>),
/// isto pravilo kao kod detalja materijala (US-11).
/// Redosled: korisnik mora biti aktivan student izvođenja → čitaju se materijali izvođenja →
/// zadržavaju se samo objavljeni (nacrt i povučen materijal se ne vraćaju).
/// Materijali se čitaju tek posle provere prava, pa korisnik bez prava ne saznaje šta postoji.
/// Lista je uređena po nazivu da bi odgovor bio stabilan.
/// NAPOMENA: stranica sa listom (tačka 2) i prazno stanje na stranici (tačka 3) nisu deo ovog koraka.
/// </summary>
public sealed class ListPublishedMaterialsForStudentHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMembershipReader _memberships;

    public ListPublishedMaterialsForStudentHandler(
        IMaterialRepository materials,
        IMembershipReader memberships)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(memberships);

        _materials = materials;
        _memberships = memberships;
    }

    /// <summary>
    /// Vraća objavljene materijale izvođenja; prazna lista znači da objavljenih materijala nema.
    /// </summary>
    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili nije aktivan student izvođenja.
    /// </exception>
    public async Task<IReadOnlyList<StudentMaterialListItem>> HandleAsync(
        ListPublishedMaterialsForStudentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var isActiveStudent = query.RequestingUserId != Guid.Empty
            && query.CourseOfferingId != Guid.Empty
            && await _memberships.IsActiveStudentAsync(
                query.RequestingUserId,
                query.CourseOfferingId,
                cancellationToken);

        if (!isActiveStudent)
        {
            throw new MaterialAccessDeniedException(
                "Listu materijala mogu da vide samo studenti upisani na izvođenje predmeta.");
        }

        var materials = await _materials.ListByOfferingAsync(query.CourseOfferingId, cancellationToken);

        return materials
            .Where(m => m is not null
                && m.CourseOfferingId == query.CourseOfferingId
                && m.PublicationState == MaterialPublicationState.Published)
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id)
            .Select(m => new StudentMaterialListItem(m.Id, m.Name, m.Description))
            .ToArray();
    }
}
