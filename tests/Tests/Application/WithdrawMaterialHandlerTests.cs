using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-09 (tačka 1): nastavnik povlači objavljen materijal.
/// </summary>
[TestFixture]
public sealed class WithdrawMaterialHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private static InMemoryOfferingStaffChecker StaffOnOffering() =>
        new([(TeacherId, OfferingId)]);

    private static Material Draft(Guid? offeringId = null) =>
        new(Guid.NewGuid(), offeringId ?? OfferingId, "Predavanje 1", "Uvod");

    private static Material Published(Guid? offeringId = null)
    {
        var material = Draft(offeringId);
        material.Publish();
        return material;
    }

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

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_TeacherWithdrawsPublishedMaterialAndSavesIt()
    {
        var material = Published();
        var repository = RepositoryWith(material);
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        var outcome = await handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, material.Id));

        Assert.That(outcome, Is.EqualTo(WithdrawMaterialOutcome.Withdrawn));
        repository.Verify(x => x.UpdateAsync(
            It.Is<Material>(m =>
                m.Id == material.Id &&
                m.PublicationState == MaterialPublicationState.Withdrawn),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WithdrawnMaterialStaysInRepositoryAndIsNotDeleted()
    {
        var repository = new InMemoryMaterialRepository();
        var material = Published();
        await repository.AddAsync(material);
        var handler = new WithdrawMaterialHandler(repository, StaffOnOffering());

        await handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, material.Id));

        var saved = await repository.GetByIdAsync(material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.PublicationState, Is.EqualTo(MaterialPublicationState.Withdrawn));
            Assert.That(saved.Name, Is.EqualTo("Predavanje 1"));
        });
    }

    [Test]
    public async Task HandleAsync_WithdrawsOnlyRequestedMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var target = Published();
        var other = Published();
        await repository.AddAsync(target);
        await repository.AddAsync(other);
        var handler = new WithdrawMaterialHandler(repository, StaffOnOffering());

        await handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, target.Id));

        var otherSaved = await repository.GetByIdAsync(other.Id);
        Assert.That(otherSaved!.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
    }

    // Negativno ponašanje: pristup

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsNotFoundAndSavesNothing()
    {
        var repository = new Mock<IMaterialRepository>();
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        var outcome = await handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, Guid.NewGuid()));

        Assert.That(outcome, Is.EqualTo(WithdrawMaterialOutcome.MaterialNotFound));
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_UserNotStaffOfOffering_IsDeniedAndStateStaysPublished()
    {
        var material = Published();
        var repository = RepositoryWith(material);
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        Assert.That(
            () => handler.HandleAsync(new WithdrawMaterialCommand(OtherUserId, material.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Published));
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_StaffOfAnotherOffering_IsDenied()
    {
        var material = Published(offeringId: Guid.NewGuid());
        var repository = RepositoryWith(material);
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        Assert.That(
            () => handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, material.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_EmptyUserId_IsDenied()
    {
        var material = Published();
        var repository = RepositoryWith(material);
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        Assert.That(
            () => handler.HandleAsync(new WithdrawMaterialCommand(Guid.Empty, material.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        VerifyNothingSaved(repository);
    }

    [Test]
    public void HandleAsync_NotStaffAndDraft_IsDeniedBeforeStateIsRevealed()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        Assert.That(
            () => handler.HandleAsync(new WithdrawMaterialCommand(OtherUserId, material.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    // Negativno ponašanje: stanje

    [Test]
    public async Task HandleAsync_Draft_ReturnsNotPublishedAndStateStaysDraft()
    {
        var material = Draft();
        var repository = RepositoryWith(material);
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        var outcome = await handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, material.Id));

        Assert.That(outcome, Is.EqualTo(WithdrawMaterialOutcome.NotPublished));
        Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
        VerifyNothingSaved(repository);
    }

    // Granično ponašanje

    [Test]
    public async Task HandleAsync_WithdrawnTwice_SecondReturnsNotPublishedAndSavesOnce()
    {
        var material = Published();
        var repository = RepositoryWith(material);
        var handler = new WithdrawMaterialHandler(repository.Object, StaffOnOffering());

        var first = await handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, material.Id));
        var second = await handler.HandleAsync(new WithdrawMaterialCommand(TeacherId, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(WithdrawMaterialOutcome.Withdrawn));
            Assert.That(second, Is.EqualTo(WithdrawMaterialOutcome.NotPublished));
            Assert.That(material.PublicationState, Is.EqualTo(MaterialPublicationState.Withdrawn));
        });
        repository.Verify(x => x.UpdateAsync(
            It.IsAny<Material>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleAsync_NullCommand_Throws()
    {
        var handler = new WithdrawMaterialHandler(new Mock<IMaterialRepository>().Object, StaffOnOffering());

        Assert.That(() => handler.HandleAsync(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Constructor_NullDependencies_Throw()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new WithdrawMaterialHandler(null!, StaffOnOffering()),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new WithdrawMaterialHandler(new Mock<IMaterialRepository>().Object, null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
