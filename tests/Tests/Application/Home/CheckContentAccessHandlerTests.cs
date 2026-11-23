using Application.Engagements;
using Application.Enrollments;
using Application.Home;
using Domain.Activities;
using Domain.Dashboard;
using Domain.Staff;
using Infrastructure.Activities;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Home;


[TestFixture]
public sealed class CheckContentAccessHandlerTests
{
    private static readonly DateTimeOffset Deadline = new(2026, 12, 1, 23, 59, 0, TimeSpan.Zero);

    private readonly Guid _student = Guid.NewGuid();
    private readonly Guid _assistant = Guid.NewGuid();
    private readonly Guid _stranger = Guid.NewGuid();
    private readonly Guid _ers = Guid.NewGuid();
    private readonly Guid _oib = Guid.NewGuid();

    private static readonly FeatureRegistry AssignmentsOnly = new(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments,
            "/offerings/{offeringId}/assignments/{itemId}")
    });

    private InMemoryActivityStore _store = null!;
    private Mock<IStaffOfferingsReader> _staff = null!;
    private Mock<IStudentOfferingsReader> _students = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryActivityStore();
        _staff = new Mock<IStaffOfferingsReader>();
        _students = new Mock<IStudentOfferingsReader>();

        _staff.Setup(x => x.ListActiveForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TeachingOfferingDto>());
        _students.Setup(x => x.ListActiveForStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EnrolledOfferingDto>());

        _students.Setup(x => x.ListActiveForStudentAsync(_student, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27") });
        _staff.Setup(x => x.ListActiveForUserAsync(_assistant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new TeachingOfferingDto(_ers, "Elementi razvoja softvera", "2026/27", Responsibility.Assistant) });
    }

    private CheckContentAccessHandler Handler() =>
        new(new ListMyOfferingsHandler(_staff.Object, _students.Object), _store, _store, AssignmentsOnly);

    private Guid AddTask(Guid offeringId, bool published = true, string type = "assignment")
    {
        var id = Guid.NewGuid();
        _store.AddActivity(new Activity(id, offeringId, type, published, Deadline));
        return id;
    }

    [Test]
    public async Task UpisanStudent_OtvaraSvojPredmet()
    {
        var access = await Handler().ForOfferingAsync(_student, _ers);

        Assert.Multiple(() =>
        {
            Assert.That(access, Is.Not.Null);
            Assert.That(access!.CourseName, Is.EqualTo("Elementi razvoja softvera"));
            Assert.That(access.Role, Is.Null);
        });
    }

    [Test]
    public async Task Saradnik_OtvaraPredmetSaUlogom()
    {
        var access = await Handler().ForOfferingAsync(_assistant, _ers);

        Assert.That(access?.Role, Is.EqualTo("Assistant"));
    }

    [Test]
    public async Task KorisnikBezClanstva_NeOtvaraTudjiPredmetNiZadatak()
    {
        var taskId = AddTask(_ers);

        var offering = await Handler().ForOfferingAsync(_stranger, _ers);
        var assignment = await Handler().ForAssignmentAsync(_stranger, _ers, taskId);

        Assert.Multiple(() =>
        {
            Assert.That(offering, Is.Null);
            Assert.That(assignment, Is.Null);
        });
    }

    [Test]
    public async Task UpisanStudent_OtvaraObjavljenZadatak()
    {
        var taskId = AddTask(_ers);

        var access = await Handler().ForAssignmentAsync(_student, _ers, taskId);

        Assert.Multiple(() =>
        {
            Assert.That(access, Is.Not.Null);
            Assert.That(access!.ActivityId, Is.EqualTo(taskId));
            Assert.That(access.Deadline, Is.EqualTo(Deadline));
        });
    }

    [Test] 
    public async Task StudentNeOtvaraNacrtZadatka_NiPrekoRucnoUpisaneVeze()
    {
        var draft = AddTask(_ers, published: false);

        var access = await Handler().ForAssignmentAsync(_student, _ers, draft);

        Assert.That(access, Is.Null);
    }

    [Test]
    public async Task Saradnik_OtvaraNacrtNaSvomIzvodjenju()
    {
        var draft = AddTask(_ers, published: false);

        var access = await Handler().ForAssignmentAsync(_assistant, _ers, draft);

        Assert.That(access?.IsPublished, Is.False);
    }

    [Test]
    public async Task ZadatakDrugogIzvodjenja_SeNeOtvaraPrekoMogPredmeta()
    {
        var otherTask = AddTask(_oib);

        var access = await Handler().ForAssignmentAsync(_student, _ers, otherTask);

        Assert.That(access, Is.Null);
    }

    [Test]
    public async Task NepodrzanaVrsta_SeNeOtvara()
    {
        var quiz = AddTask(_ers, type: "quiz");

        var access = await Handler().ForAssignmentAsync(_student, _ers, quiz);

        Assert.That(access, Is.Null);
    }

    [Test]
    public void OdbijaPrazneIdentifikatore()
    {
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<ArgumentException>(() => Handler().ForOfferingAsync(Guid.Empty, _ers));
            Assert.ThrowsAsync<ArgumentException>(() => Handler().ForOfferingAsync(_student, Guid.Empty));
            Assert.ThrowsAsync<ArgumentException>(() => Handler().ForAssignmentAsync(_student, _ers, Guid.Empty));
        });
    }
}
