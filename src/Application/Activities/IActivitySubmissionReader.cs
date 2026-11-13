using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;

namespace Application.Activities;

/// <summary>Čita predaje jednog studenta na jednom izvođenju iz izvorne evidencije (US-7.3).</summary>
public interface IActivitySubmissionReader
{
    Task<IReadOnlyList<ActivitySubmission>> ListForStudentAsync(
        Guid studentId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
