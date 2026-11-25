using Application.Submissions;
using Domain.Submissions;
using Infrastructure.Submissions;
using NUnit.Framework;

namespace Tests.Infrastructure.Submissions;

[TestFixture]
public sealed class InMemorySubmissionRepositoryTests
{
    [Test]
    public async Task FindCurrentAsync_UsesBothAssignmentAndStudent()
    {
        var repository = new InMemorySubmissionRepository();
        var own = CreateSubmission(Guid.NewGuid(), Guid.NewGuid(), "Moj rad");
        var otherStudent = CreateSubmission(own.AssignmentId, Guid.NewGuid(), "Tuđi rad");
        var otherAssignment = CreateSubmission(Guid.NewGuid(), own.StudentId, "Drugi zadatak");

        await repository.AddAsync(own);
        await repository.AddAsync(otherStudent);
        await repository.AddAsync(otherAssignment);

        Assert.That(await repository.FindCurrentAsync(own.AssignmentId, own.StudentId), Is.SameAs(own));
        Assert.That(await repository.FindCurrentAsync(otherStudent.AssignmentId, otherStudent.StudentId),
            Is.SameAs(otherStudent));
        Assert.That(await repository.FindCurrentAsync(otherAssignment.AssignmentId, otherAssignment.StudentId),
            Is.SameAs(otherAssignment));
        Assert.That(await repository.FindCurrentAsync(otherAssignment.AssignmentId, otherStudent.StudentId), Is.Null);
    }

    [Test]
    public async Task AddAsync_PreservesCurrentSubmissionWhenPairAlreadyExists()
    {
        var repository = new InMemorySubmissionRepository();
        var original = CreateSubmission(Guid.NewGuid(), Guid.NewGuid(), "Prvi rad");
        var duplicate = CreateSubmission(original.AssignmentId, original.StudentId, "Drugi rad");
        await repository.AddAsync(original);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.AddAsync(duplicate));
        Assert.That(await repository.FindCurrentAsync(original.AssignmentId, original.StudentId), Is.SameAs(original));
    }

    [Test]
    public async Task GetOwnSubmissionHandler_ReadsActualRepositoryAndHidesOtherStudentsWork()
    {
        var repository = new InMemorySubmissionRepository();
        var studentId = Guid.NewGuid();
        var other = CreateSubmission(Guid.NewGuid(), Guid.NewGuid(), "Tuđi rad");
        await repository.AddAsync(other);
        var handler = new GetOwnSubmissionHandler(new SignedInStudent(studentId), repository);

        Assert.That(await handler.HandleAsync(other.AssignmentId), Is.Null);

        var own = CreateSubmission(other.AssignmentId, studentId, "Moj rad");
        await repository.AddAsync(own);

        Assert.That(await handler.HandleAsync(own.AssignmentId), Is.SameAs(own));
    }

    private static Submission CreateSubmission(Guid assignmentId, Guid studentId, string text)
    {
        return new Submission(Guid.NewGuid(), assignmentId, studentId, text,
            new List<Attachment> { new Attachment("rad.pdf", "application/pdf", 1024) },
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed class SignedInStudent : ICurrentUser
    {
        public Guid StudentId { get; }

        public SignedInStudent(Guid studentId)
        {
            StudentId = studentId;
        }
    }
}
