using Application.Access;
using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Zahtev studenta za detalje materijala (ST-009, US-11).
/// </summary>
public sealed record GetMaterialDetailsQuery(Guid RequestingUserId, Guid MaterialId);

/// <summary>
/// Ishod zahteva za detalje materijala (ST-009, US-11).
/// </summary>
public enum GetMaterialDetailsOutcome
{
    /// <summary>Materijal je dostupan; <see cref="GetMaterialDetailsResult.Details"/> je zadat.</summary>
    Found,

    /// <summary>
    /// Materijal ne postoji ili nije objavljen (nacrt, povučen). Student ne saznaje
    /// da neobjavljen materijal postoji.
    /// </summary>
    MaterialNotFound
}

/// <summary>
/// Spoljna veza u detaljima materijala.
/// </summary>
public sealed record MaterialDetailsLink(Guid Id, string Url);

/// <summary>
/// Prilog u detaljima materijala: izvorni naziv, tip i veličina (bez ključa skladišta).
/// </summary>
public sealed record MaterialDetailsAttachment(Guid Id, string OriginalFileName, string ContentType, long SizeInBytes);

/// <summary>
/// Detalji materijala koje vidi student: naziv, opis, spoljne veze i prilozi.
/// </summary>
public sealed record MaterialDetails(
    Guid Id,
    string Name,
    string Description,
    IReadOnlyList<MaterialDetailsLink> Links,
    IReadOnlyList<MaterialDetailsAttachment> Attachments);

/// <summary>
/// Rezultat zahteva; <see cref="Details"/> je zadat samo za <see cref="GetMaterialDetailsOutcome.Found"/>.
/// </summary>
public sealed record GetMaterialDetailsResult(GetMaterialDetailsOutcome Outcome, MaterialDetails? Details = null);

/// <summary>
/// Slučaj upotrebe: student otvara materijal i vidi opis, spoljne veze i listu priloga
/// (ST-009, US-11, stavka 1).
/// Pravilo dogovoreno sa US-12: student vidi materijal samo ako je aktivan student izvođenja
/// materijala (<see cref="IMembershipReader.IsActiveStudentAsync"/>) i ako je materijal objavljen.
/// Redosled je isti kao kod preuzimanja priloga (US-12): materijal mora postojati → korisnik mora
/// biti aktivan student izvođenja → materijal mora biti objavljen → čitaju se veze i prilozi.
/// Veze i prilozi se čitaju tek posle provere prava, pa korisnik bez prava ne saznaje šta postoji.
/// NAPOMENA: prikaz tipa i veličine (stavka 2) i preuzimanje priloga (stavka 3) nisu deo ovog koraka.
/// </summary>
public sealed class GetMaterialDetailsHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialExternalLinkRepository _links;
    private readonly IMaterialAttachmentRepository _attachments;
    private readonly IMembershipReader _memberships;

    public GetMaterialDetailsHandler(
        IMaterialRepository materials,
        IMaterialExternalLinkRepository links,
        IMaterialAttachmentRepository attachments,
        IMembershipReader memberships)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(memberships);

        _materials = materials;
        _links = links;
        _attachments = attachments;
        _memberships = memberships;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili nije aktivan student izvođenja materijala.
    /// </exception>
    public async Task<GetMaterialDetailsResult> HandleAsync(
        GetMaterialDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var material = await _materials.GetByIdAsync(query.MaterialId, cancellationToken);
        if (material is null)
            return new(GetMaterialDetailsOutcome.MaterialNotFound);

        var isActiveStudent = query.RequestingUserId != Guid.Empty
            && await _memberships.IsActiveStudentAsync(
                query.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);
        if (!isActiveStudent)
        {
            throw new MaterialAccessDeniedException(
                "Materijal mogu da otvore samo studenti upisani na izvođenje predmeta.");
        }

        if (material.PublicationState != MaterialPublicationState.Published)
            return new(GetMaterialDetailsOutcome.MaterialNotFound);

        var links = (await _links.ListByMaterialAsync(material.Id, cancellationToken))
            .Where(l => l.MaterialId == material.Id)
            .OrderBy(l => l.Url.AbsoluteUri, StringComparer.Ordinal)
            .Select(l => new MaterialDetailsLink(l.Id, l.Url.AbsoluteUri))
            .ToArray();

        var attachments = (await _attachments.ListByMaterialAsync(material.Id, cancellationToken))
            .Where(a => a.MaterialId == material.Id)
            .OrderBy(a => a.OriginalFileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Id)
            .Select(a => new MaterialDetailsAttachment(a.Id, a.OriginalFileName, a.ContentType, a.SizeInBytes))
            .ToArray();

        return new(
            GetMaterialDetailsOutcome.Found,
            new MaterialDetails(material.Id, material.Name, material.Description, links, attachments));
    }
}
