namespace Application.TeachingSessions;

public interface ICourseOfferingReader
{
    Task<bool> OfferingExistsAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default);

    Task<bool> GroupBelongsToOfferingAsync(
        Guid offeringId,
        Guid groupId,
        CancellationToken cancellationToken = default);
}
