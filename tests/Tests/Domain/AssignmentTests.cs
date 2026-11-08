using Domain.Assignments;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class AssignmentTests
{
    [Test]
    public void Publish_PublishesCompleteDraft()
    {
        var assignment = new Assignment("Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);

        var error = assignment.Publish();

        Assert.That(error, Is.Null);
        Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Published));
    }

    [Test]
    public void Publish_ReportsMissingFieldsAndKeepsDraft()
    {
        var assignment = new Assignment("Zadatak", "Opis", "  ", null);

        var error = assignment.Publish();

        Assert.That(error, Is.EqualTo("Nedostaju obavezni podaci: uputstvo, rok."));
        Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Draft));
    }

    [Test]
    public void Publish_RejectsAlreadyPublishedAssignment()
    {
        var assignment = new Assignment("Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);
        Assert.That(assignment.Publish(), Is.Null);

        var error = assignment.Publish();

        Assert.That(error, Is.EqualTo("Samo zadatak u nacrtu može da se objavi."));
        Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Published));
    }
}
