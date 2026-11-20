namespace Application.Academic;

public sealed record CreateCourseOfferingCommand(
    Guid CourseId,
    Guid AcademicYearId);
