using Domain.CourseEnrollments;

namespace Application.CourseEnrollments;

public interface ICourseEnrollmentRepository
{
    
    Task<bool> TryAddActiveEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken = default);
}
