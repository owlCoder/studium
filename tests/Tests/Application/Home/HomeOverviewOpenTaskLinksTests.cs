using Application.Activities;
using Application.Engagements;
using Application.Enrollments;
using Application.Home;
using Domain.Activities;
using Domain.Dashboard;
using Domain.Staff;
using Infrastructure.Activities;
using Moq;
using NUnit.Framework;
using Tests.Application.Activities;

namespace Tests.Application.Home;


[TestFixture]
public sealed class HomeOverviewOpenTaskLinksTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _ers = Guid.NewGuid();
    private readonly Guid _bp = Guid.NewGuid();

    private static readonly FeatureRegistry AssignmentsOnly = new(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments,
            "/offerings/{offeringId}/assignments/{itemId}")
    });

    private InMemoryActivityStore _store = null!;
    private Mock<IStaffOfferingsReader> _staff = null!;
    private Mock<IStudentOfferingsReader> _student = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryActivityStore();
        _staff = new Mock<IStaffOfferingsReader>();
        _student = new Mock<IStudentOfferingsReader>();

        _staff.Setup(x => x.ListActiveForUserAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new TeachingOfferingDto(_bp, "Baze podataka", "2026/27", Responsibility.Assistant) });
        _student.Setup(x => x.ListActiveForStudentAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27") });
    }

    private GetHomeOverviewHandler Handler()
    {
        var time = new FixedTimeProvider(Now);
        return new GetHomeOverviewHandler(
            new ListMyOfferingsHandler(_staff.Object, _student.Object),
            new OpenActivityCounter(_store, _store, _store, AssignmentsOnly, time),
            AssignmentsOnly,
            new OpenTaskLinkReader(_store, _store, _store, AssignmentsOnly, time));
    }

    [Test]
    public async Task UpisanStudent_DobijaVezuKaPredmetuIKaSvakomOtvorenomZadatku()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        _store.AddActivity(new Activity(first, _ers, "assignment", true, Now.AddDays(1)));
        _store.AddActivity(new Activity(second, _ers, "assignment", true, Now.AddDays(5)));

        var ers = (await Handler().HandleAsync(_userId)).Enrolled.Single();

        Assert.Multiple(() =>
        {
            Assert.That(ers.Links.Select(l => l.Href), Does.Contain($"/offerings/{_ers}"));
            Assert.That(ers.OpenTasks.Select(t => t.Href), Is.EqualTo(new[]
            {
                $"/offerings/{_ers}/assignments/{first}",
                $"/offerings/{_ers}/assignments/{second}"
            }));
            Assert.That(ers.OpenTasks, Has.Count.EqualTo(ers.OpenActivities));
        });
    }

    [Test] 
    public async Task NacrtZadatka_NemaVezuNaPocetnojStraniStudenta()
    {
        var draft = Guid.NewGuid();
        _store.AddActivity(new Activity(draft, _ers, "assignment", isPublished: false, Now.AddDays(3)));

        var ers = (await Handler().HandleAsync(_userId)).Enrolled.Single();

        Assert.Multiple(() =>
        {
            Assert.That(ers.OpenTasks, Is.Empty);
            Assert.That(ers.Links.Any(l => l.Href.Contains(draft.ToString())), Is.False);
        });
    }

    [Test]
    public async Task IzvodjenjeKojePredajem_NemaStudentskeVezeKaZadacima()
    {
        _store.AddActivity(new Activity(Guid.NewGuid(), _bp, "assignment", true, Now.AddDays(3)));

        var bp = (await Handler().HandleAsync(_userId)).Teaching.Single();

        Assert.Multiple(() =>
        {
            Assert.That(bp.OpenTasks, Is.Empty);
            Assert.That(bp.Links.Select(l => l.Href), Does.Contain($"/offerings/{_bp}"));
        });
    }

    [Test] 
    public async Task VezeKaOblastima_SamoZaDostupneOblasti()
    {
        var ers = (await Handler().HandleAsync(_userId)).Enrolled.Single();

        Assert.That(ers.Links.Select(l => l.Rel), Is.EqualTo(new[] { "izvodjenje", "assignments" }));
    }
}
