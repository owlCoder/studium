using NUnit.Framework;
using Domain.TeachingSessions;

namespace Tests.Domain;

[TestFixture]
public sealed class TeachingSessionTests
{
    [Test]
    public void Constructor_CreatesPlannedSession()
    {
        var id = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var startsAt = new DateTime(2026, 11, 7, 10, 0, 0);
        var endsAt = new DateTime(2026, 11, 7, 11, 30, 0);

        var session = new TeachingSession(
            id, "Vežbe", offeringId, groupId, startsAt, endsAt, "Učionica 101");

        Assert.Multiple(() =>
        {
            Assert.That(session.Id, Is.EqualTo(id));
            Assert.That(session.Kind, Is.EqualTo("Vežbe"));
            Assert.That(session.OfferingId, Is.EqualTo(offeringId));
            Assert.That(session.GroupId, Is.EqualTo(groupId));
            Assert.That(session.TargetsWholeOffering, Is.False);
            Assert.That(session.StartsAt, Is.EqualTo(startsAt));
            Assert.That(session.EndsAt, Is.EqualTo(endsAt));
            Assert.That(session.Location, Is.EqualTo("Učionica 101"));
            Assert.That(session.State, Is.EqualTo(SessionState.Planiran));
        });
    }

    [Test]
    public void Constructor_TargetingWholeOffering_HasNoGroup()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Predavanje", Guid.NewGuid(), null,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Amfiteatar");

        Assert.Multiple(() =>
        {
            Assert.That(session.GroupId, Is.Null);
            Assert.That(session.TargetsWholeOffering, Is.True);
        });
    }

    [Test]
    public void Constructor_RejectsEndBeforeStart()
    {
        Assert.That(
            () => new TeachingSession(
                Guid.NewGuid(), "Vežbe", Guid.NewGuid(), Guid.NewGuid(),
                new DateTime(2026, 11, 7, 10, 0, 0),
                new DateTime(2026, 11, 7, 9, 30, 0),
                "Učionica 101"),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEqualStartAndEnd()
    {
        var moment = new DateTime(2026, 11, 7, 10, 0, 0);

        Assert.That(
            () => new TeachingSession(
                Guid.NewGuid(), "Vežbe", Guid.NewGuid(), Guid.NewGuid(),
                moment, moment,
                "Učionica 101"),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsBlankKind()
    {
        Assert.That(
            () => new TeachingSession(
                Guid.NewGuid(), " ", Guid.NewGuid(), Guid.NewGuid(),
                new DateTime(2026, 11, 7, 10, 0, 0),
                new DateTime(2026, 11, 7, 11, 0, 0),
                "Učionica 101"),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEmptyId()
    {
        Assert.That(
            () => new TeachingSession(
                Guid.Empty, "Vežbe", Guid.NewGuid(), Guid.NewGuid(),
                new DateTime(2026, 11, 7, 10, 0, 0),
                new DateTime(2026, 11, 7, 11, 0, 0),
                "Učionica 101"),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEmptyOffering()
    {
        Assert.That(
            () => new TeachingSession(
                Guid.NewGuid(), "Vežbe", Guid.Empty, Guid.NewGuid(),
                new DateTime(2026, 11, 7, 10, 0, 0),
                new DateTime(2026, 11, 7, 11, 0, 0),
                "Učionica 101"),
            Throws.TypeOf<ArgumentException>());
    }
}
