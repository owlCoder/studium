using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.CourseEnrollments;

public interface ICourseExecutionPermissionChecker
{
    Task<bool> CanTeacherManageExecutionAsync(Guid teacherId, Guid courseExecutionId, CancellationToken cancellationToken = default);
}
