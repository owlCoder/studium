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

    /// <summary>
    /// Vraća prilog ili <c>null</c> ako ne postoji (ST-009, US-07).
    /// </summary>
    Task<MaterialAttachment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uklanja zapis priloga (ST-009, US-07). Vraća <c>true</c> ako je zapis uklonjen,
    /// a <c>false</c> ako ga nije bilo (npr. već ga je uklonio drugi zahtev).
    /// Sadržaj datoteke u <see cref="IAttachmentFileStorage"/> se ovde ne briše.
    /// </summary>
    Task<bool> RemoveAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
