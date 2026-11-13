using Application.Courses;
using Application.Interfaces;
using Domain.Courses;
using Domain.Entities;
using Infrastructure.Courses;
using Infrastructure.Repositories;

namespace Infrastructure.Demo;

public static class DemoData
{
    public static readonly Guid ErsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OibId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // Demo nastavnik zadužen samo za ERS.
    public static readonly Guid ErsTeacherId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static ICourseReader CreateCourseReader()
    {
        return new InMemoryCourseReader(new[]
        {
            new Course(ErsId, "ERS", "Elementi razvoja softvera"),
            new Course(OibId, "OIB", "Osnove informacione bezbednosti")
        });
    }

    public static IOfferingAccessReader CreateOfferingAccessReader()
    {
        return new InMemoryOfferingAccessReader(new[]
        {
            (ErsTeacherId, ErsId)
        });
    }

    public static IEnumerable<Enrollment> CreateEnrollments()
    {
        var prekinut = new Enrollment
        {
            OfferingId = ErsId,
            StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003")
        };
        prekinut.Terminate();

        return new List<Enrollment>
        {
            new Enrollment
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
                OfferingId = ErsId,
                StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")
            },
            new Enrollment
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002"),
                OfferingId = ErsId,
                StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002")
            },
            new Enrollment
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000003"),
                OfferingId = OibId,
                StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")
            },
            prekinut
        };
    }
}
