using Domain.Submissions;

namespace Application.Submissions;

/// <summary>
/// Obrađuje predaju individualnog rada (ST-012).
/// Student se uzima iz prijave (ICurrentUser), a vreme predaje iz sata (IClock),
/// nikad iz komande. Predaja je dozvoljena samo za objavljen zadatak
/// i najkasnije u trenutku isteka roka.
/// Pravilo za članstvo dodaje se u sledećem commitu.
/// </summary>
public class SubmitWorkHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IAssignmentReader _assignmentReader;
    private readonly ISubmissionRepository _repository;

    public SubmitWorkHandler(
        ICurrentUser currentUser,
        IClock clock,
        IAssignmentReader assignmentReader,
        ISubmissionRepository repository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _assignmentReader = assignmentReader;
        _repository = repository;
    }

    public async Task<SubmitWorkResult> HandleAsync(SubmitWorkCommand command)
    {
        Guid studentId = _currentUser.StudentId;
        DateTimeOffset submittedAt = _clock.UtcNow;

        // 1. Pročitamo zadatak na koji student predaje rad.
        AssignmentInfo? assignment = await _assignmentReader.GetByIdAsync(command.AssignmentId);

        // 2. Ako zadatak ne postoji, odbijamo predaju. Ništa se ne čuva.
        if (assignment == null)
        {
            return SubmitWorkResult.Fail("AssignmentNotFound");
        }

        // 3. Predaja je dozvoljena samo dok je zadatak objavljen (Published).
        //    Nacrt (Draft) i Zatvoren (Closed) se odbijaju. Ništa se ne čuva.
        if (assignment.State != AssignmentState.Published)
        {
            return SubmitWorkResult.Fail("AssignmentNotOpen");
        }

        // 4. Rok je uključiv, pa predaja tačno u trenutku roka prolazi.
        //    Sve posle roka se odbija. Ništa se ne čuva.
        if (submittedAt > assignment.Deadline)
        {
            return SubmitWorkResult.Fail("DeadlinePassed");
        }

        // 5. Sve provere su prošle, pravimo predaju.
        //    Ako je predaja prazna, Submission baca izuzetak i ništa se ne čuva.
        var submission = new Submission(
            Guid.NewGuid(),
            command.AssignmentId,
            studentId,
            command.Text,
            command.Attachments,
            submittedAt);

        // 6. Čuvamo predaju i vraćamo potvrdu studentu.
        await _repository.AddAsync(submission);

        var confirmation = new SubmissionConfirmation(submission.Id, submission.SubmittedAt);
        return SubmitWorkResult.Ok(confirmation);
    }
}
