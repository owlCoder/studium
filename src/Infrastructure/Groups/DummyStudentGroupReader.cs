using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Groups;

namespace Infrastructure.Groups;

public class DummyStudentGroupReader : IStudentGroupReader
{
    public Task<List<Guid>> GetActiveGroupIdsForStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var mockGrupe = new List<Guid>
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        return Task.FromResult(mockGrupe);
    }
}
