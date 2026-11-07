using Domain.Submissions;

namespace Application.Submissions;

/// <summary>
/// Obrađuje predaju individualnog rada (ST-012).
/// Student se uzima iz prijave (ICurrentUser), a vreme predaje iz sata (IClock),
/// nikad iz komande. Pravila za stanje zadatka, rok i članstvo dodaju se u sledećim commitovima.
/// </summary>
public class SubmitWorkHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ISubmissionRepository _repository;

    public SubmitWorkHandler(ICurrentUser currentUser, IClock clock, ISubmissionRepository repository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _repository = repository;
    }

    public async Task<SubmissionConfirmation> HandleAsync(SubmitWorkCommand command)
    {
        Guid studentId = _currentUser.StudentId;
        DateTimeOffset submittedAt = _clock.UtcNow;

        // Ako je predaja prazna, Submission baca izuzetak i ništa se ne čuva.
        var submission = new Submission(
            Guid.NewGuid(),
            command.AssignmentId,
            studentId,
            command.Text,
            command.Attachments,
            submittedAt);

        await _repository.AddAsync(submission);

        return new SubmissionConfirmation(submission.Id, submission.SubmittedAt);
    }
}
