namespace Application.Activities;

public enum ListActivitiesStatus
{
    Succeeded,
    Canceled
}

/// <summary>
/// Rezultat prikaza aktivnosti. Otkazan zahtev se vraća kao status,
/// bez bacanja izuzetka.
/// </summary>
public sealed record ListActivitiesResult(
    ListActivitiesStatus Status,
    IReadOnlyList<ActivityListItem> Activities)
{
    public bool IsSuccess => Status == ListActivitiesStatus.Succeeded;

    public static ListActivitiesResult Success(IReadOnlyList<ActivityListItem> activities) =>
        new(ListActivitiesStatus.Succeeded, activities);

    public static ListActivitiesResult Canceled() =>
        new(ListActivitiesStatus.Canceled, Array.Empty<ActivityListItem>());
}
