namespace Application.Assignments;

public interface IAssignmentPublicationAccess
{
    Task<bool> CanPublishAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
}
