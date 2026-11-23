using Application.Submissions;
using Domain.Submissions;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public class GetOwnSubmissionHandlerTests
{
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _assignmentId = Guid.NewGuid();

    [Test]
    public async Task HandleAsync_ReturnsOwnCurrentSubmissionWithItsDetails()
    {
        var submission = CreateSubmission(_assignmentId, _studentId);
        var repository = new Mock<ISubmissionRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindCurrentAsync(_assignmentId, _studentId)).ReturnsAsync(submission);
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(_assignmentId);

        Assert.That(result, Is.SameAs(submission));
        repository.Verify(x => x.FindCurrentAsync(_assignmentId, _studentId), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ReturnsNullWhenStudentHasNoSubmission()
    {
        var repository = new Mock<ISubmissionRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindCurrentAsync(_assignmentId, _studentId)).ReturnsAsync((Submission?)null);

        var result = await CreateHandler(repository).HandleAsync(_assignmentId);

        Assert.That(result, Is.Null);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task HandleAsync_DoesNotExposeSubmissionOutsideRequestedScope(bool otherStudent)
    {
        var submission = otherStudent
            ? CreateSubmission(_assignmentId, Guid.NewGuid())
            : CreateSubmission(Guid.NewGuid(), _studentId);
        var repository = new Mock<ISubmissionRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindCurrentAsync(_assignmentId, _studentId)).ReturnsAsync(submission);

        var result = await CreateHandler(repository).HandleAsync(_assignmentId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_ReadsCurrentSubmissionOnEveryRequest()
    {
        var previous = CreateSubmission(_assignmentId, _studentId);
        var current = CreateSubmission(_assignmentId, _studentId);
        var repository = new Mock<ISubmissionRepository>(MockBehavior.Strict);
        repository.SetupSequence(x => x.FindCurrentAsync(_assignmentId, _studentId))
            .ReturnsAsync(previous)
            .ReturnsAsync(current);
        var handler = CreateHandler(repository);

        Assert.That(await handler.HandleAsync(_assignmentId), Is.SameAs(previous));
        Assert.That(await handler.HandleAsync(_assignmentId), Is.SameAs(current));
    }

    [Test]
    public void HandleAsync_RejectsMissingStudentIdentity()
    {
        var repository = new Mock<ISubmissionRepository>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.StudentId).Returns(Guid.Empty);
        var handler = new GetOwnSubmissionHandler(currentUser.Object, repository.Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.HandleAsync(_assignmentId));
        repository.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_RejectsEmptyAssignmentId()
    {
        var repository = new Mock<ISubmissionRepository>(MockBehavior.Strict);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await CreateHandler(repository).HandleAsync(Guid.Empty));
        repository.VerifyNoOtherCalls();
    }

    private GetOwnSubmissionHandler CreateHandler(Mock<ISubmissionRepository> repository)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.StudentId).Returns(_studentId);
        return new GetOwnSubmissionHandler(currentUser.Object, repository.Object);
    }

    private static Submission CreateSubmission(Guid assignmentId, Guid studentId)
    {
        return new Submission(Guid.NewGuid(), assignmentId, studentId, "Moj rad",
            new List<Attachment> { new Attachment("rad.pdf", "application/pdf", 1024) },
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));
    }
}
