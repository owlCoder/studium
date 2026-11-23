
namespace Application.Assignments;

public interface IAssignmentDraftWriter
{
    Task SaveAsync(
        Guid assignmentId,
        CreateAssignmentCommand command,
        CancellationToken cancellationToken = default);
}
