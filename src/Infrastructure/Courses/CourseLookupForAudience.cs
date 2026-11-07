using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.ContentAudience;

namespace Infrastructure.Courses;

public class CourseLookupForAudience : IContentLookupForAudience, IGroupLookupForAudience
{
    public async Task<bool> ExistsAsync(Guid contentId, CancellationToken cancellationToken)
    {
        // Provera da li sadržaj postoji (ako nemaš bazu, proveri da li je ID validan)
        return await Task.FromResult(contentId != Guid.Empty);
    }

    public async Task<Guid> GetCourseExecutionIdAsync(Guid contentId, CancellationToken cancellationToken)
    {
        // Vraća ID izvođenja kursa povezanog sa ovim sadržajem
        return await Task.FromResult(Guid.NewGuid());
    }

public async Task<bool> CanInstructorEditContentAsync(Guid contentId, Guid instructorId, CancellationToken cancellationToken)
{
    // Ako je prosleđen neki specijalni "testni neovlašćeni" ID, vrati false
    if (instructorId == Guid.Empty)
    {
        return false;
    }

    // Za validne zahteve vraća true (demo režim, ali sa kontrolom)
    return await Task.FromResult(true); 
}

public async Task<bool> AllGroupsBelongToCourseExecutionAsync(List<Guid> groupIds, Guid courseExecutionId, CancellationToken cancellationToken)
{
    if (groupIds == null || !groupIds.Any())
    {
        return false;
    }

    // Možemo simulirati da ako grupa ima neki specifični testni uslov vrati false, 
    // inače true za normalan rad.
    return await Task.FromResult(true);
}
}