using Application.Assignments;
using Domain.Assignments;
using NUnit.Framework;

namespace Tests.Application.Assignments;

[TestFixture]
public sealed class PublishAssignmentHandlerTests
{
    [Test]
    public async Task HandleAsync_PublishesAndSavesCompleteDraft()
    {
        var assignmentId = Guid.NewGuid();
        var assignment = CompleteDraft();
        var repository = new FakeAssignmentRepository(assignmentId, assignment);
        var handler = new PublishAssignmentHandler(repository);

        var result = await handler.HandleAsync(assignmentId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Published));
            Assert.That(repository.SavedAssignment, Is.SameAs(assignment));
            Assert.That(repository.SaveCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsErrorWhenAssignmentDoesNotExist()
    {
        var repository = new FakeAssignmentRepository();
        var handler = new PublishAssignmentHandler(repository);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Zadatak ne postoji."));
            Assert.That(repository.SaveCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotSaveIncompleteDraft()
    {
        var assignmentId = Guid.NewGuid();
        var assignment = new Assignment("Zadatak", "Opis", " ", null);
        var repository = new FakeAssignmentRepository(assignmentId, assignment);
        var handler = new PublishAssignmentHandler(repository);

        var result = await handler.HandleAsync(assignmentId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Nedostaju obavezni podaci: uputstvo, rok."));
            Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Draft));
            Assert.That(repository.SaveCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotSaveAlreadyPublishedAssignmentAgain()
    {
        var assignmentId = Guid.NewGuid();
        var assignment = CompleteDraft();
        Assert.That(assignment.Publish(), Is.Null);

        var repository = new FakeAssignmentRepository(assignmentId, assignment);
        var handler = new PublishAssignmentHandler(repository);

        var result = await handler.HandleAsync(assignmentId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Samo zadatak u nacrtu može da se objavi."));
            Assert.That(assignment.State, Is.EqualTo(AssignmentStatus.Published));
            Assert.That(repository.SaveCallCount, Is.Zero);
        });
    }

    private static Assignment CompleteDraft()
    {
        return new Assignment("Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);
    }

    private sealed class FakeAssignmentRepository : IAssignmentRepository
    {
        private readonly Guid? _assignmentId;
        private readonly Assignment? _assignment;

        public FakeAssignmentRepository(Guid? assignmentId = null, Assignment? assignment = null)
        {
            _assignmentId = assignmentId;
            _assignment = assignment;
        }

        public Assignment? SavedAssignment { get; private set; }

        public int SaveCallCount { get; private set; }

        public Task<Assignment?> FindAsync(
            Guid assignmentId,
            CancellationToken cancellationToken = default)
        {
            var result = assignmentId == _assignmentId ? _assignment : null;
            return Task.FromResult(result);
        }

        public Task SaveAsync(
            Assignment assignment,
            CancellationToken cancellationToken = default)
        {
            SavedAssignment = assignment;
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }
}
