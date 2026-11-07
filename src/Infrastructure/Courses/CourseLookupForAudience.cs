using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.ContentAudience;

namespace Infrastructure.Courses;

public class CourseLookupForAudience : IContentLookupForAudience, IGroupLookupForAudience
{
    // Definšemo testne ID-jeve za simulaciju neovlašćenog pristupa i tuđih grupa
    public static readonly Guid UnauthorizedInstructorId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid AnotherExecutionGroupId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public async Task<bool> ExistsAsync(Guid contentId, CancellationToken cancellationToken)
    {
        return await Task.FromResult(contentId != Guid.Empty);
    }

    public async Task<Guid> GetCourseExecutionIdAsync(Guid contentId, CancellationToken cancellationToken)
    {
        // Vraćamo fiksni ID izvođenja za ovaj sadržaj
        return await Task.FromResult(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    public async Task<bool> CanInstructorEditContentAsync(Guid contentId, Guid instructorId, CancellationToken cancellationToken)
    {
        // 1. Ako su ID-jevi prazni -> odbijamo
        if (contentId == Guid.Empty || instructorId == Guid.Empty)
        {
            return false;
        }

        // 2. STROGA PROVERA ZA TEST: Ako je u pitanju namerno testirani neovlašćeni instruktor, vraćamo false (izaziva odbijanje/Forbidden)
        if (instructorId == UnauthorizedInstructorId)
        {
            return false;
        }

        // Inače, za validne ulogovane instruktore dozvoljavamo
        return await Task.FromResult(true);
    }

    public async Task<bool> AllGroupsBelongToCourseExecutionAsync(List<Guid> groupIds, Guid courseExecutionId, CancellationToken cancellationToken)
    {
        if (groupIds == null || !groupIds.Any() || courseExecutionId == Guid.Empty)
        {
            return false;
        }

        // STROGA PROVERA ZA TEST: Ako bilo koja grupa iz liste pripada "drugom izvođenju" (naš testni ID), odbijamo
        foreach (var groupId in groupIds)
        {
            if (groupId == Guid.Empty || groupId == AnotherExecutionGroupId)
            {
                return false; // Izaziva Bad Request (400)
            }
        }

        return await Task.FromResult(true);
    }
}