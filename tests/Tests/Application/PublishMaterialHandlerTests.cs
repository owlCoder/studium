using Moq;
using NUnit.Framework;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;

namespace Tests.Application;

/// <summary>
/// ST-009, US-08: objava materijala.
/// </summary>
[TestFixture]
public sealed class PublishMaterialHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private static InMemoryOfferingStaffChecker StaffOnOffering() =>
        new([(TeacherId, OfferingId)]);

    private static Material Draft() =>
        new(Guid.NewGuid(), OfferingId, "Predavanje 1", "Uvod");

    private static Mock<IMaterialRepository> RepositoryWith(Material material)
    {
        var repository = new Mock<IMaterialRepository>();
        repository
            .Setup(x => x.GetByIdAsync(material.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(material);
        return repository;
    }

    private static void VerifyNothingSaved(Mock<IMaterialRepository> repository) =>
        repository.Verify(x => x.UpdateAsync(
            It.IsAny<Material>(),
            It.IsAny<CancellationToken>()), Times.Never);

    // --- Promena stanja iz nacrta u objavljen (tačka 1) ---

    [Test]
    public async Task HandleAsync_TeacherPublishesDraftAndSavesIt()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var handler = new PublishMaterialHandler(repository.Object, StaffOnOffering());

        var outcome = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.That(outcome, Is.EqualTo(PublishMaterialOutcome.Published));
        repository.Verify(x => x.UpdateAsync(
            It.Is<Material>(m =>
                m.Id == material.Id &&
                m.PublicationState == MaterialPublicationState.Published),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_PublishedStateIsStoredInRepository()
    {
        var repository = new InMemoryMaterialRepository();
        var material = Draft();
        await repository.AddAsync(material);
        var handler = new PublishMaterialHandler(repository, StaffOnOffering());

        await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        var saved = await repository.GetByIdAsync(material.Id);
        Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
    }

    [Test]
    public async Task HandleAsync_PublishesOnlyRequestedMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var target = Draft();
        var other = Draft();
        await repository.AddAsync(target);
        await repository.AddAsync(other);
        var handler = new PublishMaterialHandler(repository, StaffOnOffering());

        await handler.HandleAsync(new PublishMaterialCommand(TeacherId, target.Id));

        var savedOther = await repository.GetByIdAsync(other.Id);
        Assert.That(savedOther!.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
    }

    [Test]
    public async Task HandleAsync_UnknownMaterialReturnsNotFoundWithoutAskingStaffChecker()
    {
        var repository = new Mock<IMaterialRepository>();
        var checker = new Mock<IOfferingStaffChecker>();
        var handler = new PublishMaterialHandler(repository.Object, checker.Object);

        var outcome = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, Guid.NewGuid()));

        Assert.That(outcome, Is.EqualTo(PublishMaterialOutcome.MaterialNotFound));
        checker.Verify(x => x.IsStaffOnOfferingAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved(repository);
    }

    [Test]
    public async Task HandleAsync_AlreadyPublishedReturnsNotDraftAndSavesNothing()
    {
        var material = Draft();
        material.Publish();
        var repository = RepositoryWith(material);
        var handler = new PublishMaterialHandler(repository.Object, StaffOnOffering());

        var outcome = await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(PublishMaterialOutcome.NotDraft));
            Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
        });
        VerifyNothingSaved(repository);
    }

    // --- Provera nastavnog osoblja izvođenja, ST-005 (tačka 1) ---

    [Test]
    public void HandleAsync_RejectsUserWhoIsNotStaffAndSavesNothing()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var handler = new PublishMaterialHandler(repository.Object, StaffOnOffering());

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(Guid.NewGuid(), material.Id)));

        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_RejectsStaffOfAnotherOfferingAndSavesNothing()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var otherOfferingStaff = new InMemoryOfferingStaffChecker([(TeacherId, Guid.NewGuid())]);
        var handler = new PublishMaterialHandler(repository.Object, otherOfferingStaff);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id)));

        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_RejectsMissingUserWithoutAskingStaffChecker()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var checker = new Mock<IOfferingStaffChecker>();
        var handler = new PublishMaterialHandler(repository.Object, checker.Object);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(Guid.Empty, material.Id)));

        checker.Verify(x => x.IsStaffOnOfferingAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_ChecksAccessBeforeRevealingPublicationState()
    {
        var material = Draft();
        material.Publish();
        var repository = RepositoryWith(material);
        var handler = new PublishMaterialHandler(repository.Object, StaffOnOffering());

        // Korisnik van osoblja dobija odbijanje pristupa, ne podatak da materijal nije nacrt.
        Assert.ThrowsAsync<MaterialAccessDeniedException>(async () =>
            await handler.HandleAsync(new PublishMaterialCommand(Guid.NewGuid(), material.Id)));
    }

    [Test]
    public async Task HandleAsync_AsksStaffCheckerForRequestingUserAndMaterialOffering()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var checker = new Mock<IOfferingStaffChecker>();
        checker.Setup(x => x.IsStaffOnOfferingAsync(TeacherId, OfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new PublishMaterialHandler(repository.Object, checker.Object);

        await handler.HandleAsync(new PublishMaterialCommand(TeacherId, material.Id));

        checker.Verify(x => x.IsStaffOnOfferingAsync(
            TeacherId, OfferingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Ulazni podaci i zavisnosti ---

    [Test]
    public void HandleAsync_RejectsNullCommand()
    {
        var handler = new PublishMaterialHandler(new InMemoryMaterialRepository(), StaffOnOffering());

        Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.HandleAsync(null!));
    }

    [Test]
    public void Constructor_RejectsMissingRepository()
    {
        Assert.That(
            () => new PublishMaterialHandler(null!, StaffOnOffering()),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Constructor_RejectsMissingStaffChecker()
    {
        Assert.That(
            () => new PublishMaterialHandler(new InMemoryMaterialRepository(), null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
