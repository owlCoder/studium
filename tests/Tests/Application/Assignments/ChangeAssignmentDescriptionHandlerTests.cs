using Moq;
using NUnit.Framework;
using Application.Assignments;
using Domain.Assignments;

namespace Tests.Application.Assignments;

/// <summary>
/// Testovi za izmenu opisa zadatka (ST-011, Izmena zadatka).
/// </summary>
[TestFixture]
public class ChangeAssignmentDescriptionHandlerTests
{
    // Pomoćna metoda: pravi zadatak u stanju Nacrt.
    private static Assignment CreateDraft()
    {
        var deadline = new DateTimeOffset(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);
        return Assignment.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "Domaći 1", "Opis zadatka", "Uputstvo za izradu", deadline);
    }

    // Pomoćna metoda: pravi zadatak u stanju Objavljen.
    private static Assignment CreatePublished()
    {
        var assignment = CreateDraft();
        assignment.Publish();
        return assignment;
    }

    // Pomoćna metoda: nastavnik sme da menja zadatke na svakom predmetu.
    // Koriste je svi testovi kojima pravo pristupa nije ono što se proverava.
    private static Mock<IAssignmentEditAccess> AllowedTeacher()
    {
        var access = new Mock<IAssignmentEditAccess>();
        access.Setup(x => x.CanEditAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        return access;
    }

    [Test]
    public async Task HandleAsync_Draft_DescriptionIsChangedAndSaved()
    {
        var assignment = CreateDraft();
        var repository = new FakeAssignmentRepository(assignment);
        var handler = new ChangeAssignmentDescriptionHandler(repository, AllowedTeacher().Object);
        var command = new ChangeAssignmentDescriptionCommand(assignment.Id, "Novi opis");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(assignment.Description, Is.EqualTo("Novi opis"));
        Assert.That(repository.SaveCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_Published_DescriptionIsChangedAndSaved()
    {
        var assignment = CreatePublished();
        var repository = new FakeAssignmentRepository(assignment);
        var handler = new ChangeAssignmentDescriptionHandler(repository, AllowedTeacher().Object);
        var command = new ChangeAssignmentDescriptionCommand(assignment.Id, "Dopunjen opis");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(assignment.Description, Is.EqualTo("Dopunjen opis"));
        Assert.That(repository.SaveCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_AssignmentDoesNotExist_IsRejectedAndNotSaved()
    {
        // Skladište je prazno, zadatak ne postoji.
        var repository = new FakeAssignmentRepository(null);
        var handler = new ChangeAssignmentDescriptionHandler(repository, AllowedTeacher().Object);
        var command = new ChangeAssignmentDescriptionCommand(Guid.NewGuid(), "Novi opis");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Zadatak ne postoji."));
        Assert.That(repository.SaveCount, Is.EqualTo(0));
    }

    [Test]
    public async Task HandleAsync_TeacherNotOnCourse_IsRejectedAndNotSaved()
    {
        var assignment = CreatePublished();
        var repository = new FakeAssignmentRepository(assignment);

        // Nastavnik nije angažovan na izvođenju predmeta kome zadatak pripada.
        var access = new Mock<IAssignmentEditAccess>();
        access.Setup(x => x.CanEditAsync(assignment.CourseOfferingId)).ReturnsAsync(false);

        var handler = new ChangeAssignmentDescriptionHandler(repository, access.Object);
        var command = new ChangeAssignmentDescriptionCommand(assignment.Id, "Novi opis");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Nemate pravo da menjate zadatak na ovom izvođenju predmeta."));
        Assert.That(assignment.Description, Is.EqualTo("Opis zadatka"));
        Assert.That(repository.SaveCount, Is.EqualTo(0));
    }

    [Test]
    public async Task HandleAsync_AccessIsCheckedForAssignmentsCourse()
    {
        var assignment = CreateDraft();
        var repository = new FakeAssignmentRepository(assignment);
        var access = AllowedTeacher();
        var handler = new ChangeAssignmentDescriptionHandler(repository, access.Object);
        var command = new ChangeAssignmentDescriptionCommand(assignment.Id, "Novi opis");

        await handler.HandleAsync(command);

        // Pravo se proverava baš za izvođenje predmeta kome zadatak pripada.
        access.Verify(x => x.CanEditAsync(assignment.CourseOfferingId), Times.Once);
    }

    [Test]
    public async Task HandleAsync_Published_EmptyDescription_IsRejectedAndNotSaved()
    {
        var assignment = CreatePublished();
        var repository = new FakeAssignmentRepository(assignment);
        var handler = new ChangeAssignmentDescriptionHandler(repository, AllowedTeacher().Object);
        var command = new ChangeAssignmentDescriptionCommand(assignment.Id, "");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Objavljen zadatak mora imati opis."));
        Assert.That(assignment.Description, Is.EqualTo("Opis zadatka"));
        Assert.That(repository.SaveCount, Is.EqualTo(0));
    }

    // Jednostavno lažno skladište za testove.
    // Čuva jedan zadatak i broji koliko puta je pozvano čuvanje.
    private class FakeAssignmentRepository : IAssignmentRepository
    {
        private readonly Assignment? _assignment;

        public int SaveCount { get; private set; }

        public FakeAssignmentRepository(Assignment? assignment)
        {
            _assignment = assignment;
        }

        public Task<Assignment?> FindAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            if (_assignment != null && _assignment.Id == assignmentId)
            {
                return Task.FromResult<Assignment?>(_assignment);
            }

            return Task.FromResult<Assignment?>(null);
        }

        public Task SaveAsync(Assignment assignment, CancellationToken cancellationToken = default)
        {
            SaveCount = SaveCount + 1;
            return Task.CompletedTask;
        }
    }
}
