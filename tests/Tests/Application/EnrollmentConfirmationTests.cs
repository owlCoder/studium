using Application.Enrollments;
using Domain.Enrollments;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class EnrollmentConfirmationTests
{
    [Test]
    public void From_CopiesAllEnrollmentData()
    {
        var enrollment = new Enrollment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero));

        var confirmation = EnrollmentConfirmation.From(enrollment);

        Assert.Multiple(() =>
        {
            Assert.That(confirmation.EnrollmentId, Is.EqualTo(enrollment.Id));
            Assert.That(confirmation.StudentId, Is.EqualTo(enrollment.StudentId));
            Assert.That(confirmation.CourseOfferingId, Is.EqualTo(enrollment.CourseOfferingId));
            Assert.That(confirmation.EnrolledAt, Is.EqualTo(enrollment.EnrolledAt));
            Assert.That(confirmation.Status, Is.EqualTo(EnrollmentStatus.Active));
        });
    }

    [Test]
    public void From_RejectsMissingEnrollment()
    {
        Assert.That(
            () => EnrollmentConfirmation.From(null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
