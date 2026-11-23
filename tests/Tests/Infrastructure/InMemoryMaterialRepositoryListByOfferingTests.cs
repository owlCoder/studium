using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Infrastructure;

/// <summary>
/// ST-009, US-10: čitanje materijala jednog izvođenja iz memorijskog skladišta.
/// </summary>
[TestFixture]
public sealed class InMemoryMaterialRepositoryListByOfferingTests
{
    // Pozitivno ponašanje

    [Test]
    public async Task ListByOfferingAsync_ReturnsOnlyMaterialsOfThatOffering()
    {
        var repository = new InMemoryMaterialRepository();
        var offeringId = Guid.NewGuid();
        var first = new Material(Guid.NewGuid(), offeringId, "Predavanje 1", null);
        var second = new Material(Guid.NewGuid(), offeringId, "Predavanje 2", null);
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        await repository.AddAsync(new Material(Guid.NewGuid(), Guid.NewGuid(), "Tuđi predmet", null));

        var result = await repository.ListByOfferingAsync(offeringId);

        Assert.That(result.Select(m => m.Id), Is.EquivalentTo(new[] { first.Id, second.Id }));
    }

    [Test]
    public async Task ListByOfferingAsync_ReturnsMaterialsInAllPublicationStates()
    {
        var repository = new InMemoryMaterialRepository();
        var offeringId = Guid.NewGuid();
        var draft = new Material(Guid.NewGuid(), offeringId, "Nacrt", null);
        var published = new Material(Guid.NewGuid(), offeringId, "Objavljen", null);
        published.Publish();
        await repository.AddAsync(draft);
        await repository.AddAsync(published);

        var result = await repository.ListByOfferingAsync(offeringId);

        Assert.That(result.Select(m => m.Id), Is.EquivalentTo(new[] { draft.Id, published.Id }));
    }

    // Negativno ponašanje

    [Test]
    public void ListByOfferingAsync_CanceledTokenThrows()
    {
        var repository = new InMemoryMaterialRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            () => repository.ListByOfferingAsync(Guid.NewGuid(), cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    // Granično ponašanje

    [Test]
    public async Task ListByOfferingAsync_EmptyRepositoryReturnsEmptyList()
    {
        var repository = new InMemoryMaterialRepository();

        Assert.That(await repository.ListByOfferingAsync(Guid.NewGuid()), Is.Empty);
    }

    [Test]
    public async Task ListByOfferingAsync_OfferingWithoutMaterialsReturnsEmptyList()
    {
        var repository = new InMemoryMaterialRepository();
        await repository.AddAsync(new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje", null));

        Assert.That(await repository.ListByOfferingAsync(Guid.NewGuid()), Is.Empty);
    }

    [Test]
    public async Task ListByOfferingAsync_EmptyOfferingIdReturnsEmptyList()
    {
        var repository = new InMemoryMaterialRepository();
        await repository.AddAsync(new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje", null));

        Assert.That(await repository.ListByOfferingAsync(Guid.Empty), Is.Empty);
    }
}
