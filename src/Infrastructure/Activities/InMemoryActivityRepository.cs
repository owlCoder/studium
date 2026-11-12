using System.Collections.Concurrent;
using Application.Activities;
using Domain.Activity;

namespace Infrastructure.Activities;

/// <summary>
/// Memorijski adapter samo za demonstraciju; podaci se gube pri ponovnom pokretanju
/// procesa. Trajni adapter treba da implementira isti port <see cref="IActivityRepository"/>.
/// Neuspesi se vraćaju kao status, bez izuzetaka.
/// </summary>
public sealed class InMemoryActivityRepository : IActivityRepository
{
    private readonly ConcurrentDictionary<Guid, Activity> _activities = new();

    public Task<ActivityAddResult> AddAsync(Activity? activity, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(ActivityAddResult.Canceled());
        }

        if (activity is null)
        {
            return Task.FromResult(ActivityAddResult.InvalidActivity());
        }

        var added = _activities.TryAdd(activity.Id, activity);

        return Task.FromResult(added
            ? ActivityAddResult.Added()
            : ActivityAddResult.DuplicateId());
    }

    public Task<ActivityListResult> ListAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(ActivityListResult.Canceled());
        }

        IReadOnlyList<Activity> snapshot = _activities.Values.ToArray();

        return Task.FromResult(ActivityListResult.Success(snapshot));
    }
}
