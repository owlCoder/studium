using Application.Attendance;

namespace Infrastructure.Attendance;

/// <summary>
/// Memorijska zamena porta članstva (ST-006); vremenski uslov se ignoriše.
/// </summary>
public sealed class InMemoryEligibilityProvider : IEligibilityProvider
{
    private readonly List<(Guid OfferingId, Guid? GroupId, Guid StudentId)> _entries = new();

    public InMemoryEligibilityProvider Add(Guid offeringId, Guid? groupId, Guid studentId)
    {
        _entries.Add((offeringId, groupId, studentId));
        return this;
    }

    public Task<IReadOnlyList<Guid>> ListEligibleStudentIdsAsync(
        Guid offeringId,
        Guid? groupId,
        DateTime at,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = _entries
            .Where(e => e.OfferingId == offeringId && e.GroupId == groupId)
            .Select(e => e.StudentId)
            .ToArray();
        return Task.FromResult<IReadOnlyList<Guid>>(ids);
    }
}
