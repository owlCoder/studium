
namespace Domain.Courses;

using System;

public interface ICourseOfferingLookup
{
    bool HasActiveOfferings(Guid courseId);
}
