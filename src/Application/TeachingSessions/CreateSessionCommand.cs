namespace Application.TeachingSessions;

public sealed record CreateSessionCommand(
    string Kind,
    Guid OfferingId,
    Guid? GroupId,
    DateTime StartsAt,
    DateTime EndsAt,
    string Location);
