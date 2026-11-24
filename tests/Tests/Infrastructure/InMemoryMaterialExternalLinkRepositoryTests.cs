using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Infrastructure;

/// <summary>
/// ST-009, US-03: memorijsko skladište spoljnih veza materijala.
/// </summary>
[TestFixture]
public sealed class InMemoryMaterialExternalLinkRepositoryTests
{
    private static MaterialExternalLink CreateLink(Guid materialId, string url = "https://example.org/video") =>
        new(Guid.NewGuid(), materialId, url);

    // Pozitivno ponašanje

    [Test]
    public async Task AddAsync_ThenListByMaterial_ReturnsLinkWithAddress()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var materialId = Guid.NewGuid();
        var link = CreateLink(materialId, "https://example.org/predavanje-1");

        await repository.AddAsync(link);
        var result = await repository.ListByMaterialAsync(materialId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(link.Id));
            Assert.That(result[0].MaterialId, Is.EqualTo(materialId));
            Assert.That(result[0].Url.AbsoluteUri, Is.EqualTo("https://example.org/predavanje-1"));
        });
    }

    [Test]
    public async Task ListByMaterial_ReturnsOnlyLinksOfThatMaterial()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var materialId = Guid.NewGuid();
        var first = CreateLink(materialId, "https://example.org/1");
        var second = CreateLink(materialId, "https://example.org/2");
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        await repository.AddAsync(CreateLink(Guid.NewGuid(), "https://example.org/tudja"));

        var result = await repository.ListByMaterialAsync(materialId);

        Assert.That(result.Select(l => l.Id), Is.EquivalentTo(new[] { first.Id, second.Id }));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsSavedLink()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var link = CreateLink(Guid.NewGuid());
        await repository.AddAsync(link);

        var found = await repository.GetByIdAsync(link.Id);

        Assert.That(found, Is.SameAs(link));
    }

    [Test]
    public async Task RemoveAsync_RemovesLinkAndReturnsTrue()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var materialId = Guid.NewGuid();
        var link = CreateLink(materialId);
        await repository.AddAsync(link);

        var removed = await repository.RemoveAsync(link.Id);

        Assert.Multiple(async () =>
        {
            Assert.That(removed, Is.True);
            Assert.That(await repository.GetByIdAsync(link.Id), Is.Null);
            Assert.That(await repository.ListByMaterialAsync(materialId), Is.Empty);
        });
    }

    [Test]
    public async Task RemoveAsync_LeavesOtherLinksOfMaterial()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var materialId = Guid.NewGuid();
        var removedLink = CreateLink(materialId, "https://example.org/1");
        var keptLink = CreateLink(materialId, "https://example.org/2");
        await repository.AddAsync(removedLink);
        await repository.AddAsync(keptLink);

        await repository.RemoveAsync(removedLink.Id);

        var result = await repository.ListByMaterialAsync(materialId);
        Assert.That(result.Select(l => l.Id), Is.EqualTo(new[] { keptLink.Id }));
    }

    // Negativno ponašanje

    [Test]
    public async Task AddAsync_RejectsDuplicateIdAndKeepsOriginal()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var materialId = Guid.NewGuid();
        var original = CreateLink(materialId, "https://example.org/original");
        await repository.AddAsync(original);
        var duplicate = new MaterialExternalLink(original.Id, materialId, "https://example.org/duplikat");

        Assert.That(() => repository.AddAsync(duplicate), Throws.InvalidOperationException);
        var saved = await repository.GetByIdAsync(original.Id);
        Assert.That(saved!.Url.AbsoluteUri, Is.EqualTo("https://example.org/original"));
    }

    [Test]
    public void AddAsync_RejectsNullLink()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();

        Assert.That(() => repository.AddAsync(null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task GetByIdAsync_UnknownIdReturnsNull()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();

        Assert.That(await repository.GetByIdAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task RemoveAsync_UnknownIdReturnsFalse()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();

        Assert.That(await repository.RemoveAsync(Guid.NewGuid()), Is.False);
    }

    [Test]
    public void AddAsync_CanceledTokenDoesNotSave()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var materialId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            () => repository.AddAsync(CreateLink(materialId), cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(repository.ListByMaterialAsync(materialId).Result, Is.Empty);
    }

    // Granično ponašanje

    [Test]
    public async Task ListByMaterial_MaterialWithoutLinksReturnsEmptyList()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        await repository.AddAsync(CreateLink(Guid.NewGuid()));

        Assert.That(await repository.ListByMaterialAsync(Guid.NewGuid()), Is.Empty);
    }

    [Test]
    public async Task RemoveAsync_SecondRemovalReturnsFalse()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var link = CreateLink(Guid.NewGuid());
        await repository.AddAsync(link);

        var first = await repository.RemoveAsync(link.Id);
        var second = await repository.RemoveAsync(link.Id);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        });
    }

    [Test]
    public async Task AddAsync_SameAddressTwiceIsStoredAsTwoLinks()
    {
        var repository = new InMemoryMaterialExternalLinkRepository();
        var materialId = Guid.NewGuid();
        await repository.AddAsync(CreateLink(materialId, "https://example.org/isto"));
        await repository.AddAsync(CreateLink(materialId, "https://example.org/isto"));

        Assert.That(await repository.ListByMaterialAsync(materialId), Has.Count.EqualTo(2));
    }
}
