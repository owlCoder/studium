using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.CourseEnrollments;

namespace Application.CourseEnrollments;

public record EnrollStudentCommand(Guid TeacherId, Guid CourseExecutionId, Guid StudentId);

public sealed class EnrollStudentHandler
{
    private readonly ICourseEnrollmentRepository _repository;
    private readonly ICourseExecutionPermissionChecker _permissionChecker;

    public EnrollStudentHandler(
        ICourseEnrollmentRepository repository,
        ICourseExecutionPermissionChecker permissionChecker)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
    }

    public async Task<Guid> HandleAsync(EnrollStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TeacherId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Korisnik nije autentifikovan.");
        }

        var hasPermission = await _permissionChecker.CanTeacherManageExecutionAsync(command.TeacherId, command.CourseExecutionId, cancellationToken);
        if (!hasPermission)
        {
            throw new UnauthorizedAccessException("Nastavnik nema pravo upisa na ovo izvođenje predmeta.");
        }

        var enrollment = new CourseEnrollment(
            Guid.NewGuid(),
            command.CourseExecutionId,
            command.StudentId,
            DateTime.UtcNow
        );

        // Atomski check-and-insert
        var added = await _repository.TryAddActiveEnrollmentAsync(enrollment, cancellationToken);
        if (!added)
        {
            throw new InvalidOperationException("Student već ima aktivno članstvo na ovom izvođenju predmeta.");
        }

        return enrollment.Id;
    }
}
