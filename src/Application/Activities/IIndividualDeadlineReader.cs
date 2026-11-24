using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;

namespace Application.Activities;

/// <summary>
/// US-7.9: čita pojedinačne rokove (produženja iz ST-015) jednog studenta na jednom izvođenju.
/// Dok ST-015 nije spojen u main, koristi se memorijski adapter; posle toga se ovde
/// priključuje adapter nad njihovim <c>IRokoviRepository</c>, a pravilo se ne menja.
/// </summary>
public interface IIndividualDeadlineReader
{
    Task<IReadOnlyList<IndividualDeadline>> ListIndividualDeadlinesAsync(
        Guid studentId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
