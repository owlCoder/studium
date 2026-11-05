namespace Application;

using Domain;

public record SubmissionOverviewDto(
    Guid SubmissionId,
    string TaskTitle,
    string MemberName,
    string ContentUrl,
    DateTime SubmittedAt,
    SubmissionStatus Status
);

public record SubmissionStatusCountsDto(
    int TotalCount,
    int PendingCount,
    int AcceptedCount,
    int RejectedCount,
    int NeedsRevisionCount
);