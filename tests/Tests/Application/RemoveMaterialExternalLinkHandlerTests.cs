using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-07 (tačka 1): uklanjanje spoljne veze iz materijala.
/// </summary>
[TestFixture]
public sealed class RemoveMaterialExternalLinkHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialExternalLinkRepository _links = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _materials = new InMemoryMaterialRepository();
        _links = new InMemoryMaterialExternalLinkRepository();
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);
        await _materials.AddAsync(_material);
    }

    private RemoveMaterialExternalLinkHandler CreateHandler(IMaterialExternalLinkRepository? links = null) =>
        new(
            _materials,
            links ?? _links,
            new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)]));

    private async Task<MaterialExternalLink> AddLinkAsync(Guid materialId, string url = "https://example.org/video")
    {
        var link = new MaterialExternalLink(Guid.NewGuid(), materialId, url);
        await _links.AddAsync(link);
        return link;
    }

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_TeacherRemovesLink_LinkIsRemoved()
    {
        var link = await AddLinkAsync(_material.Id);

        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialExternalLinkCommand(TeacherId, _material.Id, link.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialExternalLinkOutcome.Removed));
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Null);
    }

    [Test]
    public async Task HandleAsync_OtherLinksOfMaterialStay()
    {
        var removed = await AddLinkAsync(_material.Id, "https://example.org/staro");
        var kept = await AddLinkAsync(_material.Id, "https://example.org/aktuelno");

        await CreateHandler().HandleAsync(
            new RemoveMaterialExternalLinkCommand(TeacherId, _material.Id, removed.Id));

        var left = await _links.ListByMaterialAsync(_material.Id);
        Assert.That(left.Select(l => l.Id), Is.EqualTo(new[] { kept.Id }));
    }

    // Granično ponašanje

    [Test]
    public async Task HandleAsync_RemovingSameLinkTwice_SecondTimeIsNotFound()
    {
        var link = await AddLinkAsync(_material.Id);
        var handler = CreateHandler();
        var command = new RemoveMaterialExternalLinkCommand(TeacherId, _material.Id, link.Id);

        var first = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);

        Assert.That(first, Is.EqualTo(RemoveMaterialExternalLinkOutcome.Removed));
        Assert.That(second, Is.EqualTo(RemoveMaterialExternalLinkOutcome.LinkNotFound));
    }

    [Test]
    public async Task HandleAsync_LinkRemovedByConcurrentRequest_IsNotFound()
    {
        var link = await AddLinkAsync(_material.Id);
        var repository = new Mock<IMaterialExternalLinkRepository>();
        repository
            .Setup(r => r.GetByIdAsync(link.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        repository
            .Setup(r => r.RemoveAsync(link.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var outcome = await CreateHandler(repository.Object).HandleAsync(
            new RemoveMaterialExternalLinkCommand(TeacherId, _material.Id, link.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialExternalLinkOutcome.LinkNotFound));
    }

    [Test]
    public async Task HandleAsync_LastLink_MaterialStaysWithoutLinks()
    {
        var link = await AddLinkAsync(_material.Id);

        await CreateHandler().HandleAsync(
            new RemoveMaterialExternalLinkCommand(TeacherId, _material.Id, link.Id));

        Assert.That(await _materials.GetByIdAsync(_material.Id), Is.Not.Null);
        Assert.That(await _links.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    // Negativno ponašanje

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsMaterialNotFoundAndRemovesNothing()
    {
        var link = await AddLinkAsync(_material.Id);

        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialExternalLinkCommand(TeacherId, Guid.NewGuid(), link.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialExternalLinkOutcome.MaterialNotFound));
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_UnknownLink_ReturnsLinkNotFound()
    {
        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialExternalLinkCommand(TeacherId, _material.Id, Guid.NewGuid()));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialExternalLinkOutcome.LinkNotFound));
    }

    [Test]
    public async Task HandleAsync_LinkOfAnotherMaterial_IsNotRemoved()
    {
        var otherMaterial = new Material(Guid.NewGuid(), _material.CourseOfferingId, "Predavanje 2", null);
        await _materials.AddAsync(otherMaterial);
        var foreign = await AddLinkAsync(otherMaterial.Id);

        var outcome = await CreateHandler().HandleAsync(
            new RemoveMaterialExternalLinkCommand(TeacherId, _material.Id, foreign.Id));

        Assert.That(outcome, Is.EqualTo(RemoveMaterialExternalLinkOutcome.LinkNotFound));
        Assert.That(await _links.GetByIdAsync(foreign.Id), Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_UserNotStaff_IsDeniedAndRemovesNothing()
    {
        var link = await AddLinkAsync(_material.Id);

        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialExternalLinkCommand(OtherUserId, _material.Id, link.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_StaffOfAnotherOffering_IsDenied()
    {
        var otherMaterial = new Material(Guid.NewGuid(), Guid.NewGuid(), "Tudji materijal", null);
        await _materials.AddAsync(otherMaterial);
        var link = await AddLinkAsync(otherMaterial.Id);

        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialExternalLinkCommand(TeacherId, otherMaterial.Id, link.Id)),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _links.GetByIdAsync(link.Id), Is.Not.Null);
    }

    [Test]
    public void HandleAsync_EmptyUserId_IsDenied()
    {
        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialExternalLinkCommand(Guid.Empty, _material.Id, Guid.NewGuid())),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_UserNotStaffAndUnknownLink_IsDeniedNotNotFound()
    {
        // Korisnik bez prava ne sme da sazna da li veza postoji.
        Assert.That(
            () => CreateHandler().HandleAsync(
                new RemoveMaterialExternalLinkCommand(OtherUserId, _material.Id, Guid.NewGuid())),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_NullCommand_Throws()
    {
        Assert.That(() => CreateHandler().HandleAsync(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var staff = new InMemoryOfferingStaffChecker();

        Assert.Multiple(() =>
        {
            Assert.That(() => new RemoveMaterialExternalLinkHandler(null!, _links, staff),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialExternalLinkHandler(_materials, null!, staff),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new RemoveMaterialExternalLinkHandler(_materials, _links, null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
