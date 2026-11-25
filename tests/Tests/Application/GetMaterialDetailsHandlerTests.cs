using Application.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-11 (stavka 1): stranica detalja materijala za studenta – opis, spoljne veze, lista priloga.
/// Pravilo: samo objavljen materijal i samo aktivan student izvođenja materijala.
/// </summary>
[TestFixture]
public sealed class GetMaterialDetailsHandlerTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid OtherOffering = Guid.NewGuid();
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly Guid InactiveStudent = Guid.NewGuid();
    private static readonly Guid OtherOfferingStudent = Guid.NewGuid();
    private static readonly Guid Teacher = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialExternalLinkRepository _links = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private InMemoryMembershipReader _memberships = null!;

    [SetUp]
    public void SetUp()
    {
        _materials = new InMemoryMaterialRepository();
        _links = new InMemoryMaterialExternalLinkRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _memberships = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(Student, Offering, MembershipKind.Student, IsActive: true),
            new OfferingMembership(InactiveStudent, Offering, MembershipKind.Student, IsActive: false),
            new OfferingMembership(OtherOfferingStudent, OtherOffering, MembershipKind.Student, IsActive: true),
            new OfferingMembership(Teacher, Offering, MembershipKind.Teacher, IsActive: true),
        });
    }

    private GetMaterialDetailsHandler CreateHandler() => new(_materials, _links, _attachments, _memberships);

    private async Task<Material> AddMaterialAsync(bool published, string? description = "Uvod u predmet")
    {
        var material = new Material(Guid.NewGuid(), Offering, "Vežbe 1", description);
        if (published)
            material.Publish();
        await _materials.AddAsync(material);
        return material;
    }

    private Task AddAttachmentAsync(Guid materialId, string name, string type = "application/pdf", long size = 10) =>
        _attachments.AddAsync(new MaterialAttachment(
            Guid.NewGuid(), materialId, name, type, size, Guid.NewGuid().ToString("N")));

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_PublishedMaterialAndActiveStudent_ReturnsDescriptionLinksAndAttachments()
    {
        var material = await AddMaterialAsync(published: true);
        await _links.AddAsync(new MaterialExternalLink(Guid.NewGuid(), material.Id, "https://example.org/video"));
        await AddAttachmentAsync(material.Id, "skripta.pdf", "application/pdf", 1234);

        var result = await CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Student, material.Id));

        Assert.That(result.Outcome, Is.EqualTo(GetMaterialDetailsOutcome.Found));
        var details = result.Details!;
        Assert.Multiple(() =>
        {
            Assert.That(details.Id, Is.EqualTo(material.Id));
            Assert.That(details.Name, Is.EqualTo("Vežbe 1"));
            Assert.That(details.Description, Is.EqualTo("Uvod u predmet"));
            Assert.That(details.Links.Select(l => l.Url), Is.EqualTo(new[] { "https://example.org/video" }));
            Assert.That(details.Attachments.Single().OriginalFileName, Is.EqualTo("skripta.pdf"));
            Assert.That(details.Attachments.Single().ContentType, Is.EqualTo("application/pdf"));
            Assert.That(details.Attachments.Single().SizeInBytes, Is.EqualTo(1234));
        });
    }

    [Test]
    public async Task HandleAsync_MaterialWithoutDescriptionLinksOrAttachments_ReturnsEmptyValues()
    {
        var material = await AddMaterialAsync(published: true, description: null);

        var result = await CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Student, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(GetMaterialDetailsOutcome.Found));
            Assert.That(result.Details!.Description, Is.Empty);
            Assert.That(result.Details.Links, Is.Empty);
            Assert.That(result.Details.Attachments, Is.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_ShowsOnlyLinksAndAttachmentsOfRequestedMaterial()
    {
        var material = await AddMaterialAsync(published: true);
        var other = await AddMaterialAsync(published: true);
        await _links.AddAsync(new MaterialExternalLink(Guid.NewGuid(), other.Id, "https://example.org/tudja"));
        await AddAttachmentAsync(other.Id, "tudji.pdf");

        var result = await CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Student, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Details!.Links, Is.Empty);
            Assert.That(result.Details.Attachments, Is.Empty);
        });
    }

    // Granično ponašanje

    [Test]
    public async Task HandleAsync_AttachmentsAndLinksAreSorted()
    {
        var material = await AddMaterialAsync(published: true);
        await AddAttachmentAsync(material.Id, "b-vezbe.zip");
        await AddAttachmentAsync(material.Id, "A-uvod.pdf");
        await _links.AddAsync(new MaterialExternalLink(Guid.NewGuid(), material.Id, "https://example.org/z"));
        await _links.AddAsync(new MaterialExternalLink(Guid.NewGuid(), material.Id, "https://example.org/a"));

        var result = await CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Student, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Details!.Attachments.Select(a => a.OriginalFileName),
                Is.EqualTo(new[] { "A-uvod.pdf", "b-vezbe.zip" }));
            Assert.That(result.Details.Links.Select(l => l.Url),
                Is.EqualTo(new[] { "https://example.org/a", "https://example.org/z" }));
        });
    }

    [Test]
    public async Task HandleAsync_EmptyUser_IsDenied()
    {
        var material = await AddMaterialAsync(published: true);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Guid.Empty, material.Id)));
    }

    // Negativno ponašanje

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsNotFound()
    {
        var result = await CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Student, Guid.NewGuid()));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(GetMaterialDetailsOutcome.MaterialNotFound));
            Assert.That(result.Details, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_DraftMaterial_ReturnsNotFoundEvenForActiveStudent()
    {
        var material = await AddMaterialAsync(published: false);
        await AddAttachmentAsync(material.Id, "skripta.pdf");

        var result = await CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Student, material.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(GetMaterialDetailsOutcome.MaterialNotFound));
            Assert.That(result.Details, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_StudentOfOtherOffering_IsDeniedAndLinksAndAttachmentsAreNotRead()
    {
        var material = await AddMaterialAsync(published: true);
        var links = new Mock<IMaterialExternalLinkRepository>(MockBehavior.Strict);
        var attachments = new Mock<IMaterialAttachmentRepository>(MockBehavior.Strict);
        var handler = new GetMaterialDetailsHandler(_materials, links.Object, attachments.Object, _memberships);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            handler.HandleAsync(new GetMaterialDetailsQuery(OtherOfferingStudent, material.Id)));

        links.VerifyNoOtherCalls();
        attachments.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_InactiveStudent_IsDenied()
    {
        var material = await AddMaterialAsync(published: true);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            CreateHandler().HandleAsync(new GetMaterialDetailsQuery(InactiveStudent, material.Id)));
    }

    [Test]
    public async Task HandleAsync_TeacherIsNotAStudent_IsDenied()
    {
        var material = await AddMaterialAsync(published: true);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            CreateHandler().HandleAsync(new GetMaterialDetailsQuery(Teacher, material.Id)));
    }

    [Test]
    public async Task HandleAsync_NonMemberAndDraft_IsDeniedBeforeStateIsRevealed()
    {
        var material = await AddMaterialAsync(published: false);

        Assert.ThrowsAsync<MaterialAccessDeniedException>(() =>
            CreateHandler().HandleAsync(new GetMaterialDetailsQuery(OtherOfferingStudent, material.Id)));
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new GetMaterialDetailsHandler(null!, _links, _attachments, _memberships),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new GetMaterialDetailsHandler(_materials, null!, _attachments, _memberships),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new GetMaterialDetailsHandler(_materials, _links, null!, _memberships),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new GetMaterialDetailsHandler(_materials, _links, _attachments, null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
