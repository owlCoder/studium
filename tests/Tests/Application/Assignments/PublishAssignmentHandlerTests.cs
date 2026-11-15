using Application.Assignments;
using Domain.Assignments;
using Infrastructure.Assignments;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Assignments;

[TestFixture]
public sealed class PublishAssignmentHandlerTests
{
    [Test]
    public async Task HandleAsync_PublishesAndSavesCompleteDraft()
    {
        var assignment = CompleteDraft();
        var repository = new FakeAssignmentRepository(assignment.Id, assignment);
        var access = AllowPublicationOn(assignment.CourseOfferingId);
        var handler = new PublishAssignmentHandler(repository, access.Object);

        var result = await handler.HandleAsync(assignment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Published));
            Assert.That(repository.SavedAssignment, Is.SameAs(assignment));
            Assert.That(repository.SaveCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotPublishWhenTeacherIsAssignedToAnotherOffering()
    {
        var assignment = CompleteDraft();
        var repository = new FakeAssignmentRepository(assignment.Id, assignment);
        var access = AllowPublicationOn(Guid.NewGuid());
        var handler = new PublishAssignmentHandler(repository, access.Object);

        var result = await handler.HandleAsync(assignment.Id);

        access.Verify(x => x.CanPublishAsync(assignment.CourseOfferingId, CancellationToken.None), Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Nemate pravo da objavite zadatak na ovom izvođenju predmeta."));
            Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Draft));
            Assert.That(repository.SaveCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsErrorWhenAssignmentDoesNotExist()
    {
        var repository = new FakeAssignmentRepository();
        var access = new Mock<IAssignmentPublicationAccess>(MockBehavior.Strict);
        var handler = new PublishAssignmentHandler(repository, access.Object);

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
        var courseOfferingId = Guid.NewGuid();
        var assignment = Assignment.CreateDraft(assignmentId, courseOfferingId, "Zadatak", "Opis", " ", null);
        var repository = new FakeAssignmentRepository(assignmentId, assignment);
        var handler = new PublishAssignmentHandler(repository, AllowPublicationOn(courseOfferingId).Object);

        var result = await handler.HandleAsync(assignmentId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Nedostaju obavezni podaci: uputstvo, rok."));
            Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Draft));
            Assert.That(repository.SaveCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task HandleAsync_DoesNotSaveAlreadyPublishedAssignmentAgain()
    {
        var assignment = CompleteDraft();
        Assert.That(assignment.Publish(), Is.Null);

        var repository = new FakeAssignmentRepository(assignment.Id, assignment);
        var handler = new PublishAssignmentHandler(repository, AllowPublicationOn(assignment.CourseOfferingId).Object);

        var result = await handler.HandleAsync(assignment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Samo zadatak u nacrtu može da se objavi."));
            Assert.That(assignment.Status, Is.EqualTo(AssignmentStatus.Published));
            Assert.That(repository.SaveCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task CreateAndPublishFlow_PublishesCreatedDraftWhenComplete()
    {
        var courseOfferingId = Guid.NewGuid();
        var store = new InMemoryAssignmentDraftWriter();

        var createAccess = new Mock<IAssignmentCreationAccess>();
        createAccess.Setup(x => x.CanCreateAsync(courseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var createHandler = new CreateAssignmentHandler(createAccess.Object, store);
        var publishHandler = new PublishAssignmentHandler(store, AllowPublicationOn(courseOfferingId).Object);

        var assignmentId = await createHandler.HandleAsync(new CreateAssignmentCommand(
            courseOfferingId,
            "Domaći 1",
            "Opis zadatka",
            "Uputstvo za izradu",
            DateTimeOffset.UnixEpoch));

        var result = await publishHandler.HandleAsync(assignmentId);
        var stored = store.FindById(assignmentId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Status, Is.EqualTo(AssignmentStatus.Published));
            Assert.That(stored.IsVisibleToStudents(), Is.True);
        });
    }

    [Test]
    public async Task CreateAndPublishFlow_RejectsCreatedDraftWhenIncomplete()
    {
        var courseOfferingId = Guid.NewGuid();
        var store = new InMemoryAssignmentDraftWriter();

        var createAccess = new Mock<IAssignmentCreationAccess>();
        createAccess.Setup(x => x.CanCreateAsync(courseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var createHandler = new CreateAssignmentHandler(createAccess.Object, store);
        var publishHandler = new PublishAssignmentHandler(store, AllowPublicationOn(courseOfferingId).Object);

        var assignmentId = await createHandler.HandleAsync(new CreateAssignmentCommand(
            courseOfferingId,
            "Domaći 1",
            null,
            "Uputstvo za izradu",
            null));

        var result = await publishHandler.HandleAsync(assignmentId);
        var stored = store.FindById(assignmentId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Nedostaju obavezni podaci: opis, rok."));
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Status, Is.EqualTo(AssignmentStatus.Draft));
            Assert.That(stored.IsVisibleToStudents(), Is.False);
        });
    }

    private static Assignment CompleteDraft()
    {
        return Assignment.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Zadatak",
            "Opis",
            "Uputstvo",
            DateTimeOffset.UnixEpoch);
    }

    private static Mock<IAssignmentPublicationAccess> AllowPublicationOn(Guid courseOfferingId)
    {
        var access = new Mock<IAssignmentPublicationAccess>();
        access.Setup(x => x.CanPublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid checkedOfferingId, CancellationToken _) => checkedOfferingId == courseOfferingId);
        return access;
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
