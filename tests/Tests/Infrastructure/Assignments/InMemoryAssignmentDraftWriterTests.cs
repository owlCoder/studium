using Application.Assignments;
using Domain.Assignments;
using Infrastructure.Assignments;
using Moq;
using NUnit.Framework;

namespace Tests.Infrastructure.Assignments;

[TestFixture]
public sealed class InMemoryAssignmentDraftWriterTests
{
    [Test]
    public async Task Handler_ShouldSaveDraftForRequestedCourseOffering()
    {
        var command = CreateCommand();
        var access = new Mock<IAssignmentCreationAccess>();
        access.Setup(x => x.CanCreateAsync(command.CourseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var writer = new InMemoryAssignmentDraftWriter();
        var handler = new CreateAssignmentHandler(access.Object, writer);

        var assignmentId = await handler.HandleAsync(command);
        var draft = writer.FindById(assignmentId);

        Assert.That(draft, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(draft!.Id, Is.EqualTo(assignmentId));
            Assert.That(draft.Status, Is.EqualTo(AssignmentStatus.Draft));
            Assert.That(draft.CourseOfferingId, Is.EqualTo(command.CourseOfferingId));
            Assert.That(draft.Title, Is.EqualTo(command.Title));
            Assert.That(draft.Description, Is.EqualTo(command.Description));
            Assert.That(draft.Instructions, Is.EqualTo(command.Instructions));
            Assert.That(draft.Deadline, Is.EqualTo(command.Deadline));
        });
    }

    [Test]
    public async Task SaveAsync_ShouldKeepExistingDraftWhenIdAlreadyExists()
    {
        var writer = new InMemoryAssignmentDraftWriter();
        var assignmentId = Guid.NewGuid();
        var command = CreateCommand();
        await writer.SaveAsync(assignmentId, command);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await writer.SaveAsync(assignmentId, command with { Title = "Drugi zadatak" }));
        Assert.That(writer.FindById(assignmentId)?.Title, Is.EqualTo(command.Title));
    }

    [Test]
    public void SaveAsync_ShouldNotSaveWhenAlreadyCanceled()
    {
        var writer = new InMemoryAssignmentDraftWriter();
        var assignmentId = Guid.NewGuid();
        var cancellationToken = new CancellationToken(canceled: true);

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await writer.SaveAsync(assignmentId, CreateCommand(), cancellationToken));
        Assert.That(writer.FindById(assignmentId), Is.Null);
    }

    private static CreateAssignmentCommand CreateCommand() => new(
        Guid.NewGuid(),
        "Domaći 1",
        "Opis zadatka",
        "Uputstvo za izradu",
        new DateTimeOffset(2026, 11, 10, 23, 59, 0, TimeSpan.FromHours(1)));
}
