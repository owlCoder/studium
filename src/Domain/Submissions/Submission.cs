namespace Domain.Submissions;

/// <summary>
/// Predaja individualnog rada (ST-012).
/// Student predaje rad kao tekst, priloge ili oboje.
/// </summary>
public class Submission
{
    public Guid Id { get; }
    public Guid AssignmentId { get; }
    public Guid StudentId { get; }
    public string Text { get; }
    public List<Attachment> Attachments { get; }
    public DateTimeOffset SubmittedAt { get; }

    public Submission(Guid id, Guid assignmentId, Guid studentId, string? text,
        List<Attachment>? attachments, DateTimeOffset submittedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator predaje mora biti zadat.", nameof(id));
        if (assignmentId == Guid.Empty)
            throw new ArgumentException("Zadatak mora biti zadat.", nameof(assignmentId));
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));

        Id = id;
        AssignmentId = assignmentId;
        StudentId = studentId;

        if (text == null)
            Text = "";
        else
            Text = text.Trim();

        if (attachments == null)
            Attachments = new List<Attachment>();
        else
            Attachments = attachments;

        SubmittedAt = submittedAt;
    }
}
