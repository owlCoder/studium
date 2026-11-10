using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-02: izmena naziva i opisa materijala.
/// </summary>
[TestFixture]
public sealed class UpdateMaterialDetailsHandlerTests
{
    [Test]
    public async Task HandleAsync_UpdatesExistingMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");
        await repository.AddAsync(material);
        var handler = new UpdateMaterialDetailsHandler(repository);

        var updated = await handler.HandleAsync(
            new UpdateMaterialDetailsCommand(material.Id, " Novo ", "Novi opis"));

        var stored = await repository.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(stored!.Name, Is.EqualTo("Novo"));
            Assert.That(stored.Description, Is.EqualTo("Novi opis"));
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsFalseForUnknownMaterial()
    {
        var handler = new UpdateMaterialDetailsHandler(new InMemoryMaterialRepository());

        var updated = await handler.HandleAsync(
            new UpdateMaterialDetailsCommand(Guid.NewGuid(), "Novo", null));

        Assert.That(updated, Is.False);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task HandleAsync_InvalidNameDoesNotChangeStoredMaterial(string name)
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Staro", "Stari opis");
        await repository.AddAsync(material);
        var handler = new UpdateMaterialDetailsHandler(repository);

        Assert.That(
            async () => await handler.HandleAsync(
                new UpdateMaterialDetailsCommand(material.Id, name, "Novi opis")),
            Throws.TypeOf<ArgumentException>());

        var stored = await repository.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Name, Is.EqualTo("Staro"));
            Assert.That(stored.Description, Is.EqualTo("Stari opis"));
        });
    }

    [Test]
    public async Task HandleAsync_ChangesOnlyRequestedMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var target = new Material(Guid.NewGuid(), Guid.NewGuid(), "Ciljni", null);
        var other = new Material(Guid.NewGuid(), Guid.NewGuid(), "Drugi", null);
        await repository.AddAsync(target);
        await repository.AddAsync(other);
        var handler = new UpdateMaterialDetailsHandler(repository);

        await handler.HandleAsync(new UpdateMaterialDetailsCommand(target.Id, "Izmenjen", null));

        var storedOther = await repository.GetByIdAsync(other.Id);
        Assert.That(storedOther!.Name, Is.EqualTo("Drugi"));
    }

    [Test]
    public void HandleAsync_RejectsNullCommand()
    {
        var handler = new UpdateMaterialDetailsHandler(new InMemoryMaterialRepository());

        Assert.That(
            async () => await handler.HandleAsync(null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
