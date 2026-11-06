using Domain.Activity;

namespace Application.Activities;

public sealed record ActivityListItem(
    Guid Id,
    Guid CourseId,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate)
{
    public static ActivityListItem From(Activity activity) =>
        new(activity.Id,
            activity.CourseId,
            activity.Name,
            activity.Description,
            activity.StartDate,
            activity.EndDate);
}
