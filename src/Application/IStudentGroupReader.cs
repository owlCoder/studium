using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Groups;

public interface IStudentGroupReader
{
    // Vraća listu ID-jeva grupa u kojima je student TRENUTNO aktivan
    Task<List<Guid>> GetActiveGroupIdsForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
}
