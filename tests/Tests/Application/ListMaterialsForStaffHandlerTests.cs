using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-08, tačka 3: lista materijala izvođenja sa stanjem objave za nastavnika.
/// </summary>
[TestFixture]
public sealed class ListMaterialsForStaffHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;

    [SetUp]
    public void SetUp() => _materials = new InMemoryMaterialRepository();

    private ListMaterialsForStaffHandler CreateHandler(IOfferingStaffChecker? checker = null) =>
        new(_materials, checker ?? new InMemoryOfferingStaffChecker([(TeacherId, OfferingId)]));

    private async Task<Material> AddAsync(string name, bool publish = false, Guid? offeringId = null)
    {
        var material = new Material(Guid.NewGuid(), offeringId ?? OfferingId, name, null);
        if (publish)
            material.Publish();
        await _materials.AddAsync(material);
        return material;
    }

    // --- Pozitivno ponašanje ---

    [Test]
    public async Task HandleAsync_Staff_SeesDraftAndPublishedMaterialsWithState()
    {
        var draft = await AddAsync("Nacrt predavanja");
        var published = await AddAsync("Predavanje 1", publish: true);

        var result = await CreateHandler().HandleAsync(new ListMaterialsForStaffQuery(TeacherId, OfferingId));

        Assert.That(result, Is.EqualTo(new[]
        {
            new StaffMaterialListItem(draft.Id, "Nacrt predavanja", MaterialPublicationState.Draft),
            new StaffMaterialListItem(published.Id, "Predavanje 1", MaterialPublicationState.Published)
        }));
    }

    [Test]
    public async Task HandleAsync_Staff_SeesWithdrawnMaterial()
    {
        var withdrawn = await AddAsync("Stara skripta", publish: true);
        withdrawn.Withdraw();
        await _materials.UpdateAsync(withdrawn);

        var result = await CreateHandler().HandleAsync(new ListMaterialsForStaffQuery(TeacherId, OfferingId));

        Assert.That(result, Is.EqualTo(new[]
        {
            new StaffMaterialListItem(withdrawn.Id, "Stara skripta", MaterialPublicationState.Withdrawn)
        }));
    }

    [Test]
    public async Task HandleAsync_OrdersByNameIgnoringCase()
    {
        await AddAsync("vežbe");
        await AddAsync("Animacije");
        await AddAsync("Predavanje");

        var result = await CreateHandler().HandleAsync(new ListMaterialsForStaffQuery(TeacherId, OfferingId));

        Assert.That(result.Select(m => m.Name), Is.EqualTo(new[] { "Animacije", "Predavanje", "vežbe" }));
    }

    [Test]
    public async Task HandleAsync_DoesNotShowMaterialsOfAnotherOffering()
    {
        await AddAsync("Naš materijal");
        await AddAsync("Tuđi materijal", offeringId: Guid.NewGuid());

        var result = await CreateHandler().HandleAsync(new ListMaterialsForStaffQuery(TeacherId, OfferingId));

        Assert.That(result.Select(m => m.Name), Is.EqualTo(new[] { "Naš materijal" }));
    }

    // --- Granično ponašanje ---

    [Test]
    public async Task HandleAsync_OfferingWithoutMaterials_ReturnsEmptyList()
    {
        var result = await CreateHandler().HandleAsync(new ListMaterialsForStaffQuery(TeacherId, OfferingId));

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_AsksStaffCheckerWithRequestingUserAndOffering()
    {
        var checker = new Mock<IOfferingStaffChecker>();
        checker
            .Setup(c => c.IsStaffOnOfferingAsync(TeacherId, OfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateHandler(checker.Object).HandleAsync(new ListMaterialsForStaffQuery(TeacherId, OfferingId));

        checker.Verify(
            c => c.IsStaffOnOfferingAsync(TeacherId, OfferingId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --- Negativno ponašanje ---

    [Test]
    public void HandleAsync_UserNotStaff_IsDeniedAndMaterialsAreNotRead()
    {
        var repository = new Mock<IMaterialRepository>(MockBehavior.Strict);
        var handler = new ListMaterialsForStaffHandler(
            repository.Object,
            new InMemoryOfferingStaffChecker([(TeacherId, OfferingId)]));

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            handler.HandleAsync(new ListMaterialsForStaffQuery(Guid.NewGuid(), OfferingId)));

        repository.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_StaffOfAnotherOffering_IsDenied()
    {
        var handler = CreateHandler(new InMemoryOfferingStaffChecker([(TeacherId, Guid.NewGuid())]));

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            handler.HandleAsync(new ListMaterialsForStaffQuery(TeacherId, OfferingId)));
    }

    [Test]
    public void HandleAsync_EmptyUser_IsDeniedWithoutAskingStaffChecker()
    {
        var checker = new Mock<IOfferingStaffChecker>(MockBehavior.Strict);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            CreateHandler(checker.Object).HandleAsync(new ListMaterialsForStaffQuery(Guid.Empty, OfferingId)));

        checker.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_EmptyOffering_IsDeniedWithoutAskingStaffChecker()
    {
        var checker = new Mock<IOfferingStaffChecker>(MockBehavior.Strict);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            CreateHandler(checker.Object).HandleAsync(new ListMaterialsForStaffQuery(TeacherId, Guid.Empty)));

        checker.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_NullQuery_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => CreateHandler().HandleAsync(null!));
    }

    [Test]
    public void Constructor_RejectsNullDependencies()
    {
        var checker = new InMemoryOfferingStaffChecker([]);

        Assert.Multiple(() =>
        {
            Assert.That(() => new ListMaterialsForStaffHandler(null!, checker), Throws.ArgumentNullException);
            Assert.That(() => new ListMaterialsForStaffHandler(_materials, null!), Throws.ArgumentNullException);
        });
    }
}
