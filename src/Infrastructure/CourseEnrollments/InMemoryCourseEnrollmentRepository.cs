using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.CourseEnrollments;
using Domain.CourseEnrollments;

namespace Infrastructure.CourseEnrollments;

public sealed class InMemoryCourseEnrollmentRepository : ICourseEnrollmentRepository
{
    private readonly List<CourseEnrollment> _enrollments = new();
    private readonly object _lock = new();

    public Task<bool> TryAddActiveEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var exists = _enrollments.Any(e =>
                e.CourseExecutionId == enrollment.CourseExecutionId &&
                e.StudentId == enrollment.StudentId &&
                e.Status == EnrollmentStatus.Active);

            if (exists)
            {
                return Task.FromResult(false);
            }

            _enrollments.Add(enrollment);
            return Task.FromResult(true);
        }
    }

    public Task<CourseEnrollment?> GetActiveEnrollmentAsync(Guid courseExecutionId, Guid studentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var result = _enrollments.FirstOrDefault(e =>
                e.CourseExecutionId == courseExecutionId &&
                e.StudentId == studentId &&
                e.Status == EnrollmentStatus.Active);

            return Task.FromResult(result);
        }
    }
}
