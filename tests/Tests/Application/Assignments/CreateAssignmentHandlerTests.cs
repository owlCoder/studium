
using Application.Assignments;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Assignments;

[TestFixture]
public class CreateAssignmentHandlerTests
{
    private static CreateAssignmentCommand ValidCommand()
    {
        return new CreateAssignmentCommand(
            Guid.NewGuid(),
            "Domaci zadatak 1",
            "Opis zadatka",
            "Uputstvo za izradu",
            DateTimeOffset.UtcNow.AddDays(7));
    }

    [Test]
    public async Task ValidCommand_ShouldBePassedToDraftWriter()
    {
        var command = ValidCommand();

        var access = new Mock<IAssignmentCreationAccess>();
        access.Setup(x => x.CanCreateAsync(
                command.CourseOfferingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var writer = new Mock<IAssignmentDraftWriter>();
        writer.Setup(x => x.SaveAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreateAssignmentCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateAssignmentHandler(
            access.Object, writer.Object);

        Guid result = await handler.HandleAsync(command);

        Assert.That(result, Is.Not.EqualTo(Guid.Empty));

        writer.Verify(x => x.SaveAsync(
            result,
            command,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void EmptyTitle_ShouldNotBeSaved()
    {
        var command = ValidCommand() with { Title = " " };

        var access = new Mock<IAssignmentCreationAccess>();
        var writer = new Mock<IAssignmentDraftWriter>();

        var handler = new CreateAssignmentHandler(
            access.Object, writer.Object);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await handler.HandleAsync(command));

        writer.Verify(x => x.SaveAsync(
            It.IsAny<Guid>(),
            It.IsAny<CreateAssignmentCommand>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void EmptyOfferingId_ShouldBeRejected()
    {
        var command = ValidCommand() with
        {
            CourseOfferingId = Guid.Empty
        };

        var access = new Mock<IAssignmentCreationAccess>();
        var writer = new Mock<IAssignmentDraftWriter>();

        var handler = new CreateAssignmentHandler(
            access.Object, writer.Object);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await handler.HandleAsync(command));

        access.Verify(x => x.CanCreateAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void UnauthorizedUser_ShouldNotSaveDraft()
    {
        var command = ValidCommand();

        var access = new Mock<IAssignmentCreationAccess>();
        access.Setup(x => x.CanCreateAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var writer = new Mock<IAssignmentDraftWriter>();

        var handler = new CreateAssignmentHandler(
            access.Object, writer.Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await handler.HandleAsync(command));

        writer.Verify(x => x.SaveAsync(
            It.IsAny<Guid>(),
            It.IsAny<CreateAssignmentCommand>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void FailedSaving_ShouldNotReturnSuccess()
    {
        var command = ValidCommand();

        var access = new Mock<IAssignmentCreationAccess>();
        access.Setup(x => x.CanCreateAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var writer = new Mock<IAssignmentDraftWriter>();
        writer.Setup(x => x.SaveAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreateAssignmentCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(
                new InvalidOperationException(
                    "Cuvanje nije uspelo.")));

        var handler = new CreateAssignmentHandler(
            access.Object, writer.Object);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(command));
    }
}
