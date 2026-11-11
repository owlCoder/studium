using Moq;
using NUnit.Framework;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;

namespace Tests.Application;

[TestFixture]
public sealed class CreateMaterialHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private static InMemoryOfferingStaffChecker StaffOnOffering() =>
        new([(TeacherId, OfferingId)]);

    // --- Kreiranje i validacija (tačka 2) ---

    [Test]
    public async Task HandleAsync_CreatesDraftMaterialOnOffering()
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object, StaffOnOffering());

        var id = await handler.HandleAsync(
            new CreateMaterialCommand(TeacherId, OfferingId, " Predavanje 1 ", "Uvod"));

        Assert.That(id, Is.Not.EqualTo(Guid.Empty));
        repository.Verify(x => x.AddAsync(
            It.Is<Material>(m =>
                m.Id == id &&
                m.CourseOfferingId == OfferingId &&
                m.Name == "Predavanje 1" &&
                m.Description == "Uvod" &&
                m.PublicationState == MaterialPublicationState.Draft),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_AllowsMissingDescription()
    {
        var repository = new InMemoryMaterialRepository();
        var handler = new CreateMaterialHandler(repository, StaffOnOffering());

        var id = await handler.HandleAsync(
            new CreateMaterialCommand(TeacherId, OfferingId, "Predavanje 1", null));

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
        var handler = new CreateMaterialHandler(repository.Object, StaffOnOffering());

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(TeacherId, OfferingId, name!, "Opis")));

        repository.Verify(x => x.AddAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_CreatesDistinctMaterialsForSameName()
    {
        var repository = new InMemoryMaterialRepository();
        var handler = new CreateMaterialHandler(repository, StaffOnOffering());

        var first = await handler.HandleAsync(new CreateMaterialCommand(TeacherId, OfferingId, "Vezbe", null));
        var second = await handler.HandleAsync(new CreateMaterialCommand(TeacherId, OfferingId, "Vezbe", null));

        Assert.That(first, Is.Not.EqualTo(second));
    }

    // --- Provera nastavnog osoblja izvođenja, ST-005 (tačka 3) ---

    [Test]
    public void HandleAsync_RejectsUserWhoIsNotStaffAndSavesNothing()
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object, StaffOnOffering());

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(Guid.NewGuid(), OfferingId, "Predavanje 1", null)));

        repository.Verify(x => x.AddAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_RejectsStaffOfAnotherOfferingAndSavesNothing()
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object, StaffOnOffering());
        var otherOffering = Guid.NewGuid();

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(TeacherId, otherOffering, "Predavanje 1", null)));

        repository.Verify(x => x.AddAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_RejectsMissingUserWithoutAskingStaffChecker()
    {
        var repository = new Mock<IMaterialRepository>();
        var checker = new Mock<IOfferingStaffChecker>();
        var handler = new CreateMaterialHandler(repository.Object, checker.Object);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(Guid.Empty, OfferingId, "Predavanje 1", null)));

        checker.Verify(x => x.IsStaffOnOfferingAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_RejectsEmptyOfferingAndSavesNothing()
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object, StaffOnOffering());

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(TeacherId, Guid.Empty, "Predavanje 1", null)));

        repository.Verify(x => x.AddAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_ChecksAccessBeforeValidatingName()
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new CreateMaterialHandler(repository.Object, StaffOnOffering());

        // Korisnik van osoblja dobija odbijanje pristupa, ne grešku validacije.
        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(
                new CreateMaterialCommand(Guid.NewGuid(), OfferingId, "   ", null)));
    }

    [Test]
    public async Task HandleAsync_AsksStaffCheckerForRequestingUserAndOffering()
    {
        var repository = new Mock<IMaterialRepository>();
        var checker = new Mock<IOfferingStaffChecker>();
        checker.Setup(x => x.IsStaffOnOfferingAsync(TeacherId, OfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CreateMaterialHandler(repository.Object, checker.Object);

        await handler.HandleAsync(new CreateMaterialCommand(TeacherId, OfferingId, "Predavanje 1", null));

        checker.Verify(x => x.IsStaffOnOfferingAsync(
            TeacherId, OfferingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Constructor_RejectsMissingStaffChecker()
    {
        Assert.That(
            () => new CreateMaterialHandler(new InMemoryMaterialRepository(), null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
