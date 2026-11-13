using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.CourseEnrollments;

namespace Infrastructure.CourseEnrollments;

public sealed class DenyAllCourseExecutionPermissionChecker : ICourseExecutionPermissionChecker
{
    public Task<bool> CanTeacherManageExecutionAsync(
        Guid teacherId,
        Guid courseExecutionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
