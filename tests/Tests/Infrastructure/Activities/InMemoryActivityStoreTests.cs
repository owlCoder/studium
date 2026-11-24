using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Activities;
using Infrastructure.Activities;
using NUnit.Framework;

namespace Tests.Infrastructure.Activities;

[TestFixture]
public sealed class InMemoryActivityStoreTests
{
    [Test]
    public void PredajaZaNepostojecuAktivnost_SeOdbija()
    {
        var store = new InMemoryActivityStore();

        Assert.Throws<InvalidOperationException>(() =>
            store.AddSubmission(new ActivitySubmission(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
    }

    [Test]
    public async Task VracaSamoPredajeTogStudentaNaTomIzvodjenju()
    {
        var store = new InMemoryActivityStore();
        var offering = Guid.NewGuid();
        var otherOffering = Guid.NewGuid();
        var student = Guid.NewGuid();
        var deadline = DateTimeOffset.UtcNow.AddDays(1);

        var a1 = new Activity(Guid.NewGuid(), offering, "assignment", true, deadline);
        var a2 = new Activity(Guid.NewGuid(), otherOffering, "assignment", true, deadline);
        store.AddActivity(a1);
        store.AddActivity(a2);
        store.AddSubmission(new ActivitySubmission(Guid.NewGuid(), a1.Id, student));
        store.AddSubmission(new ActivitySubmission(Guid.NewGuid(), a2.Id, student));
        store.AddSubmission(new ActivitySubmission(Guid.NewGuid(), a1.Id, Guid.NewGuid()));

        var result = await store.ListForStudentAsync(student, offering);

        Assert.That(result.Select(s => s.ActivityId), Is.EqualTo(new[] { a1.Id }));
    }
}
