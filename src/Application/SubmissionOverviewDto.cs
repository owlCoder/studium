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

// ST-013: svi brojači se računaju iz istih listi pregleda (TaskSubmissionReviewClassifier).
// TotalCount = broj važećih predaja studenata članova (Unreviewed + Reviewed).
public record SubmissionStatusCountsDto(
    int WithoutSubmissionCount,
    int UnreviewedCount,
    int ReviewedCount,
    int TotalCount,
    int PendingCount,
    int AcceptedCount,
    int RejectedCount,
    int NeedsRevisionCount
);