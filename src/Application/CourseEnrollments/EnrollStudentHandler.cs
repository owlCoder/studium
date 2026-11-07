using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.CourseEnrollments;

namespace Application.CourseEnrollments;

public record EnrollStudentCommand(Guid CourseExecutionId, Guid StudentId);

public sealed class EnrollStudentHandler
{
    private readonly ICourseEnrollmentRepository _repository;

    public EnrollStudentHandler(ICourseEnrollmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> HandleAsync(EnrollStudentCommand command, CancellationToken cancellationToken = default)
    {
        // Provera da li student već ima aktivno članstvo na istom izvođenju
        var existing = await _repository.GetActiveEnrollmentAsync(command.CourseExecutionId, command.StudentId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("Student već ima aktivno članstvo na ovom izvođenju predmeta.");
        }

        var enrollment = new CourseEnrollment(
            Guid.NewGuid(),
            command.CourseExecutionId,
            command.StudentId,
            DateTime.UtcNow
        );

        await _repository.SaveAsync(enrollment, cancellationToken);
        return enrollment.Id;
    }
}
