using Application.Activities;
using Domain.Activities;

namespace Tests.Acceptance;

internal sealed class MutableActivitySource : IActivityReader, IActivitySubmissionReader
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, Activity> _activities = new();
    private readonly List<ActivitySubmission> _submissions = new();

    public int ActivityReads { get; private set; }

    public void Add(Activity activity)
    {
        lock (_lock)
        {
            _activities[activity.Id] = activity;
        }
    }

    public void ChangeDeadline(Guid activityId, DateTimeOffset newDeadline)
    {
        lock (_lock)
        {
            var old = _activities[activityId];
            _activities[activityId] = new Activity(
                old.Id, old.OfferingId, old.ActivityType, old.IsPublished, newDeadline);
        }
    }

    public void AddSubmission(ActivitySubmission submission)
    {
        lock (_lock)
        {
            _submissions.Add(submission);
        }
    }

    public Task<IReadOnlyList<Activity>> ListForOfferingAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            ActivityReads++;
            IReadOnlyList<Activity> result = _activities.Values
                .Where(a => a.OfferingId == offeringId)
                .ToArray();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<ActivitySubmission>> ListForStudentAsync(
        Guid studentId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var ids = _activities.Values.Where(a => a.OfferingId == offeringId).Select(a => a.Id).ToHashSet();
            IReadOnlyList<ActivitySubmission> result = _submissions
                .Where(s => s.StudentId == studentId && ids.Contains(s.ActivityId))
                .ToArray();
            return Task.FromResult(result);
        }
    }
}
