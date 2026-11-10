using Application.Access;

namespace Infrastructure.Access;

/// <summary>
/// Memorijski adapter za članstva i angažovanja (ST-018, US-6); nema trajnog čuvanja podataka.
/// </summary>
public sealed class InMemoryMembershipReader : IMembershipReader
{
    private readonly IReadOnlyList<OfferingMembership> _memberships;

    public InMemoryMembershipReader(IEnumerable<OfferingMembership> memberships)
    {
        ArgumentNullException.ThrowIfNull(memberships);
        _memberships = memberships.ToArray();
    }

    public Task<bool> IsActiveStudentAsync(
        Guid userId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _memberships.Any(m =>
            m.UserId == userId &&
            m.OfferingId == offeringId &&
            m.Kind == MembershipKind.Student &&
            m.IsActive);

        return Task.FromResult(result);
    }

    public Task<bool> IsEngagedStaffAsync(
        Guid userId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _memberships.Any(m =>
            m.UserId == userId &&
            m.OfferingId == offeringId &&
            (m.Kind is MembershipKind.Teacher or MembershipKind.Assistant) &&
            m.IsActive);

        return Task.FromResult(result);
    }
}
