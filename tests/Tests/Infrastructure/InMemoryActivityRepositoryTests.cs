using Domain.Activity;
using Infrastructure.Activities;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryActivityRepositoryTests
{
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Activity ActivityOf(string name) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, "Opis", Start, Start.AddDays(7));

    [Test]
    public async Task ListAsync_ReturnsEmptyCollectionForNewRepository()
    {
        var repository = new InMemoryActivityRepository();

        var result = await repository.ListAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task AddAsync_StoresActivityForLaterListing()
    {
        var repository = new InMemoryActivityRepository();
        var activity = ActivityOf("Timski projekat");

        await repository.AddAsync(activity);
        var result = await repository.ListAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(activity.Id));
        Assert.That(result[0].CourseId, Is.EqualTo(activity.CourseId));
    }

    [Test]
    public async Task AddAsync_RejectsActivityWithDuplicateIdWithoutChangingStoredState()
    {
        var repository = new InMemoryActivityRepository();
        var id = Guid.NewGuid();
        var original = new Activity(id, Guid.NewGuid(), "Originalna", "Opis", Start, Start.AddDays(7));
        var duplicate = new Activity(id, Guid.NewGuid(), "Duplikat", "Opis", Start, Start.AddDays(7));
        await repository.AddAsync(original);

        Assert.That(
            async () => await repository.AddAsync(duplicate),
            Throws.TypeOf<InvalidOperationException>());

        var result = await repository.ListAsync();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Originalna"));
    }

    [Test]
    public void AddAsync_RespectsCancellation()
    {
        var repository = new InMemoryActivityRepository();
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => repository.AddAsync(ActivityOf("Aktivnost"), source.Token),
            Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void ListAsync_RespectsCancellation()
    {
        var repository = new InMemoryActivityRepository();
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => repository.ListAsync(source.Token),
            Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public async Task AddAsync_StoresAllActivitiesAddedConcurrently()
    {
        var repository = new InMemoryActivityRepository();
        var activities = Enumerable.Range(0, 64)
            .Select(index => ActivityOf($"Aktivnost {index}"))
            .ToArray();

        await Task.WhenAll(activities.Select(activity => Task.Run(() => repository.AddAsync(activity))));

        var result = await repository.ListAsync();
        Assert.That(result, Has.Count.EqualTo(activities.Length));
    }
}
