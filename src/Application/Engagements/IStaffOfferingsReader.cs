using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Engagements;

/// <summary>
/// Port za upit nad angažovanjima. Implementacija vraća samo AKTIVNA angažovanja
/// zadatog korisnika, sa nazivom predmeta, akademskom godinom i ulogom.
/// </summary>
public interface IStaffOfferingsReader
{
    Task<IReadOnlyList<TeachingOfferingDto>> ListActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
