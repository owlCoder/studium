namespace Application.Activities;

public sealed record CreateActivityCommand(
    string CourseSlug,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate);
