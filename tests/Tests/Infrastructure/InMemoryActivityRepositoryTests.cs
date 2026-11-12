using Application.Activities;
using Domain.Activity;
using Infrastructure.Activities;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryActivityRepositoryTests
{
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Activity ActivityOf(string name) =>
        Activity.TryCreate(Guid.NewGuid(), Guid.NewGuid(), name, "Opis", Start, Start.AddDays(7)).Activity!;

    [Test]
    public async Task ListAsync_ReturnsEmptyCollectionForNewRepository()
    {
        var repository = new InMemoryActivityRepository();

        var result = await repository.ListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Activities, Is.Empty);
        });
    }

    [Test]
    public async Task AddAsync_StoresActivityForLaterListing()
    {
        var repository = new InMemoryActivityRepository();
        var activity = ActivityOf("Timski projekat");

        var outcome = await repository.AddAsync(activity);
        var result = await repository.ListAsync();

        Assert.That(outcome.Status, Is.EqualTo(ActivityAddStatus.Added));
        Assert.That(result.Activities, Has.Count.EqualTo(1));
        Assert.That(result.Activities[0].Id, Is.EqualTo(activity.Id));
        Assert.That(result.Activities[0].ContextId, Is.EqualTo(activity.ContextId));
    }

    [Test]
    public async Task AddAsync_RejectsActivityWithDuplicateIdWithoutChangingStoredState()
    {
        var repository = new InMemoryActivityRepository();
        var id = Guid.NewGuid();
        var original = Activity.TryCreate(id, Guid.NewGuid(), "Originalna", "Opis", Start, Start.AddDays(7)).Activity!;
        var duplicate = Activity.TryCreate(id, Guid.NewGuid(), "Duplikat", "Opis", Start, Start.AddDays(7)).Activity!;
        await repository.AddAsync(original);

        var outcome = await repository.AddAsync(duplicate);

        Assert.That(outcome.Status, Is.EqualTo(ActivityAddStatus.DuplicateId));
        var result = await repository.ListAsync();
        Assert.That(result.Activities, Has.Count.EqualTo(1));
        Assert.That(result.Activities[0].Name, Is.EqualTo("Originalna"));
    }

    [Test]
    public async Task AddAsync_ReturnsInvalidActivityWhenActivityIsNull()
    {
        var repository = new InMemoryActivityRepository();

        var outcome = await repository.AddAsync(null);

        Assert.That(outcome.Status, Is.EqualTo(ActivityAddStatus.InvalidActivity));
        Assert.That((await repository.ListAsync()).Activities, Is.Empty);
    }

    [Test]
    public async Task AddAsync_ReturnsCanceledInsteadOfThrowing()
    {
        var repository = new InMemoryActivityRepository();
        using var source = new CancellationTokenSource();
        source.Cancel();

        var outcome = await repository.AddAsync(ActivityOf("Aktivnost"), source.Token);

        Assert.That(outcome.Status, Is.EqualTo(ActivityAddStatus.Canceled));
    }

    [Test]
    public async Task ListAsync_ReturnsCanceledInsteadOfThrowing()
    {
        var repository = new InMemoryActivityRepository();
        using var source = new CancellationTokenSource();
        source.Cancel();

        var result = await repository.ListAsync(source.Token);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ActivityListStatus.Canceled));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Activities, Is.Empty);
        });
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
        Assert.That(result.Activities, Has.Count.EqualTo(activities.Length));
    }
}
