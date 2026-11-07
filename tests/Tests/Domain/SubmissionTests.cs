using NUnit.Framework;
using Domain.Submissions;

namespace Tests.Domain;

[TestFixture]
public class SubmissionTests
{
    [Test]
    public void Constructor_SetsValues()
    {
        var id = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var submittedAt = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);
        var attachments = new List<Attachment>();
        attachments.Add(new Attachment("rad.pdf", "application/pdf", 2048));

        var submission = new Submission(id, assignmentId, studentId, " Moj rad ", attachments, submittedAt);

        Assert.That(submission.Id, Is.EqualTo(id));
        Assert.That(submission.AssignmentId, Is.EqualTo(assignmentId));
        Assert.That(submission.StudentId, Is.EqualTo(studentId));
        Assert.That(submission.Text, Is.EqualTo("Moj rad"));
        Assert.That(submission.Attachments.Count, Is.EqualTo(1));
        Assert.That(submission.SubmittedAt, Is.EqualTo(submittedAt));
    }

    [Test]
    public void Constructor_RejectsEmptyId()
    {
        Assert.That(
            () => new Submission(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Moj rad", null, DateTimeOffset.UtcNow),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEmptyAssignmentId()
    {
        Assert.That(
            () => new Submission(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "Moj rad", null, DateTimeOffset.UtcNow),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEmptyStudentId()
    {
        Assert.That(
            () => new Submission(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Moj rad", null, DateTimeOffset.UtcNow),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_NullTextAndAttachments_BecomeEmpty()
    {
        var submission = new Submission(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, DateTimeOffset.UtcNow);

        Assert.That(submission.Text, Is.EqualTo(""));
        Assert.That(submission.Attachments.Count, Is.EqualTo(0));
    }
}
