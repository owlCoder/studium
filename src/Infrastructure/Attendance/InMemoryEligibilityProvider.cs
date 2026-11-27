using Application.Attendance;
using Domain.Attendance;

namespace Infrastructure.Attendance;

/// <summary>
/// Memorijska zamena porta članstva (ST-006, ST-019) dok drugi timovi ne
/// obezbede pravu implementaciju. Podobnost računa po pravilu iz domena
/// (<see cref="EligibilityRule"/>), uz period važenja članstva.
/// </summary>
public sealed class InMemoryEligibilityProvider : IEligibilityProvider
{
    private readonly List<GroupMembership> _memberships = new();

    /// <summary>
    /// Dodaje članstvo koje važi uvek. Zadržano radi postojećih testova i demo scenarija.
    /// </summary>
    public InMemoryEligibilityProvider Add(Guid offeringId, Guid? groupId, Guid studentId) =>
        Add(offeringId, groupId, studentId, MembershipPeriod.Always);

    /// <summary>
    /// Dodaje članstvo koje važi samo u zadatom periodu.
    /// </summary>
    public InMemoryEligibilityProvider Add(
        Guid offeringId,
        Guid? groupId,
        Guid studentId,
        MembershipPeriod period)
    {
        _memberships.Add(new GroupMembership(studentId, offeringId, groupId, period));
        return this;
    }

    public Task<IReadOnlyList<Guid>> ListEligibleStudentIdsAsync(
        Guid offeringId,
        Guid? groupId,
        DateTime at,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = EligibilityRule.EligibleStudents(_memberships, offeringId, groupId, at);
        return Task.FromResult(ids);
    }
}
