using Application.Activities;
using Domain.Activities;
using Domain.Dashboard;

namespace Application.Home;


public sealed class OpenTaskLinkReader : IOpenTaskLinkReader
{
    private readonly IActivityReader _activities;
    private readonly IActivitySubmissionReader _submissions;
    private readonly IIndividualDeadlineReader _individualDeadlines;
    private readonly FeatureRegistry _registry;
    private readonly OpenActivityRule _rule;
    private readonly TimeProvider _time;

    public OpenTaskLinkReader(
        IActivityReader activities,
        IActivitySubmissionReader submissions,
        IIndividualDeadlineReader individualDeadlines,
        FeatureRegistry registry,
        TimeProvider time)
    {
        _activities = activities ?? throw new ArgumentNullException(nameof(activities));
        _submissions = submissions ?? throw new ArgumentNullException(nameof(submissions));
        _individualDeadlines = individualDeadlines ?? throw new ArgumentNullException(nameof(individualDeadlines));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _rule = new OpenActivityRule(registry);
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    public async Task<IReadOnlyList<OpenTaskLinkDto>> ListForStudentAsync(
        Guid studentId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        }
        if (offeringId == Guid.Empty)
        {
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        }

        var activities = await _activities.ListForOfferingAsync(offeringId, cancellationToken);
        var submissions = await _submissions.ListForStudentAsync(studentId, offeringId, cancellationToken);
        var deadlines = await _individualDeadlines.ListIndividualDeadlinesAsync(studentId, offeringId, cancellationToken);
        var now = _time.GetUtcNow();

        var result = new List<OpenTaskLinkDto>();
        foreach (var activity in activities.Where(a => a.OfferingId == offeringId).DistinctBy(a => a.Id))
        {
            if (!_rule.IsOpen(activity, studentId, submissions, deadlines, now))
            {
                continue;
            }

            var href = _registry.TryBuildLink(activity.ActivityType, offeringId, activity.Id);
            if (href is null)
            {
                continue;
            }

            result.Add(new OpenTaskLinkDto(
                activity.Id,
                activity.ActivityType,
                OpenActivityRule.EffectiveDeadline(activity, studentId, deadlines),
                href));
        }

       
        return result.OrderBy(link => link.Deadline).ToArray();
    }
}
