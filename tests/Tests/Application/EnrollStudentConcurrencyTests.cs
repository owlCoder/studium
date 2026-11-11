using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.CourseEnrollments;
using Infrastructure.CourseEnrollments;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class EnrollStudentConcurrencyTests
{
    [Test]
    public async Task HandleAsync_ParallelRequests_OnlyOneSucceeds()
    {
        var repo = new InMemoryCourseEnrollmentRepository();
        var checker = new AlwaysAllowTestChecker();
        var handler = new EnrollStudentHandler(repo, checker);

        var teacherId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

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

        
        Assert.That(results.Count(r => r == true), Is.EqualTo(1));
    }

    private sealed class AlwaysAllowTestChecker : ICourseExecutionPermissionChecker
    {
        public Task<bool> CanTeacherManageExecutionAsync(
            Guid teacherId,
            Guid courseExecutionId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
