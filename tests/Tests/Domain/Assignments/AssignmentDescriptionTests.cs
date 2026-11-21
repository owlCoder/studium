using NUnit.Framework;
using Domain.Assignments;

namespace Tests.Domain.Assignments;

/// <summary>
/// Testovi za izmenu opisa zadatka u domenu (ST-011, Izmena zadatka).
/// </summary>
[TestFixture]
public class AssignmentDescriptionTests
{
    // Pomoćna metoda: pravi zadatak u stanju Nacrt.
    private static Assignment CreateDraft()
    {
        var deadline = new DateTimeOffset(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);
        return Assignment.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "Domaći 1", "Opis zadatka", "Uputstvo za izradu", deadline);
    }

    // Pomoćna metoda: pravi zadatak u stanju Objavljen.
    private static Assignment CreatePublished()
    {
        var assignment = CreateDraft();
        assignment.Publish();
        return assignment;
    }

    [Test]
    public void ChangeDescription_Draft_DescriptionIsChanged()
    {
        var assignment = CreateDraft();

        var error = assignment.ChangeDescription("Novi opis");

        Assert.That(error, Is.Null);
        Assert.That(assignment.Description, Is.EqualTo("Novi opis"));
    }

    [Test]
    public void ChangeDescription_Published_DescriptionIsChanged()
    {
        var assignment = CreatePublished();

        var error = assignment.ChangeDescription("Dopunjen opis");

        Assert.That(error, Is.Null);
        Assert.That(assignment.Description, Is.EqualTo("Dopunjen opis"));
        Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Published));
    }

    [Test]
    public void ChangeDescription_Draft_CanBeLeftWithoutDescription()
    {
        // Nacrt sme da bude nepotpun, opis se proverava tek pri objavi.
        var assignment = CreateDraft();

        var error = assignment.ChangeDescription(null);

        Assert.That(error, Is.Null);
        Assert.That(assignment.Description, Is.Null);
    }

    [Test]
    public void ChangeDescription_Published_EmptyDescriptionIsRejected()
    {
        var assignment = CreatePublished();

        var error = assignment.ChangeDescription("");

        // Opis ostaje stari.
        Assert.That(error, Is.EqualTo("Objavljen zadatak mora imati opis."));
        Assert.That(assignment.Description, Is.EqualTo("Opis zadatka"));
    }

    [Test]
    public void ChangeDescription_Published_OnlySpacesIsRejected()
    {
        var assignment = CreatePublished();

        var error = assignment.ChangeDescription("   ");

        // Opis ostaje stari.
        Assert.That(error, Is.EqualTo("Objavljen zadatak mora imati opis."));
        Assert.That(assignment.Description, Is.EqualTo("Opis zadatka"));
    }

    [Test]
    public void ChangeDescription_OtherDataStaysTheSame()
    {
        var assignment = CreatePublished();

        assignment.ChangeDescription("Novi opis");

        Assert.That(assignment.Title, Is.EqualTo("Domaći 1"));
        Assert.That(assignment.Instructions, Is.EqualTo("Uputstvo za izradu"));
    }
}
