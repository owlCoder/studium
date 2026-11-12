using Application.Courses;
using Application.Enrollments;
using Domain.Courses;
using Domain.Enrollments;
using Domain.Offerings;
using Infrastructure.Courses;
using Infrastructure.Enrollments;

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


    public static readonly Guid DemoStudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public static IStudentOfferingsReader CreateStudentOfferingsReader()
    {

        var ers = new Course(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "ERS", "Elementi razvoja softvera");
        var oib = new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "OIB", "Osnove informacione bezbednosti");

        var ersOffering = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000001"), ers.Id, "2026/27");
        var oibOffering = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000002"), oib.Id, "2026/27");
        var ersPrevious = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000003"), ers.Id, "2025/26");

        return new InMemoryStudentOfferingsReader(
            new[] { ers, oib },
            new[] { ersOffering, oibOffering, ersPrevious },
            new[]
            {

                new Membership(Guid.Parse("44444444-0000-0000-0000-000000000001"), DemoStudentId, ersOffering.Id),
                new Membership(Guid.Parse("44444444-0000-0000-0000-000000000002"), DemoStudentId, oibOffering.Id),
                new Membership(Guid.Parse("44444444-0000-0000-0000-000000000003"), DemoStudentId, ersPrevious.Id,
                    MembershipStatus.Terminated)
            });
    }
}
