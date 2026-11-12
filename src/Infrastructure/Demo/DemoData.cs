using Application.Courses;
using Domain.Courses;
using Domain.Entities;
using Infrastructure.Courses;

namespace Infrastructure.Demo;

public static class DemoData
{
    public static readonly Guid ErsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OibId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static ICourseReader CreateCourseReader()
    {
        return new InMemoryCourseReader(new[]
        {
            new Course(ErsId, "ERS", "Elementi razvoja softvera"),
            new Course(OibId, "OIB", "Osnove informacione bezbednosti")
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
