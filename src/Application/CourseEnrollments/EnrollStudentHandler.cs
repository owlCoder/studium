using Application.Interfaces;
using Domain.CourseEnrollments;

namespace Application.CourseEnrollments;

public record EnrollStudentCommand(Guid TeacherId, Guid CourseExecutionId, Guid StudentId);

public sealed class EnrollStudentHandler
{
    private readonly ICourseEnrollmentRepository _repository;
    private readonly IOfferingAccessReader _offeringAccess;

    public EnrollStudentHandler(
        ICourseEnrollmentRepository repository,
        IOfferingAccessReader offeringAccess)
    {
        _repository = repository;
        _offeringAccess = offeringAccess;
    }

    public async Task<Guid> HandleAsync(EnrollStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TeacherId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Korisnik nije autentifikovan.");
        }

       
        var isTeacherOfOffering = await _offeringAccess.IsTeacherOfOfferingAsync(command.TeacherId, command.CourseExecutionId);
        if (!isTeacherOfOffering)
        {
            throw new UnauthorizedAccessException("Nastavnik nema pravo upisa na ovo izvođenje predmeta.");
        }

        var enrollment = new CourseEnrollment(
            Guid.NewGuid(),
            command.CourseExecutionId,
            command.StudentId,
            DateTime.UtcNow);

        
        var added = await _repository.TryAddActiveEnrollmentAsync(enrollment, cancellationToken);
        if (!added)
        {
            throw new InvalidOperationException("Student već ima aktivno članstvo na ovom izvođenju predmeta.");
        }

        return enrollment.Id;
    }
}
