using System;
using System.Collections.Generic;
using Domain.Courses;

namespace Tests.Domain;

internal class FakeCourseOfferingLookup : ICourseOfferingLookup
{
    private readonly HashSet<Guid> _coursesWithActiveOfferings = new();

    public void AddActiveOffering(Guid courseId)
    {
        _coursesWithActiveOfferings.Add(courseId);
    }

    public bool HasActiveOfferings(Guid courseId)
    {
        return _coursesWithActiveOfferings.Contains(courseId);
    }
}
