using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Infrastructure;

public class InMemoryGroupMembershipRepository : Application.Groups.IGroupMembershipRepository
{
    private readonly List<Domain.Entities.GroupMembership> _memberships = new();
    private readonly object _lock = new();

    public Task AddAsync(Domain.Entities.GroupMembership membership, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var existingActive = _memberships.FirstOrDefault(m => 
                m.GroupId == membership.GroupId && 
                m.StudentId == membership.StudentId && 
                m.EndDate == null);

            if (existingActive != null)
            {
                throw new InvalidOperationException("Student je vec u ovoj grupi.");
            }

            _memberships.Add(membership);
        }
        return Task.CompletedTask;
    }

    public Task<Domain.Entities.GroupMembership?> GetActiveAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var active = _memberships.FirstOrDefault(m => m.GroupId == groupId && m.StudentId == studentId && m.EndDate == null);
            return Task.FromResult(active);
        }
    }

    public Task<IReadOnlyList<Domain.Entities.GroupMembership>> GetActiveByStudentIdAsync(
    Guid studentId,
    CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var activeMemberships = _memberships
                .Where(m => m.StudentId == studentId && m.EndDate == null)
                .ToList();

            return Task.FromResult<IReadOnlyList<Domain.Entities.GroupMembership>>(
                activeMemberships);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Kod in-memory liste čuvanje se dešava u memoriji, pa je ovde prazno
        return Task.CompletedTask;
    }
}
