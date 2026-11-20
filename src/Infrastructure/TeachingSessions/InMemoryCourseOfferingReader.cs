using Application.TeachingSessions;

namespace Infrastructure.TeachingSessions;

public sealed class InMemoryCourseOfferingReader : ICourseOfferingReader
{
    private readonly HashSet<Guid> _offerings = new();
    private readonly HashSet<(Guid OfferingId, Guid GroupId)> _groups = new();

    public InMemoryCourseOfferingReader AddOffering(Guid offeringId)
    {
        _offerings.Add(offeringId);
        return this;
    }

    public InMemoryCourseOfferingReader AddGroup(Guid offeringId, Guid groupId)
    {
        _offerings.Add(offeringId);
        _groups.Add((offeringId, groupId));
        return this;
    }

    public Task<bool> OfferingExistsAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_offerings.Contains(offeringId));
    }

    public Task<bool> GroupBelongsToOfferingAsync(
        Guid offeringId,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_groups.Contains((offeringId, groupId)));
    }
}
