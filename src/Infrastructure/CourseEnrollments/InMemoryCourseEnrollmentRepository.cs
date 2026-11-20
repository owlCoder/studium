using Application.CourseEnrollments;
using Application.DTOs;
using Application.Interfaces;
using Domain.CourseEnrollments;
using Domain.Entities;

namespace Infrastructure.CourseEnrollments;

public sealed class InMemoryCourseEnrollmentRepository : ICourseEnrollmentRepository, IEnrollmentRepository
{
    private readonly List<Enrollment> _enrollments;
    private readonly object _lock = new();

    public InMemoryCourseEnrollmentRepository(IEnumerable<Enrollment>? enrollments = null)
    {
        _enrollments = enrollments?.ToList() ?? new List<Enrollment>();
    }

   
    public Task<bool> TryAddActiveEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var alreadyActive = _enrollments.Any(e =>
                e.OfferingId == enrollment.CourseExecutionId &&
                e.StudentId == enrollment.StudentId &&
                e.IsActive);

            if (alreadyActive)
            {
                return Task.FromResult(false);
            }

            _enrollments.Add(new Enrollment
            {
                Id = enrollment.Id,
                OfferingId = enrollment.CourseExecutionId,
                StudentId = enrollment.StudentId
            });

            return Task.FromResult(true);
        }
    }

  
    public Task<List<StudentEnrollmentDto>> GetEnrollmentsByOfferingAsync(Guid offeringId)
    {
        lock (_lock)
        {
            var enrollments = _enrollments
                .Where(e => e.OfferingId == offeringId && e.IsActive)
                .Select(e => new StudentEnrollmentDto(e.Id, e.StudentId, e.IsActive))
                .ToList();

            return Task.FromResult(enrollments);
        }
    }

    public Task<Enrollment?> GetByIdAsync(Guid enrollmentId)
    {
        lock (_lock)
        {
            return Task.FromResult(_enrollments.FirstOrDefault(e => e.Id == enrollmentId));
        }
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}
