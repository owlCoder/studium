using NUnit.Framework;
using Domain.TeachingSessions;

namespace Tests.Domain;

[TestFixture]
public sealed class TeachingSessionStateTests
{
    // Kreira sesiju u početnom stanju Planned.
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

        // Proverava da li se planirana sesija može označiti kao održana.
        Assert.That(session.State, Is.EqualTo(SessionState.Odrzan));
    }

    [Test]
    public void Cancel_FromPlanned_BecomesCancelled()
    {
        var session = Planned();

        session.Cancel();

        // Proverava promenu stanja iz Planned u Otkazan.
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

        // Otkazivanje treba da promeni samo stanje sesije.
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

        // Otkazana sesija ne može biti označena kao održana.
        Assert.That(
            () => session.MarkHeld(),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Cancel_OnHeld_Throws()
    {
        var session = Planned();
        session.MarkHeld();

        // Održana sesija ne može biti otkazana.
        Assert.That(
            () => session.Cancel(),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void MarkHeld_Twice_IsIdempotent()
    {
        var session = Planned();
        session.MarkHeld();

        // Ponovljeno izvršavanje operacije ne treba da izazove grešku.
        Assert.DoesNotThrow(() => session.MarkHeld());
        Assert.That(session.State, Is.EqualTo(SessionState.Odrzan));
    }

    [Test]
    public void Cancel_Twice_IsIdempotent()
    {
        var session = Planned();
        session.Cancel();

        // Ponovljeno otkazivanje ne treba da promeni postojeće stanje.
        Assert.DoesNotThrow(() => session.Cancel());
        Assert.That(session.State, Is.EqualTo(SessionState.Otkazan));
    }
}