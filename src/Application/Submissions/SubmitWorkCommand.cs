using Domain.Submissions;

namespace Application.Submissions;

/// <summary>
/// Podaci koje student šalje kad predaje individualni rad (ST-012).
/// Namerno nema StudentId ni ime studenta, jer se student uzima iz prijave (ICurrentUser).
/// </summary>
public class SubmitWorkCommand
{
    public Guid AssignmentId { get; }
    public string? Text { get; }
    public List<Attachment>? Attachments { get; }

    public SubmitWorkCommand(Guid assignmentId, string? text, List<Attachment>? attachments)
    {
        AssignmentId = assignmentId;
        Text = text;
        Attachments = attachments;
    }
}
