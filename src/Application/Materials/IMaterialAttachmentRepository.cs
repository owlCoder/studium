using Domain.Materials;

namespace Application.Materials;

/// <summary>
/// Port za čuvanje podataka o prilozima materijala (ST-009, US-04):
/// izvorni naziv, tip, veličina i ključ sadržaja u skladištu.
/// Sam sadržaj datoteke čuva <see cref="IAttachmentFileStorage"/>.
/// </summary>
public interface IMaterialAttachmentRepository
{
    Task AddAsync(
        MaterialAttachment attachment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialAttachment>> ListByMaterialAsync(
        Guid materialId,
        CancellationToken cancellationToken = default);
}
