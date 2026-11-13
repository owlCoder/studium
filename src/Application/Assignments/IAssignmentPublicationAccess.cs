namespace Application.Assignments;

public interface IAssignmentPublicationAccess
{
    Task<bool> CanPublishAsync(Guid offeringId, CancellationToken cancellationToken = default);
}
