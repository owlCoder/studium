using Domain.Academic;

namespace Application.Academic;

public interface IAcademicYearRepository
{
    Task<bool> ExistsByLabelAsync(string label, CancellationToken cancellationToken = default);
    Task<AcademicYear?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AcademicYear year, CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(AcademicYear year, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicYear>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> TryUpdateAsync(AcademicYear year, CancellationToken cancellationToken = default);
}
