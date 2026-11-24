namespace Application.Submissions;

/// <summary>
/// Potvrda koju student dobije kad je predaja uspešna (ST-012).
/// Sadrži broj predaje i tačno vreme kada je predaja primljena.
/// </summary>
public class SubmissionConfirmation
{
    public Guid SubmissionId { get; }
    public DateTimeOffset SubmittedAt { get; }

    public SubmissionConfirmation(Guid submissionId, DateTimeOffset submittedAt)
    {
        SubmissionId = submissionId;
        SubmittedAt = submittedAt;
    }
}
