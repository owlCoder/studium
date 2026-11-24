namespace Domain.Works;

/// <summary>
/// Rad koji je student predao na konkretnom izvođenju predmeta (ST-006).
/// </summary>
public sealed class StudentWork
{
    public Guid Id { get; }
    public Guid StudentId { get; }
    public Guid CourseOfferingId { get; }
    public string Title { get; }
    public DateTimeOffset SubmittedAt { get; }
    public WorkStatus Status { get; private set; }
    public int? Grade { get; private set; }

    public StudentWork(
        Guid id,
        Guid studentId,
        Guid courseOfferingId,
        string title,
        DateTimeOffset submittedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator rada mora biti zadat.", nameof(id));
        if (studentId == Guid.Empty)
            throw new ArgumentException("Identifikator studenta mora biti zadat.", nameof(studentId));
        if (courseOfferingId == Guid.Empty)
            throw new ArgumentException("Identifikator izvođenja mora biti zadat.", nameof(courseOfferingId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Naziv rada mora biti zadat.", nameof(title));

        Id = id;
        StudentId = studentId;
        CourseOfferingId = courseOfferingId;
        Title = title;
        SubmittedAt = submittedAt;
        Status = WorkStatus.Submitted;
    }

    public void SetGrade(int grade)   // ← preimenovano iz Grade
    {
        if (grade < 5 || grade > 10)
            throw new ArgumentOutOfRangeException(nameof(grade), "Ocena mora biti između 5 i 10.");
        Grade = grade;
        Status = WorkStatus.Graded;
    }

    public void Publish()
    {
        if (Status != WorkStatus.Graded)
            throw new InvalidOperationException("Rad mora biti ocenjen pre objavljivanja.");
        Status = WorkStatus.Published;
    }
}
