using Application.Academic;
using Domain.Academic;

namespace Infrastructure.Academic;

public sealed class InMemoryCourseOfferingRepository : ICourseOfferingRepository
{
    private readonly List<CourseOffering> _offerings = new();
    private readonly object _lock = new();

    public Task<bool> ExistsAsync(Guid courseId, Guid academicYearId, CancellationToken cancellationToken = default)
    {
        var exists = _offerings.Any(o =>
            o.CourseId == courseId && o.AcademicYearId == academicYearId);
        return Task.FromResult(exists);
    }

    public Task<CourseOffering?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_offerings.FirstOrDefault(o => o.Id == id));
    }

    public Task AddAsync(CourseOffering offering, CancellationToken cancellationToken = default)
    {
        _offerings.Add(offering);
        return Task.CompletedTask;
    }

    public Task<bool> TryAddAsync(CourseOffering offering, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_offerings.Any(o => o.CourseId == offering.CourseId
                                 && o.AcademicYearId == offering.AcademicYearId))
            {
                return Task.FromResult(false);
            }

            _offerings.Add(offering);
            return Task.FromResult(true);
        }
    }
}
