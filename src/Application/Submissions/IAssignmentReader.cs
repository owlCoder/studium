namespace Application.Submissions;

/// <summary>
/// Čita podatke o zadatku na koji student predaje rad (ST-012).
/// Vraća null ako zadatak sa tim Id-jem ne postoji.
/// </summary>
public interface IAssignmentReader
{
    Task<AssignmentInfo?> GetByIdAsync(Guid assignmentId);
}
