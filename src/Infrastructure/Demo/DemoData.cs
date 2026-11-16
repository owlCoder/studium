using Application.Courses;
using Domain.Courses;
using Infrastructure.Courses;

namespace Infrastructure.Demo;

public static class DemoData
{
    public static readonly Guid ErsCourseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid LecturesGroupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ExercisesGroupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid ActiveStudentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid InactiveStudentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

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
                Id = LecturesGroupId,
                Name = "Grupa 1",
                Type = Domain.Entities.GroupType.Predavanja,
                CourseId = ErsCourseId
            },

            new Domain.Entities.Group
            {
                Id = ExercisesGroupId,
                Name = "Grupa 2",
                Type = Domain.Entities.GroupType.Vezbe,
                CourseId = ErsCourseId
            }
        };
    }

    public static IReadOnlyDictionary<Guid, bool> CreateStudentStatuses()
    {
        return new Dictionary<Guid, bool>
        {
            [ActiveStudentId] = true,
            [InactiveStudentId] = false
        };
    }
}
