using Domain.Enrollments;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class EnrollmentTests
{
    [Test]
    public void Constructor_CreatesActiveEnrollment()
    {
        var enrollment = new Enrollment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(enrollment.Status, Is.EqualTo(EnrollmentStatus.Active));
            Assert.That(enrollment.IsActive, Is.True);
        });
    }

    [Test]
    public void Constructor_StoresEnrollmentDataAndDate()
    {
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var enrolledAt = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

        var enrollment = new Enrollment(id, studentId, offeringId, enrolledAt);

        Assert.Multiple(() =>
        {
            Assert.That(enrollment.Id, Is.EqualTo(id));
            Assert.That(enrollment.StudentId, Is.EqualTo(studentId));
            Assert.That(enrollment.CourseOfferingId, Is.EqualTo(offeringId));
            Assert.That(enrollment.EnrolledAt, Is.EqualTo(enrolledAt));
        });
    }

    [Test]
    public void Constructor_RejectsMissingId()
    {
        Assert.That(
            () => new Enrollment(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsMissingStudent()
    {
        Assert.That(
            () => new Enrollment(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsMissingCourseOffering()
    {
        Assert.That(
            () => new Enrollment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow),
            Throws.TypeOf<ArgumentException>());
    }
}
