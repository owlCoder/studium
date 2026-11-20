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

    public Task UpdateAsync(CourseOffering offering, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var index = _offerings.FindIndex(o => o.Id == offering.Id);
            if (index < 0)
            {
                throw new InvalidOperationException("Izvođenje ne postoji.");
            }

            _offerings[index] = offering;
            return Task.CompletedTask;
        }
    }
}
