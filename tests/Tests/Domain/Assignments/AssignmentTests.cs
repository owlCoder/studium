using Domain.Assignments;
using NUnit.Framework;

namespace Tests.Domain.Assignments;

[TestFixture]
public sealed class AssignmentTests
{
    private static readonly DateTimeOffset Deadline =
        new(2026, 11, 10, 23, 59, 0, TimeSpan.FromHours(1));

    [Test]
    public void CreateDraft_ShouldSetDraftStatusAndKeepCourseOfferingId()
    {
        var id = Guid.NewGuid();
        var courseOfferingId = Guid.NewGuid();

        var assignment = Assignment.CreateDraft(
            id, courseOfferingId, "Domaći 1", "Opis zadatka", "Uputstvo za izradu", Deadline);

        Assert.Multiple(() =>
        {
            Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Draft));
            Assert.That(assignment.CourseOfferingId, Is.EqualTo(courseOfferingId));
            Assert.That(assignment.Id, Is.EqualTo(id));
            Assert.That(assignment.Title, Is.EqualTo("Domaći 1"));
            Assert.That(assignment.Description, Is.EqualTo("Opis zadatka"));
            Assert.That(assignment.Instructions, Is.EqualTo("Uputstvo za izradu"));
            Assert.That(assignment.Deadline, Is.EqualTo(Deadline));
        });
    }

    [Test]
    public void CreateDraft_ShouldRejectEmptyAssignmentId()
    {
        var exception = Assert.Throws<ArgumentException>(() => Assignment.CreateDraft(
            Guid.Empty, Guid.NewGuid(), "Domaći 1", "Opis", "Uputstvo", Deadline));

        Assert.That(exception!.ParamName, Is.EqualTo("id"));
    }

    [Test]
    public void CreateDraft_ShouldRejectEmptyCourseOfferingId()
    {
        var exception = Assert.Throws<ArgumentException>(() => Assignment.CreateDraft(
            Guid.NewGuid(), Guid.Empty, "Domaći 1", "Opis", "Uputstvo", Deadline));

        Assert.That(exception!.ParamName, Is.EqualTo("courseOfferingId"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void CreateDraft_ShouldRejectMissingTitle(string? title)
    {
        var exception = Assert.Throws<ArgumentException>(() => Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), title!, "Opis", "Uputstvo", Deadline));

        Assert.That(exception!.ParamName, Is.EqualTo("title"));
    }

    [Test]
    public void GetMissingPublicationFields_ReturnsOnlyMissingFields()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", " \t", "Uputstvo", null);

        Assert.That(assignment.GetMissingPublicationFields(), Is.EqualTo(new[] { "opis", "rok" }));
    }

    [Test]
    public void GetMissingPublicationFields_ReturnsEmptyListForCompleteDraft()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", Deadline);

        Assert.That(assignment.GetMissingPublicationFields(), Is.Empty);
    }

    [Test]
    public void GetPublicationError_ReturnsNullWithoutPublishingCompleteDraft()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", Deadline);

        Assert.That(assignment.GetPublicationError(), Is.Null);
        Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Draft));
    }

    [Test]
    public void GetPublicationError_ReportsMissingFields()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", null);

        Assert.That(assignment.GetPublicationError(), Is.EqualTo("Nedostaju obavezni podaci: rok."));
    }

    [Test]
    public void GetPublicationError_ReportsAlreadyPublishedAssignment()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", Deadline);
        Assert.That(assignment.Publish(), Is.Null);

        Assert.That(assignment.GetPublicationError(), Is.EqualTo("Samo zadatak u nacrtu može da se objavi."));
    }

    [Test]
    public void IsVisibleToStudents_BecomesTrueAfterPublication()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", Deadline);

        Assert.That(assignment.IsVisibleToStudents(), Is.False);
        Assert.That(assignment.Publish(), Is.Null);
        Assert.That(assignment.IsVisibleToStudents(), Is.True);
    }

    [Test]
    public void IsVisibleToStudents_RemainsFalseAfterRejectedPublication()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", null);

        Assert.That(assignment.Publish(), Is.Not.Null);
        Assert.That(assignment.IsVisibleToStudents(), Is.False);
    }

    [Test]
    public void Publish_PublishesCompleteDraft()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", Deadline);

        var error = assignment.Publish();

        Assert.That(error, Is.Null);
        Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Published));
    }

    [Test]
    public void Publish_ReportsMissingFieldsAndKeepsDraft()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "  ", null);

        var error = assignment.Publish();

        Assert.That(error, Is.EqualTo("Nedostaju obavezni podaci: uputstvo, rok."));
        Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Draft));
    }

    [Test]
    public void Publish_RejectsAlreadyPublishedAssignment()
    {
        var assignment = Assignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Zadatak", "Opis", "Uputstvo", Deadline);
        Assert.That(assignment.Publish(), Is.Null);

        var error = assignment.Publish();

        Assert.That(error, Is.EqualTo("Samo zadatak u nacrtu može da se objavi."));
        Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Published));
    }
}
