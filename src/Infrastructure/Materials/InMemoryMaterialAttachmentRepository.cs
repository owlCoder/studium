using System.Collections.Concurrent;
using Application.Materials;
using Domain.Materials;

namespace Infrastructure.Materials;

/// <summary>
/// Memorijski adapter za podatke o prilozima; nema trajnog čuvanja podataka.
/// Služi do uvođenja baze (zahteva ADR, videti docs/architecture.md).
/// </summary>
public sealed class InMemoryMaterialAttachmentRepository : IMaterialAttachmentRepository
{
    private readonly ConcurrentDictionary<Guid, MaterialAttachment> _attachments = new();

    public Task AddAsync(MaterialAttachment attachment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_attachments.TryAdd(attachment.Id, attachment))
        {
            throw new InvalidOperationException(
                $"Prilog sa identifikatorom '{attachment.Id}' već postoji.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MaterialAttachment>> ListByMaterialAsync(
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<MaterialAttachment> result = _attachments.Values
            .Where(a => a.MaterialId == materialId)
            .ToArray();

        return Task.FromResult(result);
    }
}
