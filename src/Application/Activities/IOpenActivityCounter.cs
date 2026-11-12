namespace Application.Activities;


public interface IOpenActivityCounter
{
    Task<int> CountOpenAsync(
        Guid studentId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
