using Domain.Enrollments;

namespace Application.Enrollments;

/// <summary>
/// Potvrda koju nastavnik dobija nakon uspešnog upisa studenta (ST-006).
/// </summary>
public sealed class EnrollmentConfirmation
{
    public Guid EnrollmentId { get; }
    public Guid StudentId { get; }
    public Guid CourseOfferingId { get; }
    public DateTimeOffset EnrolledAt { get; }
    public EnrollmentStatus Status { get; }

    public EnrollmentConfirmation(
        Guid enrollmentId,
        Guid studentId,
        Guid courseOfferingId,
        DateTimeOffset enrolledAt,
        EnrollmentStatus status)
    {
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        CourseOfferingId = courseOfferingId;
        EnrolledAt = enrolledAt;
        Status = status;
    }

    public static EnrollmentConfirmation From(Enrollment enrollment)
    {
        ArgumentNullException.ThrowIfNull(enrollment);

        return new EnrollmentConfirmation(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.CourseOfferingId,
            enrollment.EnrolledAt,
            enrollment.Status);
    }
}
