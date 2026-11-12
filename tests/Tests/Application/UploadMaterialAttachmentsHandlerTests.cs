using System.Text;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-04 (tačka 3): otpremanje jedne ili više datoteka u materijal.
/// </summary>
[TestFixture]
public sealed class UploadMaterialAttachmentsHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    // Ista pravila kao u src/Api/appsettings.json (ADR-0002).
    private static readonly AttachmentUploadPolicy Policy = new(
        new[] { ".pdf", ".docx", ".pptx", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".zip" },
        20L * 1024 * 1024);

    private string _root = null!;
    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us04-upload-" + Guid.NewGuid().ToString("N"));
        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);
        await _materials.AddAsync(_material);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private UploadMaterialAttachmentsHandler CreateHandler(
        IAttachmentFileStorage? storage = null,
        AttachmentUploadPolicy? policy = null)
    {
        storage ??= new LocalDiskAttachmentFileStorage(_root);
        policy ??= Policy;
        var staff = new InMemoryOfferingStaffChecker(new[] { (TeacherId, _material.CourseOfferingId) });
        var attachFile = new AttachFileToMaterialHandler(_materials, _attachments, storage, policy);
        return new UploadMaterialAttachmentsHandler(_materials, _attachments, staff, policy, attachFile);
    }

    private static UploadedFile File(string name, string text, string contentType = "application/pdf")
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new UploadedFile(name, contentType, bytes.Length, () => new MemoryStream(bytes));
    }

    private UploadMaterialAttachmentsCommand Command(Guid userId, params UploadedFile[] files) =>
        new(userId, _material.Id, files);

    private string[] StoredFiles() =>
        Directory.Exists(_root) ? Directory.GetFiles(_root) : Array.Empty<string>();

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_OneFile_IsSavedAndReturnedWithMetadata()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Command(TeacherId, File("skripta.pdf", "sadrzaj")));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result![0].OriginalFileName, Is.EqualTo("skripta.pdf"));
            Assert.That(result[0].ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result[0].SizeInBytes, Is.EqualTo(Encoding.UTF8.GetByteCount("sadrzaj")));
        });
        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.That(stored.Select(a => a.Id), Is.EquivalentTo(new[] { result![0].Id }));
        Assert.That(StoredFiles(), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_SeveralFiles_AreAllSavedInSentOrder()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Command(
            TeacherId,
            File("1-uvod.pdf", "a"),
            File("2-vezbe.docx", "bb", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            File("3-slika.png", "ccc", "image/png")));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Select(a => a.OriginalFileName),
            Is.EqualTo(new[] { "1-uvod.pdf", "2-vezbe.docx", "3-slika.png" }));
        Assert.That(result!.Select(a => a.SizeInBytes), Is.EqualTo(new long[] { 1, 2, 3 }));
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Has.Count.EqualTo(3));
        Assert.That(StoredFiles(), Has.Length.EqualTo(3));
    }

    [Test]
    public async Task HandleAsync_SavedContent_CanBeReadBackFromStorage()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var handler = CreateHandler(storage);

        var result = await handler.HandleAsync(Command(TeacherId, File("a.txt", "tekst priloga", "text/plain")));

        var stored = (await _attachments.ListByMaterialAsync(_material.Id)).Single(a => a.Id == result![0].Id);
        await using var stream = await storage.OpenReadAsync(stored.StorageKey);
        using var reader = new StreamReader(stream!);
        Assert.That(await reader.ReadToEndAsync(), Is.EqualTo("tekst priloga"));
    }

    // Granično ponašanje

    [Test]
    public async Task HandleAsync_FileExactlyAtMaxSize_IsSaved()
    {
        var policy = new AttachmentUploadPolicy(new[] { ".pdf" }, 10);
        var handler = CreateHandler(policy: policy);

        var result = await handler.HandleAsync(Command(TeacherId, File("granica.pdf", "0123456789")));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result![0].SizeInBytes, Is.EqualTo(10));
    }

    [Test]
    public async Task HandleAsync_TwoFilesWithSameName_AreBothSaved()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Command(
            TeacherId, File("isti.pdf", "prva"), File("isti.pdf", "druga")));

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result![0].Id, Is.Not.EqualTo(result[1].Id));
    }

    // Negativno ponašanje: pristup

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsNullAndSavesNothing()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new UploadMaterialAttachmentsCommand(TeacherId, Guid.NewGuid(), new[] { File("a.pdf", "x") }));

        Assert.That(result, Is.Null);
        Assert.That(StoredFiles(), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_UserNotStaffOfOffering_IsDeniedAndSavesNothing()
    {
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(Command(OtherUserId, File("a.pdf", "x"))),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
        Assert.That(StoredFiles(), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_StaffOfAnotherOffering_IsDenied()
    {
        var otherMaterial = new Material(Guid.NewGuid(), Guid.NewGuid(), "Tudji materijal", null);
        await _materials.AddAsync(otherMaterial);
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(new UploadMaterialAttachmentsCommand(
                TeacherId, otherMaterial.Id, new[] { File("a.pdf", "x") })),
            Throws.TypeOf<MaterialAccessDeniedException>());
        Assert.That(await _attachments.ListByMaterialAsync(otherMaterial.Id), Is.Empty);
    }

    [Test]
    public void HandleAsync_EmptyUserId_IsDenied()
    {
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(Command(Guid.Empty, File("a.pdf", "x"))),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    [Test]
    public void HandleAsync_NotStaffAndNoFiles_ReportsDeniedBeforeValidation()
    {
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(Command(OtherUserId)),
            Throws.TypeOf<MaterialAccessDeniedException>());
    }

    // Negativno ponašanje: datoteke

    [Test]
    public void HandleAsync_NoFiles_Throws()
    {
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(Command(TeacherId)),
            Throws.ArgumentException);
    }

    [Test]
    public async Task HandleAsync_SecondFileHasUnsupportedType_NoFileIsSaved()
    {
        var handler = CreateHandler();

        var ex = Assert.ThrowsAsync<AttachmentRejectedException>(() => handler.HandleAsync(Command(
            TeacherId, File("dobra.pdf", "x"), File("program.exe", "y"))));

        Assert.That(ex!.Reason, Is.EqualTo(AttachmentRejectionReason.UnsupportedType));
        Assert.That(ex.FileName, Is.EqualTo("program.exe"));
        Assert.That(ex.Message, Does.StartWith("Datoteka 'program.exe' nije priložena: tip datoteke nije dozvoljen."));
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
        Assert.That(StoredFiles(), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_FileLargerThanMax_NoFileIsSaved()
    {
        var policy = new AttachmentUploadPolicy(new[] { ".pdf" }, 10);
        var handler = CreateHandler(policy: policy);

        var ex = Assert.ThrowsAsync<AttachmentRejectedException>(() => handler.HandleAsync(Command(
            TeacherId, File("mala.pdf", "1"), File("velika.pdf", "01234567890"))));

        Assert.That(ex!.Reason, Is.EqualTo(AttachmentRejectionReason.TooLarge));
        Assert.That(ex.Message, Is.EqualTo("Datoteka 'velika.pdf' nije priložena: veća je od dozvoljenih 10 B."));
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
        Assert.That(StoredFiles(), Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task HandleAsync_FileWithoutName_NoFileIsSaved(string name)
    {
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(Command(TeacherId, File("a.pdf", "x"), File(name, "y"))),
            Throws.ArgumentException);
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_FileWithoutContentType_NoFileIsSaved()
    {
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(Command(TeacherId, File("a.pdf", "x", contentType: " "))),
            Throws.ArgumentException);
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [Test]
    public async Task HandleAsync_StorageFailsOnSecondFile_FirstStaysSavedAndErrorIsReported()
    {
        var real = new LocalDiskAttachmentFileStorage(_root);
        var storage = new Mock<IAttachmentFileStorage>();
        var calls = 0;
        storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((stream, ct) =>
                ++calls == 1 ? real.SaveAsync(stream, ct) : throw new IOException("Disk je pun."));
        var handler = CreateHandler(storage.Object);

        Assert.That(
            () => handler.HandleAsync(Command(TeacherId, File("prva.pdf", "1"), File("druga.pdf", "2"))),
            Throws.TypeOf<IOException>());

        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.That(stored.Select(a => a.OriginalFileName), Is.EqualTo(new[] { "prva.pdf" }));
    }

    [Test]
    public void HandleAsync_NullCommand_Throws()
    {
        var handler = CreateHandler();

        Assert.That(() => handler.HandleAsync(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
