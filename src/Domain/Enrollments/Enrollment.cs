namespace Domain.Enrollments;

/// <summary>
/// Članstvo studenta na konkretnom izvođenju predmeta (ST-006).
/// </summary>
public sealed class Enrollment
{
    public Guid Id { get; }
    public Guid StudentId { get; }
    public Guid CourseOfferingId { get; }
    public DateTimeOffset EnrolledAt { get; }
    public EnrollmentStatus Status { get; private set; }

    public bool IsActive => Status == EnrollmentStatus.Active;

    public Enrollment(
        Guid id,
        Guid studentId,
        Guid courseOfferingId,
        DateTimeOffset enrolledAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator članstva mora biti zadat.", nameof(id));
        if (studentId == Guid.Empty)
            throw new ArgumentException("Identifikator studenta mora biti zadat.", nameof(studentId));
        if (courseOfferingId == Guid.Empty)
            throw new ArgumentException(
                "Identifikator izvođenja predmeta mora biti zadat.",
                nameof(courseOfferingId));

        Id = id;
        StudentId = studentId;
        CourseOfferingId = courseOfferingId;
        EnrolledAt = enrolledAt;
        Status = EnrollmentStatus.Active;
    }
}
