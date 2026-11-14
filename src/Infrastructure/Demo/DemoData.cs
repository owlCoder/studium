using Application.Courses;
using Domain.Courses;
using Infrastructure.Courses;

namespace Infrastructure.Demo;

public static class DemoData
{
    public static ICourseReader CreateCourseReader()
    {
        return new InMemoryCourseReader(new[]
        {
            new Course(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "ERS", "Elementi razvoja softvera"),
            new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "OIB", "Osnove informacione bezbednosti")
        });
    }

    public static IEnumerable<Domain.Entities.Group> CreateGroups()
    {
        return new[]
        {
            new Domain.Entities.Group
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Grupa 1",
                Type = Domain.Entities.GroupType.Predavanja,
                CourseId = Guid.Parse("11111111-1111-1111-1111-111111111111")
            },

            new Domain.Entities.Group
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Grupa 2",
                Type = Domain.Entities.GroupType.Vezbe,
                CourseId = Guid.Parse("11111111-1111-1111-1111-111111111111")
            }
        };
    }
}
