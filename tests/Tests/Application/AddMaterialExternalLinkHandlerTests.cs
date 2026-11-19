using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-03: dodavanje spoljne veze materijalu.
/// </summary>
[TestFixture]
public sealed class AddMaterialExternalLinkHandlerTests
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

    private AddMaterialExternalLinkHandler CreateHandler() =>
        new(
            _materials,
            _links,
            new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)]));

    private Task<Guid?> AddAsync(Guid userId, string url, Guid? materialId = null) =>
        CreateHandler().HandleAsync(
            new AddMaterialExternalLinkCommand(userId, materialId ?? _material.Id, url));

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_TeacherAddsLink_LinkIsSavedForMaterial()
    {
        var id = await AddAsync(TeacherId, "https://example.org/video-1");

        Assert.That(id, Is.Not.Null);
        var saved = await _links.GetByIdAsync(id!.Value);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.MaterialId, Is.EqualTo(_material.Id));
            Assert.That(saved.Url.AbsoluteUri, Is.EqualTo("https://example.org/video-1"));
        });
    }

    [Test]
    public async Task HandleAsync_TwoLinks_BothAreListedForMaterial()
    {
        await AddAsync(TeacherId, "https://example.org/1");
        await AddAsync(TeacherId, "http://example.org/2");

        var links = await _links.ListByMaterialAsync(_material.Id);

        Assert.That(
            links.Select(l => l.Url.AbsoluteUri),
            Is.EquivalentTo(new[] { "https://example.org/1", "http://example.org/2" }));
    }

    [Test]
    public async Task HandleAsync_TrimsAddress()
    {
        var id = await AddAsync(TeacherId, "  https://example.org/a  ");

        var saved = await _links.GetByIdAsync(id!.Value);
        Assert.That(saved!.Url.AbsoluteUri, Is.EqualTo("https://example.org/a"));
    }

    // Negativno ponašanje

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsNullAndSavesNothing()
    {
        var unknownMaterialId = Guid.NewGuid();

        var id = await AddAsync(TeacherId, "https://example.org/video", unknownMaterialId);

        Assert.Multiple(async () =>
        {
            Assert.That(id, Is.Null);
            Assert.That(await _links.ListByMaterialAsync(unknownMaterialId), Is.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_UserNotOnStaff_IsDeniedAndSavesNothing()
    {
        Assert.That(
            () => AddAsync(OtherUserId, "https://example.org/video"),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _links.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_TeacherOfAnotherOffering_IsDenied()
    {
        var otherMaterial = new Material(Guid.NewGuid(), Guid.NewGuid(), "Drugi predmet", null);
        await _materials.AddAsync(otherMaterial);

        Assert.That(
            () => AddAsync(TeacherId, "https://example.org/video", otherMaterial.Id),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _links.ListByMaterialAsync(otherMaterial.Id), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_EmptyUser_IsDenied()
    {
        Assert.That(
            () => AddAsync(Guid.Empty, "https://example.org/video"),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _links.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [TestCase("www.example.org")]
    [TestCase("javascript:alert(1)")]
    [TestCase("")]
    public async Task HandleAsync_InvalidAddress_IsRejectedAndSavesNothing(string url)
    {
        Assert.That(
            () => AddAsync(TeacherId, url),
            Throws.InstanceOf<ArgumentException>().With.Property("ParamName").EqualTo("url"));
        Assert.That(await _links.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [Test]
    public void HandleAsync_UserWithoutRightsGetsDenialBeforeAddressCheck()
    {
        Assert.That(
            () => AddAsync(OtherUserId, "www.example.org"),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_RejectsNullCommand()
    {
        Assert.That(() => CreateHandler().HandleAsync(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_RejectsMissingDependencies()
    {
        var staff = new InMemoryOfferingStaffChecker();

        Assert.Multiple(() =>
        {
            Assert.That(() => new AddMaterialExternalLinkHandler(null!, _links, staff), Throws.ArgumentNullException);
            Assert.That(() => new AddMaterialExternalLinkHandler(_materials, null!, staff), Throws.ArgumentNullException);
            Assert.That(() => new AddMaterialExternalLinkHandler(_materials, _links, null!), Throws.ArgumentNullException);
        });
    }
}
