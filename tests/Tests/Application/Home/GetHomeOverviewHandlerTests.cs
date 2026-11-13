using Application.Activities;
using Application.Engagements;
using Application.Enrollments;
using Application.Home;
using Domain.Dashboard;
using Domain.Staff;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Home;

[TestFixture]
public sealed class GetHomeOverviewHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _ers = Guid.NewGuid();
    private readonly Guid _oib = Guid.NewGuid();
    private readonly Guid _bp = Guid.NewGuid();

    private Mock<IStaffOfferingsReader> _staff = null!;
    private Mock<IStudentOfferingsReader> _student = null!;
    private Mock<IOpenActivityCounter> _counter = null!;

    
    private static readonly FeatureRegistry AssignmentsOnly = new(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments,
            "/offerings/{offeringId}/assignments/{itemId}")
    });

    [SetUp]
    public void SetUp()
    {
        _staff = new Mock<IStaffOfferingsReader>();
        _student = new Mock<IStudentOfferingsReader>();
        _counter = new Mock<IOpenActivityCounter>();
        SetupStaff();
        SetupStudent();
    }

    private void SetupStaff(params TeachingOfferingDto[] rows) =>
        _staff.Setup(x => x.ListActiveForUserAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    private void SetupStudent(params EnrolledOfferingDto[] rows) =>
        _student.Setup(x => x.ListActiveForStudentAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    private void SetupOpen(Guid offeringId, int count) =>
        _counter.Setup(x => x.CountOpenAsync(_userId, offeringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

    private GetHomeOverviewHandler Handler(FeatureRegistry? registry = null) =>
        new(new ListMyOfferingsHandler(_staff.Object, _student.Object), _counter.Object,
            registry ?? AssignmentsOnly);

    [Test]
    public async Task SpajaIzvodjenjaUloguIBrojOtvorenihAktivnostiUJedanOdgovor()
    {
        SetupStaff(new TeachingOfferingDto(_bp, "Baze podataka", "2026/27", Responsibility.Assistant));
        SetupStudent(
            new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27"),
            new EnrolledOfferingDto(_oib, "Osnove informacione bezbednosti", "2026/27"));
        SetupOpen(_ers, 2);
        SetupOpen(_oib, 0);

        var result = await Handler().HandleAsync(_userId);

        var teaching = result.Teaching.Single();
        var ers = result.Enrolled.Single(x => x.OfferingId == _ers);
        var oib = result.Enrolled.Single(x => x.OfferingId == _oib);

        Assert.Multiple(() =>
        {
            Assert.That(teaching.OfferingId, Is.EqualTo(_bp));
            Assert.That(teaching.CourseName, Is.EqualTo("Baze podataka"));
            Assert.That(teaching.AcademicYear, Is.EqualTo("2026/27"));
            Assert.That(teaching.Role, Is.EqualTo("Assistant"));
            Assert.That(teaching.OpenActivities, Is.Null);

            Assert.That(result.Enrolled, Has.Count.EqualTo(2));
            Assert.That(ers.OpenActivities, Is.EqualTo(2));
            Assert.That(ers.Role, Is.Null);
            Assert.That(oib.OpenActivities, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task SvakoIzvodjenjeImaVezuKaSvojojStraniciIPodrzanimOblastima()
    {
        SetupStaff(new TeachingOfferingDto(_bp, "Baze podataka", "2026/27", Responsibility.Teacher));
        SetupStudent(new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27"));

        var result = await Handler().HandleAsync(_userId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Teaching.Single().Links, Is.EqualTo(new[]
            {
                new HomeLinkDto("izvodjenje", $"/offerings/{_bp}"),
                new HomeLinkDto("assignments", $"/offerings/{_bp}/assignments")
            }));
            Assert.That(result.Enrolled.Single().Links, Is.EqualTo(new[]
            {
                new HomeLinkDto("izvodjenje", $"/offerings/{_ers}"),
                new HomeLinkDto("assignments", $"/offerings/{_ers}/assignments")
            }));
        });
    }

    [Test]
    public async Task NepodrzanaOblast_NemaVezu()
    {
        SetupStudent(new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27"));

        var result = await Handler(AssignmentsOnly).HandleAsync(_userId);

        var rels = result.Enrolled.Single().Links.Select(link => link.Rel).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(rels, Does.Not.Contain("materials"));
            Assert.That(rels, Does.Not.Contain("announcements"));
        });
    }

    [Test]
    public async Task StudentISaradnikNaIstomIzvodjenju_IzvodjenjeSePojavljujeJednom()
    {
        SetupStaff(new TeachingOfferingDto(_oib, "Osnove informacione bezbednosti", "2026/27", Responsibility.Assistant));
        SetupStudent(
            new EnrolledOfferingDto(_oib, "Osnove informacione bezbednosti", "2026/27"),
            new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27"));

        var result = await Handler().HandleAsync(_userId);

        var all = result.Teaching.Select(x => x.OfferingId)
            .Concat(result.Enrolled.Select(x => x.OfferingId))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(all, Is.Unique);
            Assert.That(result.Teaching.Select(x => x.OfferingId), Is.EqualTo(new[] { _oib }));
            Assert.That(result.Enrolled.Select(x => x.OfferingId), Is.EqualTo(new[] { _ers }));
        });

        _counter.Verify(x => x.CountOpenAsync(_userId, _oib, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task NeCuvaKopije_DrugiPozivCitaIzvorPonovo()
    {
        SetupStudent(new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27"));
        SetupOpen(_ers, 1);
        var handler = Handler();

        var first = await handler.HandleAsync(_userId);

        
        SetupStudent(new EnrolledOfferingDto(_oib, "Osnove informacione bezbednosti", "2026/27"));
        SetupOpen(_oib, 3);

        var second = await handler.HandleAsync(_userId);

        Assert.Multiple(() =>
        {
            Assert.That(first.Enrolled.Single().OfferingId, Is.EqualTo(_ers));
            Assert.That(first.Enrolled.Single().OpenActivities, Is.EqualTo(1));
            Assert.That(second.Enrolled.Single().OfferingId, Is.EqualTo(_oib));
            Assert.That(second.Enrolled.Single().OpenActivities, Is.EqualTo(3));
        });

        _student.Verify(x => x.ListActiveForStudentAsync(_userId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _staff.Verify(x => x.ListActiveForUserAsync(_userId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task KorisnikBezIzvodjenja_DobijaPrazanPregled()
    {
        var result = await Handler().HandleAsync(_userId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Teaching, Is.Empty);
            Assert.That(result.Enrolled, Is.Empty);
        });

        _counter.Verify(x => x.CountOpenAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void OdbijaPrazanIdKorisnika()
    {
        Assert.ThrowsAsync<ArgumentException>(() => Handler().HandleAsync(Guid.Empty));

        _staff.Verify(x => x.ListActiveForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _student.Verify(x => x.ListActiveForStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _counter.Verify(x => x.CountOpenAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
