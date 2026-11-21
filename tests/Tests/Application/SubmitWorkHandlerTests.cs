using Moq;
using NUnit.Framework;
using Application.Submissions;
using Domain.Submissions;

namespace Tests.Application;

[TestFixture]
public class SubmitWorkHandlerTests
{
    // Pomoćna metoda: student je aktivan član predmeta.
    // Koriste je svi testovi kojima članstvo nije ono što se proverava.
    private static Mock<IEnrollmentChecker> ActiveMember()
    {
        var enrollmentChecker = new Mock<IEnrollmentChecker>();
        enrollmentChecker.Setup(x => x.IsActiveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        return enrollmentChecker;
    }

    [Test]
    public async Task HandleAsync_SavesSubmissionWithStudentFromLogin()
    {
        var studentId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);
        var deadline = new DateTimeOffset(2026, 11, 30, 23, 59, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(studentId);

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        // Zadatak postoji i objavljen je, pa predaja treba da prođe.
        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(assignmentId))
            .ReturnsAsync(new AssignmentInfo(Guid.NewGuid(), AssignmentState.Published, deadline));

        var repository = new Mock<ISubmissionRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Submission>())).Returns(Task.CompletedTask);

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, ActiveMember().Object, repository.Object);
        var command = new SubmitWorkCommand(assignmentId, "Moj rad", null);

        var result = await handler.HandleAsync(command);
        var confirmation = result.Confirmation!;

        repository.Verify(x => x.AddAsync(
            It.Is<Submission>(s =>
                s.Id == confirmation.SubmissionId &&
                s.StudentId == studentId &&
                s.AssignmentId == assignmentId &&
                s.Text == "Moj rad")),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_ConfirmationHasTimeFromClock()
    {
        var now = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);
        var deadline = new DateTimeOffset(2026, 11, 30, 23, 59, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AssignmentInfo(Guid.NewGuid(), AssignmentState.Published, deadline));

        var repository = new Mock<ISubmissionRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Submission>())).Returns(Task.CompletedTask);

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, ActiveMember().Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "Moj rad", null);

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Confirmation!.SubmissionId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Confirmation.SubmittedAt, Is.EqualTo(now));
    }

    [Test]
    public async Task HandleAsync_EmptySubmission_IsRejectedAndNotSaved()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        // Zadatak je objavljen, da bi test stigao do provere prazne predaje.
        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AssignmentInfo(Guid.NewGuid(), AssignmentState.Published, DateTimeOffset.UtcNow.AddDays(7)));

        var repository = new Mock<ISubmissionRepository>();

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, ActiveMember().Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "   ", null);

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("EmptySubmission"));
        Assert.That(result.Confirmation, Is.Null);

        // Odbijena predaja ne sme ništa da sačuva (stavka 8).
        repository.Verify(x => x.AddAsync(It.IsAny<Submission>()), Times.Never);
    }

    // Isti test se pokreće dva puta: jednom za Nacrt, jednom za Zatvoren.
    [TestCase(AssignmentState.Draft)]
    [TestCase(AssignmentState.Closed)]
    public async Task HandleAsync_AssignmentNotPublished_IsRejectedAndNotSaved(AssignmentState state)
    {
        var now = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);
        var deadline = new DateTimeOffset(2026, 11, 30, 23, 59, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        // Zadatak postoji, ali nije objavljen.
        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AssignmentInfo(Guid.NewGuid(), state, deadline));

        var repository = new Mock<ISubmissionRepository>();

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, ActiveMember().Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "Moj rad", null);

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("AssignmentNotOpen"));
        Assert.That(result.Confirmation, Is.Null);

        // Odbijena predaja ne sme ništa da sačuva (stavka 8).
        repository.Verify(x => x.AddAsync(It.IsAny<Submission>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_AssignmentDoesNotExist_IsRejectedAndNotSaved()
    {
        var now = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        // Čitač ne nalazi zadatak i vraća null.
        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((AssignmentInfo?)null);

        var repository = new Mock<ISubmissionRepository>();

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, ActiveMember().Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "Moj rad", null);

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("AssignmentNotFound"));

        // Odbijena predaja ne sme ništa da sačuva (stavka 8).
        repository.Verify(x => x.AddAsync(It.IsAny<Submission>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_SubmittedExactlyAtDeadline_IsAccepted()
    {
        var deadline = new DateTimeOffset(2026, 11, 30, 23, 59, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        // Student predaje tačno u trenutku roka.
        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(deadline);

        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AssignmentInfo(Guid.NewGuid(), AssignmentState.Published, deadline));

        var repository = new Mock<ISubmissionRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Submission>())).Returns(Task.CompletedTask);

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, ActiveMember().Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "Moj rad", null);

        var result = await handler.HandleAsync(command);

        // Rok je uključiv, pa predaja mora da prođe i da se sačuva.
        Assert.That(result.Success, Is.True);
        Assert.That(result.Confirmation!.SubmittedAt, Is.EqualTo(deadline));
        repository.Verify(x => x.AddAsync(It.IsAny<Submission>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_SubmittedAfterDeadline_IsRejectedAndNotSaved()
    {
        var deadline = new DateTimeOffset(2026, 11, 30, 23, 59, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(Guid.NewGuid());

        // Student predaje jedan tick (najmanji korak vremena) posle roka.
        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(deadline.AddTicks(1));

        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AssignmentInfo(Guid.NewGuid(), AssignmentState.Published, deadline));

        var repository = new Mock<ISubmissionRepository>();

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, ActiveMember().Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "Moj rad", null);

        var result = await handler.HandleAsync(command);

        // Predaja je odbijena i ništa nije sačuvano (stavka 8).
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("DeadlinePassed"));
        Assert.That(result.Confirmation, Is.Null);
        repository.Verify(x => x.AddAsync(It.IsAny<Submission>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_StudentNotActiveMember_IsRejectedAndNotSaved()
    {
        var studentId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);
        var deadline = new DateTimeOffset(2026, 11, 30, 23, 59, 0, TimeSpan.Zero);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.StudentId).Returns(studentId);

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now);

        var assignmentReader = new Mock<IAssignmentReader>();
        assignmentReader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AssignmentInfo(offeringId, AssignmentState.Published, deadline));

        // Student nije upisan na predmet ili mu je članstvo prekinuto.
        var enrollmentChecker = new Mock<IEnrollmentChecker>();
        enrollmentChecker.Setup(x => x.IsActiveMemberAsync(studentId, offeringId))
            .ReturnsAsync(false);

        var repository = new Mock<ISubmissionRepository>();

        var handler = new SubmitWorkHandler(currentUser.Object, clock.Object, assignmentReader.Object, enrollmentChecker.Object, repository.Object);
        var command = new SubmitWorkCommand(Guid.NewGuid(), "Moj rad", null);

        var result = await handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("NotActiveMember"));
        Assert.That(result.Confirmation, Is.Null);

        // Odbijena predaja ne sme ništa da sačuva (stavka 8).
        repository.Verify(x => x.AddAsync(It.IsAny<Submission>()), Times.Never);
    }
}
