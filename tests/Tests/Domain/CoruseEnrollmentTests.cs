using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.CourseEnrollments;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class CourseEnrollmentTests
{
    [Test]
    public void Constructor_SetsActiveStatusAndEnrolledDate()
    {
        var executionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var enrollment = new CourseEnrollment(Guid.NewGuid(), executionId, studentId, DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(enrollment.Status, Is.EqualTo(EnrollmentStatus.Active));
            Assert.That(enrollment.CourseExecutionId, Is.EqualTo(executionId));
            Assert.That(enrollment.StudentId, Is.EqualTo(studentId));
        });
    }
}
