using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.Activities;
using Domain.Activities;
using Domain.Dashboard;
using Infrastructure.Activities;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Activities;

[TestFixture]
public sealed class OpenActivityCounterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
    private const string Assignment = "assignment";

    // Milenin registar (US-7.6): podržan je samo "assignment".
    private static FeatureRegistry Registry() => new(new[]
    {
        new SupportedActivityType(Assignment, FunctionalArea.Assignments,
            "/offerings/{offeringId}/assignments/{itemId}")
    });

    private Guid _student;
    private Guid _offering;
    private InMemoryActivityStore _store = null!;
    private OpenActivityCounter _counter = null!;

    [SetUp]
    public void SetUp()
    {
        _student = Guid.NewGuid();
        _offering = Guid.NewGuid();
        _store = new InMemoryActivityStore();
        _counter = new OpenActivityCounter(_store, _store, Registry(), new FixedTimeProvider(Now));
    }

    
    [Test]
    public async Task JedanOtvorenZadatak_Daje1_PoslePredaje0()
    {
        var task = new Activity(Guid.NewGuid(), _offering, Assignment,
            isPublished: true, deadline: Now.AddDays(7));
        _store.AddActivity(task);

        var before = await _counter.CountOpenAsync(_student, _offering);

        _store.AddSubmission(new ActivitySubmission(Guid.NewGuid(), task.Id, _student));

        var after = await _counter.CountOpenAsync(_student, _offering);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo(1));
            Assert.That(after, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task BrojiSamoVrstePodrzaneURegistruUS76()
    {
        _store.AddActivity(new Activity(Guid.NewGuid(), _offering, Assignment, true, Now.AddDays(1)));
        _store.AddActivity(new Activity(Guid.NewGuid(), _offering, "team-assignment", true, Now.AddDays(1)));
        _store.AddActivity(new Activity(Guid.NewGuid(), _offering, "quiz", true, Now.AddDays(1)));

        Assert.That(await _counter.CountOpenAsync(_student, _offering), Is.EqualTo(1));
    }

    [Test]
    public async Task BrojiSamoTrazenoIzvodjenje()
    {
        _store.AddActivity(new Activity(Guid.NewGuid(), _offering, Assignment, true, Now.AddDays(1)));
        _store.AddActivity(new Activity(Guid.NewGuid(), Guid.NewGuid(), Assignment, true, Now.AddDays(1)));

        Assert.That(await _counter.CountOpenAsync(_student, _offering), Is.EqualTo(1));
    }

    [Test]
    public async Task NeobjavljenaIIstekla_SeNeBroje()
    {
        _store.AddActivity(new Activity(Guid.NewGuid(), _offering, Assignment, false, Now.AddDays(1)));
        _store.AddActivity(new Activity(Guid.NewGuid(), _offering, Assignment, true, Now.AddDays(-1)));

        Assert.That(await _counter.CountOpenAsync(_student, _offering), Is.EqualTo(0));
    }

    [Test]
    public async Task IzvodjenjeBezAktivnosti_Daje0()
    {
        Assert.That(await _counter.CountOpenAsync(_student, _offering), Is.EqualTo(0));
    }

    [Test]
    public void OdbijaPrazanIdStudentaIliIzvodjenja()
    {
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<ArgumentException>(() => _counter.CountOpenAsync(Guid.Empty, _offering));
            Assert.ThrowsAsync<ArgumentException>(() => _counter.CountOpenAsync(_student, Guid.Empty));
        });
    }

    [Test]
    public async Task SvakiPozivPonovoCitaIzvor()
    {
        var activities = new Mock<IActivityReader>();
        activities.Setup(x => x.ListForOfferingAsync(_offering, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Activity>());
        var submissions = new Mock<IActivitySubmissionReader>();
        submissions.Setup(x => x.ListForStudentAsync(_student, _offering, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ActivitySubmission>());

        var counter = new OpenActivityCounter(activities.Object, submissions.Object,
            Registry(), new FixedTimeProvider(Now));

        await counter.CountOpenAsync(_student, _offering);
        await counter.CountOpenAsync(_student, _offering);

        activities.Verify(x => x.ListForOfferingAsync(_offering, It.IsAny<CancellationToken>()), Times.Exactly(2));
        submissions.Verify(x => x.ListForStudentAsync(_student, _offering, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
