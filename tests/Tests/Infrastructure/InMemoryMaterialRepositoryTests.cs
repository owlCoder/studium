using NUnit.Framework;
using Domain.Materials;
using Infrastructure.Materials;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryMaterialRepositoryTests
{
    [Test]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSameDraftMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", "Opis");

        await repository.AddAsync(material);
        var loaded = await repository.GetByIdAsync(material.Id);

        Assert.That(loaded, Is.SameAs(material));
        Assert.That(loaded!.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNullForUnknownId()
    {
        var repository = new InMemoryMaterialRepository();

        var loaded = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(loaded, Is.Null);
    }

    [Test]
    public async Task AddAsync_RejectsDuplicateIdAndKeepsOriginal()
    {
        var repository = new InMemoryMaterialRepository();
        var id = Guid.NewGuid();
        var original = new Material(id, Guid.NewGuid(), "Original", null);
        var duplicate = new Material(id, Guid.NewGuid(), "Duplikat", null);

        await repository.AddAsync(original);

        Assert.That(
            async () => await repository.AddAsync(duplicate),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That((await repository.GetByIdAsync(id))!.Name, Is.EqualTo("Original"));
    }

    [Test]
    public void AddAsync_RejectsNull()
    {
        var repository = new InMemoryMaterialRepository();

        Assert.That(
            async () => await repository.AddAsync(null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    // ST-009, US-02: čuvanje izmena

    [Test]
    public async Task UpdateAsync_StoresChangesOfExistingMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", null);
        await repository.AddAsync(material);

        material.UpdateDetails("Novo", "Novi opis");
        await repository.UpdateAsync(material);

        var loaded = await repository.GetByIdAsync(material.Id);
        Assert.That(loaded!.Name, Is.EqualTo("Novo"));
    }

    [Test]
    public async Task UpdateAsync_RejectsUnknownMaterialAndDoesNotAddIt()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Naziv", null);

        Assert.That(
            async () => await repository.UpdateAsync(material),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(await repository.GetByIdAsync(material.Id), Is.Null);
    }

    [Test]
    public void UpdateAsync_RejectsNull()
    {
        var repository = new InMemoryMaterialRepository();

        Assert.That(
            async () => await repository.UpdateAsync(null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
