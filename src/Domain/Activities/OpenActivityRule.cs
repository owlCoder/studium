using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Dashboard;

namespace Domain.Activities;

/// <summary>
/// US-7.3: jedino mesto u sistemu gde je definisano šta je "otvorena aktivnost".
/// Aktivnost je otvorena za studenta ako je:
///  1) vrsta podržana u registru podržanih funkcija (US-7.6, <see cref="FeatureRegistry"/>),
///  2) objavljena,
///  3) rok joj je u budućnosti (strogo posle sada; u trenutku roka više nije otvorena),
///  4) student nema važeću predaju za nju.
/// </summary>
public sealed class OpenActivityRule
{
    private readonly FeatureRegistry _registry;

    public OpenActivityRule(FeatureRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public bool IsOpen(
        Activity activity,
        Guid studentId,
        IEnumerable<ActivitySubmission> submissions,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(submissions);

        if (!_registry.IsActivitySupported(activity.ActivityType))
        {
            return false;
        }
        if (!activity.IsPublished)
        {
            return false;
        }
        if (activity.Deadline <= now)
        {
            return false;
        }

        var hasValidSubmission = submissions.Any(s =>
            s.ActivityId == activity.Id &&
            s.StudentId == studentId &&
            s.IsValid);

        return !hasValidSubmission;
    }

    public int CountOpen(
        IEnumerable<Activity> activities,
        Guid studentId,
        IEnumerable<ActivitySubmission> submissions,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(submissions);

        var submissionList = submissions as IReadOnlyCollection<ActivitySubmission> ?? submissions.ToArray();

        return activities
            .DistinctBy(a => a.Id)
            .Count(a => IsOpen(a, studentId, submissionList, now));
    }
}
