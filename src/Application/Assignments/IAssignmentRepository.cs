using Domain.Assignments;

namespace Application.Assignments;

public interface IAssignmentRepository
{
    Task<Assignment?> FindAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    Task SaveAsync(Assignment assignment, CancellationToken cancellationToken = default);
}
