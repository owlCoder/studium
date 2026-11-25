using System.Text;
using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-11 (stavka 3): student preuzima prilog, a datoteka se čuva sa izvornim nazivom.
/// Pravo na preuzimanje određuje DownloadMaterialAttachmentHandler iz US-12.
/// </summary>
[TestFixture]
public sealed class AttachmentDownloadViewTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid OtherOffering = Guid.NewGuid();
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly Guid OtherOfferingStudent = Guid.NewGuid();
    private static readonly byte[] FileBytes = Encoding.UTF8.GetBytes("sadrzaj priloga");

    private string _root = null!;
    private string _downloads = null!;
    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private LocalDiskAttachmentFileStorage _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us11-download-" + Guid.NewGuid().ToString("N"));
        _downloads = Path.Combine(_root, "Preuzimanja");
        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _storage = new LocalDiskAttachmentFileStorage(Path.Combine(_root, "skladiste"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private async Task<(string? Path, string Output)> DownloadAsync(Guid userId, Guid materialId, Guid attachmentId)
    {
        var memberships = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(Student, Offering, MembershipKind.Student, IsActive: true),
            new OfferingMembership(OtherOfferingStudent, OtherOffering, MembershipKind.Student, IsActive: true),
        });
        var handler = new DownloadMaterialAttachmentHandler(
            _materials, _attachments, _storage, new InMemoryOfferingStaffChecker(), memberships);
        var output = new StringWriter();
        var view = new AttachmentDownloadView(handler, new AttachmentFileSaver(_downloads), output);

        var path = await view.DownloadAsync(userId, materialId, attachmentId);
        return (path, output.ToString());
    }

    private async Task<Material> AddMaterialAsync(bool published)
    {
        var material = new Material(Guid.NewGuid(), Offering, "Vežbe 1", null);
        if (published)
            material.Publish();
        await _materials.AddAsync(material);
        return material;
    }

    private async Task<MaterialAttachment> AddAttachmentAsync(Guid materialId, string originalName)
    {
        var storageKey = await _storage.SaveAsync(new MemoryStream(FileBytes));
        var attachment = new MaterialAttachment(
            Guid.NewGuid(), materialId, originalName, "application/pdf", FileBytes.Length, storageKey);
        await _attachments.AddAsync(attachment);
        return attachment;
    }

    // Pozitivno ponašanje

    [Test]
    public async Task DownloadAsync_EnrolledStudent_SavesFileWithOriginalNameAndContent()
    {
        var material = await AddMaterialAsync(published: true);
        var attachment = await AddAttachmentAsync(material.Id, "Vežbe 1 – skripta.pdf");

        var (path, output) = await DownloadAsync(Student, material.Id, attachment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo(Path.Combine(_downloads, "Vežbe 1 – skripta.pdf")));
            Assert.That(File.ReadAllBytes(path!), Is.EqualTo(FileBytes));
            Assert.That(output, Does.Contain("Preuzeto: Vežbe 1 – skripta.pdf (application/pdf, 15 B)"));
            Assert.That(output, Does.Contain($"Sačuvano u: {path}"));
        });
    }

    [Test]
    public async Task DownloadAsync_SameAttachmentTwice_SecondCopyIsNotOverwritten()
    {
        var material = await AddMaterialAsync(published: true);
        var attachment = await AddAttachmentAsync(material.Id, "skripta.pdf");

        var (first, _) = await DownloadAsync(Student, material.Id, attachment.Id);
        var (second, _) = await DownloadAsync(Student, material.Id, attachment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(Path.GetFileName(first), Is.EqualTo("skripta.pdf"));
            Assert.That(Path.GetFileName(second), Is.EqualTo("skripta (2).pdf"));
        });
    }

    // Negativno ponašanje

    [Test]
    public async Task DownloadAsync_StudentOfOtherOffering_IsDeniedAndNothingIsSaved()
    {
        var material = await AddMaterialAsync(published: true);
        var attachment = await AddAttachmentAsync(material.Id, "skripta.pdf");

        var (path, output) = await DownloadAsync(OtherOfferingStudent, material.Id, attachment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Null);
            Assert.That(output, Does.StartWith("Prilog nije dostupan: "));
            Assert.That(Directory.Exists(_downloads), Is.False, "Ništa ne sme biti sačuvano.");
        });
    }

    [Test]
    public async Task DownloadAsync_DraftMaterial_IsNotAvailableToStudent()
    {
        var material = await AddMaterialAsync(published: false);
        var attachment = await AddAttachmentAsync(material.Id, "skripta.pdf");

        var (path, output) = await DownloadAsync(Student, material.Id, attachment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Null);
            Assert.That(output, Is.EqualTo("Prilog nije dostupan." + Environment.NewLine));
        });
    }

    [Test]
    public async Task DownloadAsync_UnknownAttachment_IsNotAvailable()
    {
        var material = await AddMaterialAsync(published: true);

        var (path, output) = await DownloadAsync(Student, material.Id, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Null);
            Assert.That(output, Is.EqualTo("Prilog nije dostupan." + Environment.NewLine));
        });
    }

    [Test]
    public async Task DownloadAsync_AttachmentOfOtherMaterial_IsNotAvailable()
    {
        var material = await AddMaterialAsync(published: true);
        var other = await AddMaterialAsync(published: true);
        var attachmentOfOther = await AddAttachmentAsync(other.Id, "tudji.pdf");

        var (path, _) = await DownloadAsync(Student, material.Id, attachmentOfOther.Id);

        Assert.That(path, Is.Null);
    }

    [Test]
    public async Task DownloadAsync_ContentMissingInStorage_IsNotAvailableAndNothingIsSaved()
    {
        var material = await AddMaterialAsync(published: true);
        var attachment = new MaterialAttachment(
            Guid.NewGuid(), material.Id, "skripta.pdf", "application/pdf", 10, Guid.NewGuid().ToString("N"));
        await _attachments.AddAsync(attachment);

        var (path, output) = await DownloadAsync(Student, material.Id, attachment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Null);
            Assert.That(output, Is.EqualTo("Prilog nije dostupan." + Environment.NewLine));
            Assert.That(Directory.Exists(_downloads), Is.False);
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var handler = new DownloadMaterialAttachmentHandler(
            _materials, _attachments, _storage, new InMemoryOfferingStaffChecker(),
            new InMemoryMembershipReader(Array.Empty<OfferingMembership>()));
        var saver = new AttachmentFileSaver(_downloads);

        Assert.Multiple(() =>
        {
            Assert.That(() => new AttachmentDownloadView(null!, saver, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new AttachmentDownloadView(handler, null!, TextWriter.Null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new AttachmentDownloadView(handler, saver, null!), Throws.TypeOf<ArgumentNullException>());
        });
    }
}
