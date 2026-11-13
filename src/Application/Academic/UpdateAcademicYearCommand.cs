namespace Application.Academic;

public sealed record UpdateAcademicYearCommand(
    Guid Id,
    string Label,
    DateTime StartsAt,
    DateTime EndsAt);
