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
        var authorizedStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        if (studentId == authorizedStudentId)
        {
            return Task.FromResult(new List<Guid> { Guid.Parse("11111111-1111-1111-1111-111111111111") });
        }

        return Task.FromResult(new List<Guid>());
    }
}
