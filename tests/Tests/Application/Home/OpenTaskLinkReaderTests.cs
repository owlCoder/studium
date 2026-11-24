using Application.Home;
using Domain.Activities;
using Domain.Dashboard;
using Infrastructure.Activities;
using NUnit.Framework;
using Tests.Application.Activities;

namespace Tests.Application.Home;

[TestFixture]
public sealed class OpenTaskLinkReaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly Guid Ers = Guid.NewGuid();

    
    private static readonly FeatureRegistry Registry = new(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments,
            "/offerings/{offeringId}/assignments/{itemId}")
    });

    private InMemoryActivityStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryActivityStore();
    }

    private OpenTaskLinkReader Reader() =>
        new(_store, _store, _store, Registry, new FixedTimeProvider(Now));

    private Guid AddTask(bool published = true, int daysToDeadline = 3, string type = "assignment")
    {
        var id = Guid.NewGuid();
        _store.AddActivity(new Activity(id, Ers, type, published, Now.AddDays(daysToDeadline)));
        return id;
    }

    [Test]
    public async Task OtvorenZadatak_DobijaVezuKaBasTomZadatku()
    {
        var taskId = AddTask();

        var links = await Reader().ListForStudentAsync(Student, Ers);

        var link = links.Single();
        Assert.Multiple(() =>
        {
            Assert.That(link.ActivityId, Is.EqualTo(taskId));
            Assert.That(link.ActivityType, Is.EqualTo("assignment"));
            Assert.That(link.Href, Is.EqualTo($"/offerings/{Ers}/assignments/{taskId}"));
            Assert.That(link.Deadline, Is.EqualTo(Now.AddDays(3)));
        });
    }

    [Test] 
    public async Task VezaKaNacrtuZadatka_SeNePojavljujeStudentu()
    {
        var published = AddTask(published: true);
        var draft = AddTask(published: false);

        var links = await Reader().ListForStudentAsync(Student, Ers);

        Assert.Multiple(() =>
        {
            Assert.That(links.Select(l => l.ActivityId), Is.EqualTo(new[] { published }));
            Assert.That(links.Any(l => l.Href.Contains(draft.ToString())), Is.False);
        });
    }

    [Test]
    public async Task NepodrzanaVrsta_NemaVezu()
    {
        AddTask(type: "quiz");

        var links = await Reader().ListForStudentAsync(Student, Ers);

        Assert.That(links, Is.Empty);
    }

    [Test]
    public async Task PredatZadatakIProsaoRok_NemajuVezu()
    {
        var submitted = AddTask();
        AddTask(daysToDeadline: -1);
        _store.AddSubmission(new ActivitySubmission(Guid.NewGuid(), submitted, Student));

        var links = await Reader().ListForStudentAsync(Student, Ers);

        Assert.That(links, Is.Empty);
    }

    [Test] 
    public async Task ProduzenRok_VracaVezuSaPojedinacnimRokom()
    {
        var taskId = AddTask(daysToDeadline: -1);
        _store.SetIndividualDeadline(new IndividualDeadline(taskId, Student, Now.AddDays(2)));

        var links = await Reader().ListForStudentAsync(Student, Ers);

        Assert.That(links.Single().Deadline, Is.EqualTo(Now.AddDays(2)));
    }

    [Test]
    public async Task VezeSuPoredanePoRoku()
    {
        var later = AddTask(daysToDeadline: 9);
        var sooner = AddTask(daysToDeadline: 1);

        var links = await Reader().ListForStudentAsync(Student, Ers);

        Assert.That(links.Select(l => l.ActivityId), Is.EqualTo(new[] { sooner, later }));
    }

    [Test]
    public void OdbijaPrazneIdentifikatore()
    {
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<ArgumentException>(() => Reader().ListForStudentAsync(Guid.Empty, Ers));
            Assert.ThrowsAsync<ArgumentException>(() => Reader().ListForStudentAsync(Student, Guid.Empty));
        });
    }
}
