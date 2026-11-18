using Domain.Dashboard;

namespace Infrastructure.Dashboard;

public static class SupportedFeatures
{
    public const string Announcement = "announcement";
    public const string Material = "material";
    public const string Assignment = "assignment";

    public static FeatureRegistry CreateRegistry()
    {
        return new FeatureRegistry(new[]
        {
            new SupportedActivityType(Announcement, FunctionalArea.Announcements,
                "/offerings/{offeringId}/announcements/{itemId}"),
            new SupportedActivityType(Material, FunctionalArea.Materials,
                "/offerings/{offeringId}/materials/{itemId}"),
            new SupportedActivityType(Assignment, FunctionalArea.Assignments,
                "/offerings/{offeringId}/assignments/{itemId}")
        });
    }
}
