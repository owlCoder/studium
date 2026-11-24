using Application.Activities;
using Application.Engagements;
using Domain.Activities;
using Domain.Dashboard;

namespace Application.Home;


public sealed class CheckContentAccessHandler
{
    private readonly ListMyOfferingsHandler _myOfferings;
    private readonly IActivityReader _activities;
    private readonly IIndividualDeadlineReader _individualDeadlines;
    private readonly FeatureRegistry _registry;

    public CheckContentAccessHandler(
        ListMyOfferingsHandler myOfferings,
        IActivityReader activities,
        IIndividualDeadlineReader individualDeadlines,
        FeatureRegistry registry)
    {
        _myOfferings = myOfferings ?? throw new ArgumentNullException(nameof(myOfferings));
        _activities = activities ?? throw new ArgumentNullException(nameof(activities));
        _individualDeadlines = individualDeadlines ?? throw new ArgumentNullException(nameof(individualDeadlines));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<OfferingAccessDto?> ForOfferingAsync(
        Guid userId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        RequireIds(userId, offeringId);

        var offerings = await _myOfferings.HandleAsync(userId, cancellationToken);

        var teaching = offerings.Teaching.FirstOrDefault(row => row.OfferingId == offeringId);
        if (teaching is not null)
        {
            return new OfferingAccessDto(teaching.OfferingId, teaching.CourseName, teaching.AcademicYear, teaching.RoleName);
        }

        var enrolled = offerings.Enrolled.FirstOrDefault(row => row.OfferingId == offeringId);
        return enrolled is null
            ? null
            : new OfferingAccessDto(enrolled.OfferingId, enrolled.CourseName, enrolled.AcademicYear, Role: null);
    }

    public async Task<AssignmentAccessDto?> ForAssignmentAsync(
        Guid userId,
        Guid offeringId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        RequireIds(userId, offeringId);
        if (activityId == Guid.Empty)
        {
            throw new ArgumentException("Zadatak mora biti zadat.", nameof(activityId));
        }

        var offering = await ForOfferingAsync(userId, offeringId, cancellationToken);
        if (offering is null)
        {
            return null;
        }

        var activities = await _activities.ListForOfferingAsync(offeringId, cancellationToken);
        var activity = activities.FirstOrDefault(a => a.Id == activityId && a.OfferingId == offeringId);
        if (activity is null || !_registry.IsActivitySupported(activity.ActivityType))
        {
            return null;
        }

        var isStaff = offering.Role is not null;
        if (isStaff)
        {
            return new AssignmentAccessDto(activity.Id, offeringId, activity.ActivityType, activity.IsPublished, activity.Deadline);
        }

        if (!activity.IsPublished)
        {
            return null;
        }

        var deadlines = await _individualDeadlines.ListIndividualDeadlinesAsync(userId, offeringId, cancellationToken);
        return new AssignmentAccessDto(
            activity.Id,
            offeringId,
            activity.ActivityType,
            IsPublished: true,
            OpenActivityRule.EffectiveDeadline(activity, userId, deadlines));
    }

  
    public async Task<AreaAccessDto?> ForAreaAsync(
        Guid userId,
        Guid offeringId,
        string area,
        CancellationToken cancellationToken = default)
    {
        RequireIds(userId, offeringId);

        if (string.IsNullOrWhiteSpace(area)
            || !area.All(char.IsLetter)
            || !Enum.TryParse<FunctionalArea>(area, ignoreCase: true, out var functionalArea)
            || !_registry.IsAreaAvailable(functionalArea))
        {
            return null;
        }

        var offering = await ForOfferingAsync(userId, offeringId, cancellationToken);
        return offering is null
            ? null
            : new AreaAccessDto(
                offering.OfferingId,
                offering.CourseName,
                offering.AcademicYear,
                functionalArea.ToString().ToLowerInvariant());
    }

    private static void RequireIds(Guid userId, Guid offeringId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        }
        if (offeringId == Guid.Empty)
        {
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        }
    }
}
