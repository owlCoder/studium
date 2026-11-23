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
///  3) primenljivi rok joj je u budućnosti (strogo posle sada; u trenutku roka više nije otvorena),
///  4) student nema važeću predaju za nju.
/// US-7.9: primenljivi rok je pojedinačni rok studenta (ST-015) ako postoji, inače zajednički rok.
/// </summary>
public sealed class OpenActivityRule
{
    private readonly FeatureRegistry _registry;

    public OpenActivityRule(FeatureRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>US-7.3: provera bez pojedinačnih rokova (važi samo zajednički rok).</summary>
    public bool IsOpen(
        Activity activity,
        Guid studentId,
        IEnumerable<ActivitySubmission> submissions,
        DateTimeOffset now) =>
        IsOpen(activity, studentId, submissions, Array.Empty<IndividualDeadline>(), now);

    /// <summary>US-7.9: provera sa pojedinačnim rokovima studenta (ST-015).</summary>
    public bool IsOpen(
        Activity activity,
        Guid studentId,
        IEnumerable<ActivitySubmission> submissions,
        IEnumerable<IndividualDeadline> individualDeadlines,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(submissions);
        ArgumentNullException.ThrowIfNull(individualDeadlines);

        if (!_registry.IsActivitySupported(activity.ActivityType))
        {
            return false;
        }
        if (!activity.IsPublished)
        {
            return false;
        }
        if (EffectiveDeadline(activity, studentId, individualDeadlines) <= now)
        {
            return false;
        }

        var hasValidSubmission = submissions.Any(s =>
            s.ActivityId == activity.Id &&
            s.StudentId == studentId &&
            s.IsValid);

        return !hasValidSubmission;
    }

    /// <summary>
    /// US-7.9: rok koji važi za ovog studenta. Pojedinačni rok ima prednost nad zajedničkim
    /// (pravilo iz ST-015); ako ih greškom ima više, važi najkasniji.
    /// </summary>
    public static DateTimeOffset EffectiveDeadline(
        Activity activity,
        Guid studentId,
        IEnumerable<IndividualDeadline> individualDeadlines)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(individualDeadlines);

        var own = individualDeadlines
            .Where(d => d.ActivityId == activity.Id && d.StudentId == studentId)
            .Select(d => (DateTimeOffset?)d.Deadline)
            .Max();

        return own ?? activity.Deadline;
    }

    public int CountOpen(
        IEnumerable<Activity> activities,
        Guid studentId,
        IEnumerable<ActivitySubmission> submissions,
        DateTimeOffset now) =>
        CountOpen(activities, studentId, submissions, Array.Empty<IndividualDeadline>(), now);

    public int CountOpen(
        IEnumerable<Activity> activities,
        Guid studentId,
        IEnumerable<ActivitySubmission> submissions,
        IEnumerable<IndividualDeadline> individualDeadlines,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(submissions);
        ArgumentNullException.ThrowIfNull(individualDeadlines);

        var submissionList = submissions as IReadOnlyCollection<ActivitySubmission> ?? submissions.ToArray();
        var deadlineList = individualDeadlines as IReadOnlyCollection<IndividualDeadline> ?? individualDeadlines.ToArray();

        return activities
            .DistinctBy(a => a.Id)
            .Count(a => IsOpen(a, studentId, submissionList, deadlineList, now));
    }
}
