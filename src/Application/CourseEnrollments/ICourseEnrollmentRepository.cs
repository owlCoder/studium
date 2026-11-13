using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Domain.CourseEnrollments;

namespace Application.CourseEnrollments;

public interface ICourseEnrollmentRepository
{
    Task<bool> TryAddActiveEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken = default);
    Task<CourseEnrollment?> GetActiveEnrollmentAsync(Guid courseExecutionId, Guid studentId, CancellationToken cancellationToken = default);
}
