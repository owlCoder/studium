using Application.CourseEnrollments;
using Infrastructure.CourseEnrollments;
using Infrastructure.Repositories;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class EnrollStudentConcurrencyTests
{
    [Test]
    public async Task HandleAsync_ParallelRequests_OnlyOneSucceeds()
    {
        var teacherId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var repo = new InMemoryCourseEnrollmentRepository();
        var access = new InMemoryOfferingAccessReader(new[] { (teacherId, executionId) });
        var handler = new EnrollStudentHandler(repo, access);

        var command = new EnrollStudentCommand(teacherId, executionId, studentId);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await handler.HandleAsync(command);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.That(results.Count(r => r), Is.EqualTo(1));
    }

    [Test]
    public void HandleAsync_TeacherNotAssignedToExecution_ThrowsAndDoesNotEnroll()
    {
        var repo = new InMemoryCourseEnrollmentRepository();
        var access = new InMemoryOfferingAccessReader(new[] { (Guid.NewGuid(), Guid.NewGuid()) });
        var handler = new EnrollStudentHandler(repo, access);

        var command = new EnrollStudentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(command));
    }
}
