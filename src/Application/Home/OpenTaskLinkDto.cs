namespace Application.Home;


public sealed record OpenTaskLinkDto(
    Guid ActivityId,
    string ActivityType,
    DateTimeOffset Deadline,
    string Href);
