using Domain.Activity;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class ActivityTests
{
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    [Test]
    public void TryCreate_AssignsProperties()
    {
        var id = Guid.NewGuid();
        var contextId = Guid.NewGuid();

        var creation = Activity.TryCreate(id, contextId, "Timski projekat", "Prva faza", Start, End);

        Assert.That(creation.IsSuccess, Is.True);
        var activity = creation.Activity!;
        Assert.Multiple(() =>
        {
            Assert.That(activity.Id, Is.EqualTo(id));
            Assert.That(activity.ContextId, Is.EqualTo(contextId));
            Assert.That(activity.Name, Is.EqualTo("Timski projekat"));
            Assert.That(activity.Description, Is.EqualTo("Prva faza"));
            Assert.That(activity.StartDate, Is.EqualTo(Start));
            Assert.That(activity.EndDate, Is.EqualTo(End));
        });
    }

    [Test]
    public void TryCreate_RejectsEmptyId()
    {
        var creation = Activity.TryCreate(Guid.Empty, Guid.NewGuid(), "Aktivnost", "Opis", Start, End);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsEmptyContextId()
    {
        var creation = Activity.TryCreate(Guid.NewGuid(), Guid.Empty, "Aktivnost", "Opis", Start, End);

        AssertFailure(creation);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void TryCreate_RejectsBlankName(string? name)
    {
        var creation = Activity.TryCreate(Guid.NewGuid(), Guid.NewGuid(), name, "Opis", Start, End);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsEqualStartAndEnd()
    {
        var creation = Activity.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Aktivnost", "Opis", Start, Start);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsEndBeforeStart()
    {
        var creation = Activity.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Aktivnost", "Opis", End, Start);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_UsesEmptyDescriptionWhenDescriptionIsNull()
    {
        var creation = Activity.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Aktivnost", null, Start, End);

        Assert.That(creation.IsSuccess, Is.True);
        Assert.That(creation.Activity!.Description, Is.Empty);
    }

    private static void AssertFailure(ActivityCreation creation)
    {
        Assert.Multiple(() =>
        {
            Assert.That(creation.IsSuccess, Is.False);
            Assert.That(creation.Activity, Is.Null);
            Assert.That(creation.Error, Is.Not.Null.And.Not.Empty);
        });
    }
}
