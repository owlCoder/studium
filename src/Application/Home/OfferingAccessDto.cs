namespace Application.Home;


public sealed record OfferingAccessDto(
    Guid OfferingId,
    string CourseName,
    string AcademicYear,
    string? Role);
