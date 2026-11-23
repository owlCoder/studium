using Domain.Submissions;

namespace Application.Submissions;

/// <summary>
/// Čuva i čita predaje individualnog rada (ST-012).
/// </summary>
public interface ISubmissionRepository
{
    Task AddAsync(Submission submission);

    /// <summary>
    /// Vraća predaju koja je trenutno važeća za ovaj zadatak i studenta, ili null
    /// ako takva predaja ne postoji. Posle zamene vraća novu važeću predaju.
    /// </summary>
    Task<Submission?> FindCurrentAsync(Guid assignmentId, Guid studentId);
}
