using Domain.Activity;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class ActivityTests
{
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    [Test]
    public void Constructor_AssignsProperties()
    {
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var activity = new Activity(id, courseId, "Timski projekat", "Prva faza", Start, End);

        Assert.Multiple(() =>
        {
            Assert.That(activity.Id, Is.EqualTo(id));
            Assert.That(activity.CourseId, Is.EqualTo(courseId));
            Assert.That(activity.Name, Is.EqualTo("Timski projekat"));
            Assert.That(activity.Description, Is.EqualTo("Prva faza"));
            Assert.That(activity.StartDate, Is.EqualTo(Start));
            Assert.That(activity.EndDate, Is.EqualTo(End));
        });
    }

    [Test]
    public void Constructor_RejectsEmptyId()
    {
        Assert.That(
            () => new Activity(Guid.Empty, Guid.NewGuid(), "Aktivnost", "Opis", Start, End),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEmptyCourseId()
    {
        Assert.That(
            () => new Activity(Guid.NewGuid(), Guid.Empty, "Aktivnost", "Opis", Start, End),
            Throws.TypeOf<ArgumentException>());
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_RejectsBlankName(string name)
    {
        Assert.That(
            () => new Activity(Guid.NewGuid(), Guid.NewGuid(), name, "Opis", Start, End),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEqualStartAndEnd()
    {
        Assert.That(
            () => new Activity(Guid.NewGuid(), Guid.NewGuid(), "Aktivnost", "Opis", Start, Start),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEndBeforeStart()
    {
        Assert.That(
            () => new Activity(Guid.NewGuid(), Guid.NewGuid(), "Aktivnost", "Opis", End, Start),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_UsesEmptyDescriptionWhenDescriptionIsNull()
    {
        var activity = new Activity(Guid.NewGuid(), Guid.NewGuid(), "Aktivnost", null!, Start, End);

        Assert.That(activity.Description, Is.Empty);
    }
}
