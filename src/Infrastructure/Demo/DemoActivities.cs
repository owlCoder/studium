using Domain.Activities;
using Infrastructure.Activities;
using Infrastructure.Dashboard;

namespace Infrastructure.Demo;


public static class DemoActivities
{
    public static readonly Guid ErsOfferingId = Guid.Parse("33333333-0000-0000-0000-000000000001");
    public static readonly Guid PublishedAssignmentId = Guid.Parse("66666666-0000-0000-0000-000000000001");
    public static readonly Guid DraftAssignmentId = Guid.Parse("66666666-0000-0000-0000-000000000002");

    public static void Seed(InMemoryActivityStore store, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();

        store.AddActivity(new Activity(PublishedAssignmentId, ErsOfferingId, SupportedFeatures.Assignment,
            isPublished: true, deadline: now.AddDays(7)));
        store.AddActivity(new Activity(DraftAssignmentId, ErsOfferingId, SupportedFeatures.Assignment,
            isPublished: false, deadline: now.AddDays(14)));
    }
}
