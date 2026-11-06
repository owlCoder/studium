using System.Collections.Concurrent;
using Application.Activities;
using Domain.Activity;

namespace Infrastructure.Activities;

/// <summary>
/// Memorijski adapter samo za demonstraciju; podaci se gube pri ponovnom pokretanju
/// procesa. Trajni adapter treba da implementira isti port <see cref="IActivityRepository"/>.
/// </summary>
public sealed class InMemoryActivityRepository : IActivityRepository
{
    private readonly ConcurrentDictionary<Guid, Activity> _activities;

    public InMemoryActivityRepository(IEnumerable<Activity>? activities = null)
    {
        _activities = new ConcurrentDictionary<Guid, Activity>();
        foreach (var activity in activities ?? Array.Empty<Activity>())
        {
            if (!_activities.TryAdd(activity.Id, activity))
                throw new ArgumentException("Lista sadrži aktivnosti sa istim identifikatorom.", nameof(activities));
        }
    }

    public Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_activities.TryAdd(activity.Id, activity))
            throw new InvalidOperationException("Aktivnost sa istim identifikatorom već postoji.");

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Activity>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Activity> snapshot = _activities.Values.ToArray();
        return Task.FromResult(snapshot);
    }
}
