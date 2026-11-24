using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-11 (stavka 1): konzolni prikaz strane detalja materijala.
/// </summary>
[TestFixture]
public sealed class MaterialDetailsViewTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly Guid Outsider = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialExternalLinkRepository _links = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;

    [SetUp]
    public void SetUp()
    {
        _materials = new InMemoryMaterialRepository();
        _links = new InMemoryMaterialExternalLinkRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
    }

    private async Task<(MaterialDetails? Details, string Output)> ShowAsync(Guid userId, Guid materialId)
    {
        var memberships = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(Student, Offering, MembershipKind.Student, IsActive: true),
        });
        var output = new StringWriter();
        var view = new MaterialDetailsView(
            new GetMaterialDetailsHandler(_materials, _links, _attachments, memberships), output);

        var details = await view.ShowAsync(userId, materialId);
        return (details, output.ToString());
    }

    private async Task<Material> AddPublishedMaterialAsync(string? description)
    {
        var material = new Material(Guid.NewGuid(), Offering, "Vežbe 1", description);
        material.Publish();
        await _materials.AddAsync(material);
        return material;
    }

    [Test]
    public async Task ShowAsync_PrintsNameDescriptionLinksAndAttachments()
    {
        var material = await AddPublishedMaterialAsync("Uvod u predmet");
        await _links.AddAsync(new MaterialExternalLink(Guid.NewGuid(), material.Id, "https://example.org/video"));
        await _attachments.AddAsync(new MaterialAttachment(
            Guid.NewGuid(), material.Id, "vezbe-1.zip", "application/zip", 20, Guid.NewGuid().ToString("N")));
        await _attachments.AddAsync(new MaterialAttachment(
            Guid.NewGuid(), material.Id, "skripta.pdf", "application/pdf", 10, Guid.NewGuid().ToString("N")));

        var (details, output) = await ShowAsync(Student, material.Id);

        Assert.That(details, Is.Not.Null);
        Assert.That(output, Is.EqualTo(string.Join(Environment.NewLine,
            "Vežbe 1",
            "Opis: Uvod u predmet",
            "",
            "Spoljne veze (1):",
            "  - https://example.org/video",
            "Prilozi (2):",
            "  - skripta.pdf",
            "  - vezbe-1.zip",
            "")));
    }

    [Test]
    public async Task ShowAsync_EmptyMaterial_PrintsEmptyStates()
    {
        var material = await AddPublishedMaterialAsync(description: null);

        var (_, output) = await ShowAsync(Student, material.Id);

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Opis: (bez opisa)"));
            Assert.That(output, Does.Contain("Spoljne veze (0):"));
            Assert.That(output, Does.Contain("  (nema veza)"));
            Assert.That(output, Does.Contain("Prilozi (0):"));
            Assert.That(output, Does.Contain("  (nema priloga)"));
        });
    }

    [Test]
    public async Task ShowAsync_UserNotMember_PrintsAccessMessageAndNoContent()
    {
        var material = await AddPublishedMaterialAsync("Tajni opis");

        var (details, output) = await ShowAsync(Outsider, material.Id);

        Assert.Multiple(() =>
        {
            Assert.That(details, Is.Null);
            Assert.That(output, Does.StartWith("Materijal nije dostupan: Materijal mogu da otvore samo studenti"));
            Assert.That(output, Does.Not.Contain("Tajni opis"));
            Assert.That(output, Does.Not.Contain("Vežbe 1"));
        });
    }

    [Test]
    public async Task ShowAsync_DraftOrUnknownMaterial_PrintsNotAvailable()
    {
        var draft = new Material(Guid.NewGuid(), Offering, "Nacrt", "opis nacrta");
        await _materials.AddAsync(draft);

        var (draftDetails, draftOutput) = await ShowAsync(Student, draft.Id);
        var (unknownDetails, unknownOutput) = await ShowAsync(Student, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(draftDetails, Is.Null);
            Assert.That(unknownDetails, Is.Null);
            Assert.That(draftOutput, Is.EqualTo("Materijal nije dostupan: ne postoji ili nije objavljen." + Environment.NewLine));
            Assert.That(unknownOutput, Is.EqualTo(draftOutput));
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var handler = new GetMaterialDetailsHandler(
            _materials, _links, _attachments, new InMemoryMembershipReader(Array.Empty<OfferingMembership>()));

        Assert.Multiple(() =>
        {
            Assert.That(() => new MaterialDetailsView(null!, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new MaterialDetailsView(handler, null!), Throws.TypeOf<ArgumentNullException>());
        });
    }
}
