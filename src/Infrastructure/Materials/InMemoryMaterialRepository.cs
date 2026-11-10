using System.Collections.Concurrent;
using Application.Materials;
using Domain.Materials;

namespace Infrastructure.Materials;

/// <summary>
/// Memorijski adapter za materijale; nema trajnog čuvanja podataka.
/// Služi do uvođenja baze (zahteva ADR, videti docs/architecture.md).
/// </summary>
public sealed class InMemoryMaterialRepository : IMaterialRepository
{
    private readonly ConcurrentDictionary<Guid, Material> _materials = new();

    public Task AddAsync(Material material, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(material);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_materials.TryAdd(material.Id, material))
        {
            throw new InvalidOperationException(
                $"Materijal sa identifikatorom '{material.Id}' već postoji.");
        }

        return Task.CompletedTask;
    }

    public Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _materials.TryGetValue(id, out var material);
        return Task.FromResult(material);
    }
}
