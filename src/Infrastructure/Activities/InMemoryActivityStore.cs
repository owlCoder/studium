using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.Activities;
using Domain.Activities;

namespace Infrastructure.Activities;

/// <summary>
/// Memorijski adapter za US-7.3 dok moduli zadataka (ST-011) i predaja (ST-012) ne ponude svoje portove.
/// Implementira oba porta iz jednog skladišta, pa se predaja dodata preko <see cref="AddSubmission"/>
/// odmah vidi u sledećem brojanju.
/// </summary>
public sealed class InMemoryActivityStore : IActivityReader, IActivitySubmissionReader
{
    private readonly object _lock = new();
    private readonly List<Activity> _activities = new();
    private readonly List<ActivitySubmission> _submissions = new();

    public void AddActivity(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        lock (_lock)
        {
            _activities.Add(activity);
        }
    }

    public void AddSubmission(ActivitySubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);
        lock (_lock)
        {
            if (_activities.All(a => a.Id != submission.ActivityId))
            {
                throw new InvalidOperationException("Predaja se odnosi na nepostojeću aktivnost.");
            }
            _submissions.Add(submission);
        }
    }

    public Task<IReadOnlyList<Activity>> ListForOfferingAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            IReadOnlyList<Activity> result = _activities
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
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var activityIds = _activities
                .Where(a => a.OfferingId == offeringId)
                .Select(a => a.Id)
                .ToHashSet();

            IReadOnlyList<ActivitySubmission> result = _submissions
                .Where(s => s.StudentId == studentId && activityIds.Contains(s.ActivityId))
                .ToArray();
            return Task.FromResult(result);
        }
    }
}
