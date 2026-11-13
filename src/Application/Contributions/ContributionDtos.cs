namespace Application.Contributions;

public record EvidenceDto(int Id, string Kind, string Value, DateTime AddedAt);

public record ContributionRevisionDto(
    int RevisionNumber,
    string Description,
    DateTime CreatedAt,
    string ReviewerNote,
    IReadOnlyList<EvidenceDto> Evidence);

public record ContributionSummaryDto(
    int Id,
    int IdTask,
    int AuthorId,
    int PhaseId,
    string Status,
    DateTime CreatedAt,
    DateTime LastChangedAt);

public record ContributionDetailsDto(
    int Id,
    int IdTask,
    int AuthorId,
    int PhaseId,
    string Status,
    DateTime CreatedAt,
    DateTime LastChangedAt,
    ContributionRevisionDto CurrentRevision,
    IReadOnlyList<ContributionRevisionDto> History);
