using NUnit.Framework;
using Domain.TeachingSessions;

namespace Tests.Domain;

[TestFixture]
public sealed class TeachingSessionStateTests
{
    private static TeachingSession Planned() => new(
        Guid.NewGuid(), "Vežbe", Guid.NewGuid(), Guid.NewGuid(),
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, 11, 30, 0),
        "Učionica 101");

    [Test]
    public void MarkHeld_FromPlanned_BecomesHeld()
    {
        var session = Planned();

        session.MarkHeld();

        Assert.That(session.State, Is.EqualTo(SessionState.Odrzan));
    }

    [Test]
    public void Cancel_FromPlanned_BecomesCancelled()
    {
        var session = Planned();

        session.Cancel();

        Assert.That(session.State, Is.EqualTo(SessionState.Otkazan));
    }

    [Test]
    public void Cancel_PreservesPriorData()
    {
        var session = Planned();
        var id = session.Id;
        var kind = session.Kind;
        var startsAt = session.StartsAt;
        var endsAt = session.EndsAt;
        var location = session.Location;

        session.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(session.Id, Is.EqualTo(id));
            Assert.That(session.Kind, Is.EqualTo(kind));
            Assert.That(session.StartsAt, Is.EqualTo(startsAt));
            Assert.That(session.EndsAt, Is.EqualTo(endsAt));
            Assert.That(session.Location, Is.EqualTo(location));
            Assert.That(session.State, Is.EqualTo(SessionState.Otkazan));
        });
    }

    [Test]
    public void MarkHeld_OnCancelled_Throws()
    {
        var session = Planned();
        session.Cancel();

        Assert.That(
            () => session.MarkHeld(),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Cancel_OnHeld_Throws()
    {
        var session = Planned();
        session.MarkHeld();

        Assert.That(
            () => session.Cancel(),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void MarkHeld_Twice_IsIdempotent()
    {
        var session = Planned();
        session.MarkHeld();

        Assert.DoesNotThrow(() => session.MarkHeld());
        Assert.That(session.State, Is.EqualTo(SessionState.Odrzan));
    }

    [Test]
    public void Cancel_Twice_IsIdempotent()
    {
        var session = Planned();
        session.Cancel();

        Assert.DoesNotThrow(() => session.Cancel());
        Assert.That(session.State, Is.EqualTo(SessionState.Otkazan));
    }
}
