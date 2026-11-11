using Domain.Dashboard;

namespace Application.Dashboard;

public sealed class FilterSupportedActivitiesHandler
{
    private readonly FeatureRegistry _registry;

    public FilterSupportedActivitiesHandler(FeatureRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public DashboardActivitySummaryDto Handle(IEnumerable<DashboardActivity> activities)
    {
        ArgumentNullException.ThrowIfNull(activities);

        var items = new List<DashboardActivityItemDto>();
        foreach (var activity in activities)
        {
            if (activity is null)
            {
                continue;
            }

            var link = _registry.TryBuildLink(activity.ActivityType, activity.OfferingId, activity.ItemId);
            if (link is null)
            {
                continue;
            }

            items.Add(new DashboardActivityItemDto(
                activity.ActivityType!.Trim().ToLowerInvariant(),
                activity.Title,
                link));
        }

        var counts = items
            .GroupBy(item => item.ActivityType, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new DashboardActivitySummaryDto(items.Count, counts, items);
    }
}
