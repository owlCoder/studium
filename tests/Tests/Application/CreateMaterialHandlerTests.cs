using Moq;
using NUnit.Framework;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;

namespace Tests.Application;

[TestFixture]
public sealed class CreateMaterialHandlerTests
{
    [Test]
    public async Task HandleAsync_CreatesDraftMaterialOnOffering()
    {
        var offeringId = Guid.NewGuid();
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object);

        var id = await handler.HandleAsync(
            new CreateMaterialCommand(offeringId, " Predavanje 1 ", "Uvod"));

        Assert.That(id, Is.Not.EqualTo(Guid.Empty));
        repository.Verify(x => x.AddAsync(
            It.Is<Material>(m =>
                m.Id == id &&
                m.CourseOfferingId == offeringId &&
                m.Name == "Predavanje 1" &&
                m.Description == "Uvod" &&
                m.PublicationState == MaterialPublicationState.Draft),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_AllowsMissingDescription()
    {
        var repository = new InMemoryMaterialRepository();
        var handler = new CreateMaterialHandler(repository);

        var id = await handler.HandleAsync(
            new CreateMaterialCommand(Guid.NewGuid(), "Predavanje 1", null));

        var saved = await repository.GetByIdAsync(id);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Description, Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void HandleAsync_RejectsBlankNameAndSavesNothing(string? name)
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(Guid.NewGuid(), name!, "Opis")));

        repository.Verify(x => x.AddAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_RejectsEmptyOfferingAndSavesNothing()
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(Guid.Empty, "Predavanje 1", null)));

        repository.Verify(x => x.AddAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_CreatesDistinctMaterialsForSameName()
    {
        var repository = new InMemoryMaterialRepository();
        var handler = new CreateMaterialHandler(repository);
        var offeringId = Guid.NewGuid();

        var first = await handler.HandleAsync(new CreateMaterialCommand(offeringId, "Vezbe", null));
        var second = await handler.HandleAsync(new CreateMaterialCommand(offeringId, "Vezbe", null));

        Assert.That(first, Is.Not.EqualTo(second));
    }
}
