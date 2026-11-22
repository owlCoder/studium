using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;
using Infrastructure.Activities;
using NUnit.Framework;

namespace Tests.Infrastructure.Activities;

/// <summary>US-7.9: memorijski izvor pojedinačnih rokova.</summary>
[TestFixture]
public sealed class InMemoryActivityStoreIndividualDeadlineTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void RokZaNepostojecuAktivnost_SeOdbija()
    {
        var store = new InMemoryActivityStore();

        Assert.Throws<InvalidOperationException>(() =>
            store.SetIndividualDeadline(new IndividualDeadline(Guid.NewGuid(), Guid.NewGuid(), Now)));
    }

    [Test]
    public async Task NoviRokZamenjujeStariZaIstogStudentaIAktivnost()
    {
        var store = new InMemoryActivityStore();
        var offering = Guid.NewGuid();
        var student = Guid.NewGuid();
        var task = new Activity(Guid.NewGuid(), offering, "assignment", true, Now);
        store.AddActivity(task);

        store.SetIndividualDeadline(new IndividualDeadline(task.Id, student, Now.AddDays(1)));
        store.SetIndividualDeadline(new IndividualDeadline(task.Id, student, Now.AddDays(4)));

        var result = await store.ListIndividualDeadlinesAsync(student, offering);

        Assert.That(result.Select(d => d.Deadline), Is.EqualTo(new[] { Now.AddDays(4) }));
    }

    [Test]
    public async Task VracaSamoRokoveTogStudentaNaTomIzvodjenju()
    {
        var store = new InMemoryActivityStore();
        var offering = Guid.NewGuid();
        var student = Guid.NewGuid();
        var mine = new Activity(Guid.NewGuid(), offering, "assignment", true, Now);
        var otherOffering = new Activity(Guid.NewGuid(), Guid.NewGuid(), "assignment", true, Now);
        store.AddActivity(mine);
        store.AddActivity(otherOffering);
        store.SetIndividualDeadline(new IndividualDeadline(mine.Id, student, Now.AddDays(1)));
        store.SetIndividualDeadline(new IndividualDeadline(otherOffering.Id, student, Now.AddDays(1)));
        store.SetIndividualDeadline(new IndividualDeadline(mine.Id, Guid.NewGuid(), Now.AddDays(1)));

        var result = await store.ListIndividualDeadlinesAsync(student, offering);

        Assert.That(result.Select(d => d.ActivityId), Is.EqualTo(new[] { mine.Id }));
    }
}
