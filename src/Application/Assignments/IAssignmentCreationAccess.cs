
namespace Application.Assignments;

public interface IAssignmentCreationAccess
{
    Task<bool> CanCreateAsync(
        Guid courseOfferingId,
        CancellationToken cancellationToken = default);
}
