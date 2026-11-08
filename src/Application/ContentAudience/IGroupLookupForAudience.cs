namespace Application.ContentAudience;

public interface IGroupLookupForAudience
{
    Task<bool> AllGroupsBelongToCourseExecutionAsync(List<Guid> groupIds, Guid courseExecutionId, CancellationToken cancellationToken);
}