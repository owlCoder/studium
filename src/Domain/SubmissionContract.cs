namespace Domain;

public record SubmissionContract(
    Guid SubmissionId,
    Guid TaskId,
    Guid MemberId,
    string ContentUrl,
    DateTime SubmittedAt,
    SubmissionStatus Status
);