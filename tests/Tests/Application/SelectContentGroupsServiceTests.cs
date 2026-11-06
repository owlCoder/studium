namespace Application.Courses;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain;

public class SelectContentGroupsService
{
    public ContentGroupContract AssignGroupsToContent(Guid contentId, List<Guid> selectedGroupIds)
    {
        if (contentId == Guid.Empty)
        {
            throw new ArgumentException("ID sadržaja ne sme biti prazan.", nameof(contentId));
        }

        if (selectedGroupIds == null || !selectedGroupIds.Any())
        {
            throw new ArgumentException("Mora biti izabrana najmanje jedna grupa za prikaz sadržaja.", nameof(selectedGroupIds));
        }

        if (selectedGroupIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("Lista grupa ne sme sadržati prazan ID.", nameof(selectedGroupIds));
        }

        return new ContentGroupContract(contentId, selectedGroupIds.Distinct().ToList());
    }
}