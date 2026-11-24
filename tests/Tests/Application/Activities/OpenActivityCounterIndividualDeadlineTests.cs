using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Activities;
using Domain.Activities;
using Domain.Dashboard;
using Infrastructure.Activities;
using NUnit.Framework;

namespace Tests.Application.Activities;

/// <summary>US-7.9: brojač otvorenih aktivnosti primenjuje individualni rok studenta (ST-015).</summary>
[TestFixture]
public sealed class OpenActivityCounterIndividualDeadlineTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private static FeatureRegistry Registry() => new(new[]
    {
        new SupportedActivityType("assignment", FunctionalArea.Assignments,
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
        _counter = new OpenActivityCounter(_store, _store, _store, Registry(), new FixedTimeProvider(Now));
    }

    // Test iz US-7.9: produžen rok vraća zadatak u otvorene.
    [Test]
    public async Task ProduzenRok_VracaZadatakUOtvorene()
    {
        var task = new Activity(Guid.NewGuid(), _offering, "assignment", true, Now.AddHours(-2));
        _store.AddActivity(task);

        var before = await _counter.CountOpenAsync(_student, _offering);

        _store.SetIndividualDeadline(new IndividualDeadline(task.Id, _student, Now.AddDays(3)));

        var after = await _counter.CountOpenAsync(_student, _offering);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo(0), "Zajednički rok je prošao.");
            Assert.That(after, Is.EqualTo(1), "Produžen rok vraća zadatak u otvorene.");
        });
    }

    [Test]
    public async Task ProduzenjeJednogStudenta_NeMenjaBrojDrugom()
    {
        var other = Guid.NewGuid();
        var task = new Activity(Guid.NewGuid(), _offering, "assignment", true, Now.AddHours(-2));
        _store.AddActivity(task);
        _store.SetIndividualDeadline(new IndividualDeadline(task.Id, _student, Now.AddDays(3)));

        var mine = await _counter.CountOpenAsync(_student, _offering);
        var theirs = await _counter.CountOpenAsync(other, _offering);

        Assert.Multiple(() =>
        {
            Assert.That(mine, Is.EqualTo(1));
            Assert.That(theirs, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task BezIzvoraPojedinacnihRokova_VaziZajednickiRok()
    {
        // Stari konstruktor iz US-7.3 (bez ST-015) i dalje radi.
        var task = new Activity(Guid.NewGuid(), _offering, "assignment", true, Now.AddHours(-2));
        _store.AddActivity(task);
        _store.SetIndividualDeadline(new IndividualDeadline(task.Id, _student, Now.AddDays(3)));

        var withoutSt015 = new OpenActivityCounter(_store, _store, Registry(), new FixedTimeProvider(Now));

        Assert.That(await withoutSt015.CountOpenAsync(_student, _offering), Is.EqualTo(0));
    }
}
