namespace Application.Dashboard;

public sealed record DashboardActivity(string? ActivityType, Guid OfferingId, Guid ItemId, string Title);
