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
}
