using Domain.Assignments;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class AssignmentTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();

    [Test]
    public void Constructor_RejectsEmptyOfferingId()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new Assignment(Guid.Empty, "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch));

        Assert.That(error!.ParamName, Is.EqualTo("offeringId"));
    }

    [Test]
    public void GetMissingPublicationFields_ReturnsOnlyMissingFields()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", " \t", "Uputstvo", null);

        Assert.That(assignment.GetMissingPublicationFields(), Is.EqualTo(new[] { "opis", "rok" }));
    }

    [Test]
    public void GetMissingPublicationFields_ReturnsEmptyListForCompleteDraft()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);

        Assert.That(assignment.GetMissingPublicationFields(), Is.Empty);
    }

    [Test]
    public void GetPublicationError_ReturnsNullWithoutPublishingCompleteDraft()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);

        Assert.That(assignment.GetPublicationError(), Is.Null);
        Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Draft));
    }

    [Test]
    public void GetPublicationError_ReportsMissingFields()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", null);

        Assert.That(assignment.GetPublicationError(), Is.EqualTo("Nedostaju obavezni podaci: rok."));
    }

    [Test]
    public void GetPublicationError_ReportsAlreadyPublishedAssignment()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);
        Assert.That(assignment.Publish(), Is.Null);

        Assert.That(assignment.GetPublicationError(), Is.EqualTo("Samo zadatak u nacrtu može da se objavi."));
    }

    [Test]
    public void IsVisibleToStudents_BecomesTrueAfterPublication()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);

        Assert.That(assignment.IsVisibleToStudents(), Is.False);
        Assert.That(assignment.Publish(), Is.Null);
        Assert.That(assignment.IsVisibleToStudents(), Is.True);
    }

    [Test]
    public void IsVisibleToStudents_RemainsFalseAfterRejectedPublication()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", null);

        Assert.That(assignment.Publish(), Is.Not.Null);
        Assert.That(assignment.IsVisibleToStudents(), Is.False);
    }

    [Test]
    public void Publish_PublishesCompleteDraft()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);

        var error = assignment.Publish();

        Assert.That(error, Is.Null);
        Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Published));
    }

    [Test]
    public void Publish_ReportsMissingFieldsAndKeepsDraft()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "  ", null);

        var error = assignment.Publish();

        Assert.That(error, Is.EqualTo("Nedostaju obavezni podaci: uputstvo, rok."));
        Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Draft));
    }

    [Test]
    public void Publish_RejectsAlreadyPublishedAssignment()
    {
        var assignment = new Assignment(OfferingId, "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);
        Assert.That(assignment.Publish(), Is.Null);

        var error = assignment.Publish();

        Assert.That(error, Is.EqualTo("Samo zadatak u nacrtu može da se objavi."));
        Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Published));
    }
}
