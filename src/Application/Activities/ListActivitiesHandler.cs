namespace Application.Activities;

/// <summary>
/// Vraća sve aktivnosti, bez filtriranja po predmetu; filtriranje i autorizacija
/// nisu deo ovog demonstracionog toka.
/// </summary>
public sealed class ListActivitiesHandler
{
    private readonly IActivityRepository _repository;

    public ListActivitiesHandler(IActivityRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ActivityListItem>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var activities = await _repository.ListAsync(cancellationToken);
        return activities
            .OrderBy(activity => activity.StartDate)
            .ThenBy(activity => activity.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(activity => activity.Id)
            .Select(ActivityListItem.From)
            .ToArray();
    }
}
