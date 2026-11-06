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

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        lock (_lock){}
        // Kod in-memory liste čuvanje se dešava u memoriji, pa je ovde prazno
        return Task.CompletedTask;
    }
}