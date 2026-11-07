namespace Infrastructure;

public class InMemoryGroupMembershipRepository : Application.Groups.IGroupMembershipRepository
{
    private readonly List<Domain.Entities.GroupMembership> _memberships = new();

    public Task AddAsync(Domain.Entities.GroupMembership membership, CancellationToken cancellationToken)
    {
        _memberships.Add(membership);
        return Task.CompletedTask;
    }

    public Task<Domain.Entities.GroupMembership?> GetActiveAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken)
    {
        var active = _memberships.FirstOrDefault(m => m.GroupId == groupId && m.StudentId == studentId && m.EndDate == null);
        return Task.FromResult(active);
    }

    public Task<IReadOnlyList<Domain.Entities.GroupMembership>> GetActiveByStudentIdAsync(Guid studentId, CancellationToken cancellationToken) //2
    {
        cancellationToken.ThrowIfCancellationRequested();

        var activeMemberships = _memberships
            .Where(m => m.StudentId == studentId && m.EndDate == null)
            .ToList();

        return Task.FromResult<IReadOnlyList<Domain.Entities.GroupMembership>>(activeMemberships);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Kod in-memory liste čuvanje se dešava u memoriji, pa je ovde prazno
        return Task.CompletedTask;
    }
}
