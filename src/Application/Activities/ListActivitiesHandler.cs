namespace Application.Activities;

/// <summary>
/// Vraća sve aktivnosti, bez filtriranja po predmetu; filtriranje i autorizacija
/// nisu deo ovog demonstracionog toka. Otkazivanje se vraća kao status,
/// bez bacanja izuzetka.
/// </summary>
public sealed class ListActivitiesHandler
{
    private readonly IActivityRepository _repository;

    public ListActivitiesHandler(IActivityRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListActivitiesResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var outcome = await _repository.ListAsync(cancellationToken);

        if (!outcome.IsSuccess)
        {
            return ListActivitiesResult.Canceled();
        }

        var activities = outcome.Activities
            .OrderBy(activity => activity.StartDate)
            .ThenBy(activity => activity.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(activity => activity.Id)
            .Select(ActivityListItem.From)
            .ToArray();

        return ListActivitiesResult.Success(activities);
    }
}
