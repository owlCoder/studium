namespace Application.Home;


public sealed record AreaAccessDto(
    Guid OfferingId,
    string CourseName,
    string AcademicYear,
    string Area);
