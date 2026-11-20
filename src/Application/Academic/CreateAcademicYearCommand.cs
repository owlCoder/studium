namespace Application.Academic;

public sealed record CreateAcademicYearCommand(
    string Label,
    DateTime StartsAt,
    DateTime EndsAt);
