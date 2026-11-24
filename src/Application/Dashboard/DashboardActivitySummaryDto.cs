namespace Application.Dashboard;

public sealed record DashboardActivitySummaryDto(
    int TotalCount,
    IReadOnlyDictionary<string, int> CountsByType,
    IReadOnlyList<DashboardActivityItemDto> Items);
