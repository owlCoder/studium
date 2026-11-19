using Domain.Submissions;

namespace Application.Submissions;

/// <summary>
/// Čuva predaju individualnog rada (ST-012).
/// Za sada samo dodaje novu predaju, čitanje se dodaje kad bude potrebno.
/// </summary>
public interface ISubmissionRepository
{
    Task AddAsync(Submission submission);
}
