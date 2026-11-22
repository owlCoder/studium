using Application.Activities;
using Application.Engagements;
using Application.Enrollments;
using Application.Home;
using Domain.Courses;
using Domain.Enrollments;
using Domain.Offerings;
using Domain.Staff;
using Infrastructure.Dashboard;
using Infrastructure.Engagements;
using Infrastructure.Enrollments;

namespace Tests.Acceptance;

internal sealed class St007Scenario
{
    public static readonly Guid StudentId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private readonly List<Course> _courses = new();
    private readonly List<Offering> _offerings = new();
    private readonly List<Membership> _memberships = new();

    public MutableActivitySource Activities { get; } = new();

    public Guid AddEnrolledOffering(string code, string courseName, string academicYear = "2026/27")
    {
        var course = new Course(Guid.NewGuid(), code, courseName);
        var offering = new Offering(Guid.NewGuid(), course.Id, academicYear);
        _courses.Add(course);
        _offerings.Add(offering);
        _memberships.Add(new Membership(Guid.NewGuid(), StudentId, offering.Id));
        return offering.Id;
    }

    public GetHomeOverviewHandler CreateHandler(TimeProvider time)
    {
        var registry = SupportedFeatures.CreateRegistry();
        var students = new InMemoryStudentOfferingsReader(_courses, _offerings, _memberships);
        var staff = new InMemoryStaffOfferingsReader(_courses, _offerings, Array.Empty<TeachingAssignment>());
        var counter = new OpenActivityCounter(Activities, Activities, registry, time);

        return new GetHomeOverviewHandler(new ListMyOfferingsHandler(staff, students), counter, registry);
    }

    public IStudentOfferingsReader CreateStudentReader() =>
        new InMemoryStudentOfferingsReader(_courses, _offerings, _memberships);
}
