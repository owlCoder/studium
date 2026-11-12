namespace Application.Activities;

public sealed record CreateActivityCommand(
    string ContextSlug,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate);
