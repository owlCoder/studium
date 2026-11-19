using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Port za čuvanje spoljnih veza materijala (ST-009, US-03).
/// Obuhvata i operacije potrebne za uklanjanje veza (US-07), po istom obrascu
/// kao <see cref="IMaterialAttachmentRepository"/>.
/// Trajna implementacija (baza) dodaje se po ADR-0003, tabela <c>material_external_links</c>.
/// </summary>
public interface IMaterialExternalLinkRepository
{
    Task AddAsync(
        MaterialExternalLink link,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialExternalLink>> ListByMaterialAsync(
        Guid materialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vraća vezu ili <c>null</c> ako ne postoji.
    /// </summary>
    Task<MaterialExternalLink?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uklanja vezu. Vraća <c>true</c> ako je veza uklonjena,
    /// a <c>false</c> ako je nije bilo (npr. već ju je uklonio drugi zahtev).
    /// </summary>
    Task<bool> RemoveAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
