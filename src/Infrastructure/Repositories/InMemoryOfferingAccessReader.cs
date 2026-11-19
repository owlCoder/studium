using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Repositories;

public class InMemoryOfferingAccessReader : IOfferingAccessReader
{
    private readonly HashSet<(Guid TeacherId, Guid OfferingId)> _assignments;

    public InMemoryOfferingAccessReader(IEnumerable<(Guid TeacherId, Guid OfferingId)>? assignments = null)
    {
        _assignments = assignments?.ToHashSet() ?? new HashSet<(Guid, Guid)>();
    }

    public Task<bool> IsTeacherOfOfferingAsync(Guid teacherId, Guid offeringId)
    {
        return Task.FromResult(_assignments.Contains((teacherId, offeringId)));
    }
}
