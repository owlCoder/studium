namespace Application.Home;


public sealed record HomeOfferingDto(
    Guid OfferingId,
    string CourseName,
    string AcademicYear,
    string? Role,
    int? OpenActivities,
    IReadOnlyList<HomeLinkDto> Links);
