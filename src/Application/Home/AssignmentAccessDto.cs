namespace Application.Home;


public sealed record AssignmentAccessDto(
    Guid ActivityId,
    Guid OfferingId,
    string ActivityType,
    bool IsPublished,
    DateTimeOffset Deadline);
