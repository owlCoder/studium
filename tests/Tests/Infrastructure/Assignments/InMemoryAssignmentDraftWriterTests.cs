using Application.Access;
using Application.Assignments;
using Domain.Assignments;
using Infrastructure.Assignments;
using Infrastructure.Repositories;
using NUnit.Framework;

namespace Tests.Infrastructure.Assignments;

[TestFixture]
public sealed class InMemoryAssignmentDraftWriterTests
{
    [Test]
    public async Task Handler_ShouldSaveDraftForRequestedCourseOffering()
    {
        var command = CreateCommand();
        var teacherId = Guid.NewGuid();
        var offerings = new InMemoryOfferingAccessReader(new[] { (teacherId, command.CourseOfferingId) });
        var access = new AssignmentCreationAccess(new UserContext(teacherId, UserRole.Teacher), offerings);
        var writer = new InMemoryAssignmentDraftWriter();
        var handler = new CreateAssignmentHandler(access, writer);

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

    [TestCase(UserRole.Teacher, false)]
    [TestCase(UserRole.Student, true)]
    [TestCase(UserRole.Assistant, true)]
    [TestCase(null, true)]
    public void Handler_ShouldRejectCreationWithoutTeachingAccess(UserRole? role, bool assignedToOffering)
    {
        var command = CreateCommand();
        var userId = Guid.NewGuid();
        var assignedOfferingId = assignedToOffering ? command.CourseOfferingId : Guid.NewGuid();
        var offerings = new InMemoryOfferingAccessReader(new[] { (userId, assignedOfferingId) });
        var currentUser = role is UserRole userRole ? new UserContext(userId, userRole) : null;
        var access = new AssignmentCreationAccess(currentUser, offerings);
        var handler = new CreateAssignmentHandler(access, new InMemoryAssignmentDraftWriter());

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.HandleAsync(command));
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
