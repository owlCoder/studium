using Moq;
using NUnit.Framework;
using Application.Submissions;
using Domain.Submissions;

namespace Tests.Application;

[TestFixture]
public class SubmitWorkHandlerTests
{
    [Test]
    public async Task HandleAsync_SavesSubmissionWithStudentFromLogin()
    {
        var studentId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(studentId);

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        var repository = new Mock<ISubmissionRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Submission>())).Returns(Task.CompletedTask);

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, repository.Object);
        var command = new SubmitWorkCommand(assignmentId, "Moj rad", null);

        var result = await handler.HandleAsync(command);

        repository.Verify(x => x.AddAsync(
            It.Is<Submission>(s =>
                s.Id == result.SubmissionId &&
                s.StudentId == studentId &&
                s.AssignmentId == assignmentId &&
                s.Text == "Moj rad")),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_ConfirmationHasTimeFromClock()
    {
        var now = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        var repository = new Mock<ISubmissionRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Submission>())).Returns(Task.CompletedTask);

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "Moj rad", null);

        var result = await handler.HandleAsync(command);

        Assert.That(result.SubmissionId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.SubmittedAt, Is.EqualTo(now));
    }

    [Test]
    public void HandleAsync_EmptySubmission_IsNotSaved()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var repository = new Mock<ISubmissionRepository>();

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "   ", null);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await handler.HandleAsync(command));

        repository.Verify(x => x.AddAsync(It.IsAny<Submission>()), Times.Never);
    }
}
