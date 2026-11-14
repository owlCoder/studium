using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure;

public class InMemoryGroupRepository : Application.IGroupRepository //10
{
    private readonly List<Domain.Entities.Group> _groups = new();

    public InMemoryGroupRepository(IEnumerable<Domain.Entities.Group> groups)
    {
        _groups = groups.ToList();
    }

    public Task<Domain.Entities.Group?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var group = _groups.FirstOrDefault(g => g.Id == id);

        return Task.FromResult(group);
    }

    // NOVA METODA
    public Task AddAsync(
        Domain.Entities.Group group,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _groups.Add(group);

        return Task.CompletedTask;
    }
}
