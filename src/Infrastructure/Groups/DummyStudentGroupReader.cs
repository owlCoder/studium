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
        // Vraćamo praznu listu za potrebe pokretanja
        return Task.FromResult(new List<Guid>());
    }
}
