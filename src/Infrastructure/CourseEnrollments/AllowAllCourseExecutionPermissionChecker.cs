using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.CourseEnrollments;

namespace Infrastructure.CourseEnrollments;

public sealed class AllowAllCourseExecutionPermissionChecker : ICourseExecutionPermissionChecker
{
    public Task<bool> CanTeacherManageExecutionAsync(Guid teacherId, Guid courseExecutionId, CancellationToken cancellationToken = default)
    {
        // Za svrhe izrade/testiranja dozvoljava ako je TeacherId zadat (nije Empty)
        return Task.FromResult(teacherId != Guid.Empty);
    }
}
