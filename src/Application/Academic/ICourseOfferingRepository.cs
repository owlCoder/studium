using Domain.Academic;

namespace Application.Academic;

public interface ICourseOfferingRepository
{
    Task<bool> ExistsAsync(Guid courseId, Guid academicYearId, CancellationToken cancellationToken = default);
    Task<CourseOffering?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CourseOffering offering, CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(CourseOffering offering, CancellationToken cancellationToken = default);
}
