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
        var checker = new AllowAllCourseExecutionPermissionChecker();
        var handler = new EnrollStudentHandler(repo, checker);

        var teacherId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var command = new EnrollStudentCommand(teacherId, executionId, studentId);

        // Pokrećemo 10 paralelnih zahteva za upis u isto vreme
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

        // Tačno jedan zahtev mora da uspe, ostalih 9 moraju da padnu sa InvalidOperationException
        Assert.That(results.Count(r => r == true), Is.EqualTo(1));
    }
}
