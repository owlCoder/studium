using System.Collections.Concurrent;
using Application.Materials;
using Domain.Materials;

namespace Infrastructure.Materials;

/// <summary>
/// Memorijski adapter za spoljne veze materijala (ST-009, US-03); nema trajnog čuvanja podataka.
/// Služi do uvođenja baze (zahteva ADR, videti docs/architecture.md i ADR-0003).
/// </summary>
public sealed class InMemoryMaterialExternalLinkRepository : IMaterialExternalLinkRepository
{
    private readonly ConcurrentDictionary<Guid, MaterialExternalLink> _links = new();

    public Task AddAsync(MaterialExternalLink link, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(link);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_links.TryAdd(link.Id, link))
        {
            throw new InvalidOperationException(
                $"Veza sa identifikatorom '{link.Id}' već postoji.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MaterialExternalLink>> ListByMaterialAsync(
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<MaterialExternalLink> result = _links.Values
            .Where(l => l.MaterialId == materialId)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<MaterialExternalLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _links.TryGetValue(id, out var link);
        return Task.FromResult(link);
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_links.TryRemove(id, out _));
    }
}
