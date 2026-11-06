namespace Application.Activities;

public sealed record CreateActivityCommand(
    Guid CourseId,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate);
