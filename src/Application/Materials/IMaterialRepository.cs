using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Port za čuvanje nastavnih materijala (ST-009).
/// Trajna implementacija (baza) dodaje se tek nakon ADR odluke o skladištu.
/// </summary>
public interface IMaterialRepository
{
    Task AddAsync(
        Material material,
        CancellationToken cancellationToken = default);

    Task<Material?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Čuva izmene postojećeg materijala (ST-009, US-02).
    /// Baca <see cref="InvalidOperationException"/> ako materijal ne postoji.
    /// </summary>
    Task UpdateAsync(
        Material material,
        CancellationToken cancellationToken = default);
}
