using Domain.Activity;

namespace Application.Activities;

public enum ActivityListStatus
{
    Succeeded,
    Canceled
}

/// <summary>
/// Ishod čitanja aktivnosti. Otkazivanje se vraća kao status, ne kao izuzetak.
/// </summary>
public sealed record ActivityListResult(ActivityListStatus Status, IReadOnlyList<Activity> Activities)
{
    public bool IsSuccess => Status == ActivityListStatus.Succeeded;

    public static ActivityListResult Success(IReadOnlyList<Activity> activities) =>
        new(ActivityListStatus.Succeeded, activities);

    public static ActivityListResult Canceled() =>
        new(ActivityListStatus.Canceled, Array.Empty<Activity>());
}
