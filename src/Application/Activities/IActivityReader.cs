using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;

namespace Application.Activities;

/// <summary>Čita aktivnosti jednog izvođenja iz izvorne evidencije (US-7.3).</summary>
public interface IActivityReader
{
    Task<IReadOnlyList<Activity>> ListForOfferingAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
