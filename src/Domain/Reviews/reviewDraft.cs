namespace Studium.Domain.Reviews;

public class ReviewDraft
{
    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public Guid TeacherId { get; private set; }
    public string Comment { get; private set; }
    public string Outcome { get; private set; } // npr. "Approved", "Rejected", "NeedsRevision"
    public bool IsPublished { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ReviewDraft(Guid id, Guid submissionId, Guid teacherId, string comment, string outcome)
    {
        Id = id;
        SubmissionId = submissionId;
        TeacherId = teacherId;
        Comment = comment;
        Outcome = outcome;
        IsPublished = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDraft(string newComment, string newOutcome)
    {
        if (IsPublished)
        {
            throw new InvalidOperationException("Nije moguće izmeniti već objavljeni pregled.");
        }

        Comment = newComment;
        Outcome = newOutcome;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        IsPublished = true;
        UpdatedAt = DateTime.UtcNow;
    }
}