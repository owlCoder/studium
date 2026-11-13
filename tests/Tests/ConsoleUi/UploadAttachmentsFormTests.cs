using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-04 (tačka 4): konzolna forma za izbor i slanje datoteka sa prikazom liste priloga.
/// </summary>
[TestFixture]
public sealed class UploadAttachmentsFormTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private static readonly AttachmentUploadPolicy Policy = new(
        new[] { ".pdf", ".docx", ".pptx", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".zip" },
        20L * 1024 * 1024);

    private string _sourceDir = null!;
    private string _storageDir = null!;
    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), "studium-us04-form-" + Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(root, "izvor");
        _storageDir = Path.Combine(root, "skladiste");
        Directory.CreateDirectory(_sourceDir);

        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _material = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", null);
        await _materials.AddAsync(_material);
    }

    [TearDown]
    public void TearDown()
    {
        var root = Path.GetDirectoryName(_sourceDir)!;
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    private string CreateSourceFile(string name, string content)
    {
        var path = Path.Combine(_sourceDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private async Task<(IReadOnlyList<AttachmentUploadResult>? Results, string Output)> RunFormAsync(
        string userInput,
        Guid? userId = null,
        Guid? materialId = null,
        IAttachmentFileStorage? storage = null)
    {
        storage ??= new LocalDiskAttachmentFileStorage(_storageDir);
        var handler = new UploadMaterialAttachmentsHandler(
            _materials,
            _attachments,
            new InMemoryOfferingStaffChecker([(TeacherId, _material.CourseOfferingId)]),
            Policy,
            new AttachFileToMaterialHandler(_materials, _attachments, storage, Policy));
        var output = new StringWriter();
        var form = new UploadAttachmentsForm(handler, _attachments, new StringReader(userInput), output);

        var results = await form.RunAsync(userId ?? TeacherId, materialId ?? _material.Id);
        return (results, output.ToString());
    }

    // Pozitivno ponašanje

    [Test]
    public async Task RunAsync_OneFile_IsSavedAndShownInAttachmentList()
    {
        var path = CreateSourceFile("skripta.pdf", "sadrzaj");

        var (results, output) = await RunFormAsync($"{path}\n\n");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results![0].Saved, Is.True);
        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.Single().OriginalFileName, Is.EqualTo("skripta.pdf"));
            Assert.That(stored.Single().ContentType, Is.EqualTo("application/pdf"));
            Assert.That(stored.Single().SizeInBytes, Is.EqualTo(7));
            Assert.That(output, Does.Contain("[sačuvano] Datoteka 'skripta.pdf' je sačuvana."));
            Assert.That(output, Does.Contain("Prilozi materijala (1):"));
            Assert.That(output, Does.Contain("- skripta.pdf (application/pdf, 7 B)"));
        });
    }

    [Test]
    public async Task RunAsync_SeveralFiles_AreAllSavedAndListedAlphabetically()
    {
        var b = CreateSourceFile("b-vezbe.docx", "bb");
        var a = CreateSourceFile("a-uvod.txt", "a");

        var (results, output) = await RunFormAsync($"{b}\n{a}\n\n");

        Assert.That(results!.Select(r => r.OriginalFileName), Is.EqualTo(new[] { "b-vezbe.docx", "a-uvod.txt" }));
        Assert.That(results!.All(r => r.Saved), Is.True);
        Assert.That(output, Does.Contain("Prilozi materijala (2):"));
        Assert.That(output.IndexOf("- a-uvod.txt", StringComparison.Ordinal),
            Is.LessThan(output.IndexOf("- b-vezbe.docx", StringComparison.Ordinal)));
    }

    // Poslovni scenario ST-009 + ST-010 (docs/st-009-st-010/poslovni-scenario.md), korak 4:
    // nastavnik prilaže dve datoteke (PDF i ZIP).
    [Test]
    public async Task RunAsync_BusinessScenarioStep4_PdfAndZipAreSavedAndListed()
    {
        var pdf = CreateSourceFile("vezbe-1.pdf", "pdf sadrzaj");
        var zip = CreateSourceFile("vezbe-1-primeri.zip", "zip sadrzaj");

        var (results, output) = await RunFormAsync($"{pdf}\n{zip}\n\n");

        Assert.That(results!.Select(r => r.Saved), Is.EqualTo(new[] { true, true }));
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Prilozi materijala (2):"));
            Assert.That(output, Does.Contain("- vezbe-1.pdf (application/pdf, 11 B)"));
            Assert.That(output, Does.Contain("- vezbe-1-primeri.zip (application/zip, 11 B)"));
        });
    }

    [Test]
    public async Task RunAsync_ListIncludesAttachmentsSavedEarlier()
    {
        await RunFormAsync($"{CreateSourceFile("prvi.pdf", "1")}\n\n");

        var (_, output) = await RunFormAsync($"{CreateSourceFile("drugi.pdf", "2")}\n\n");

        Assert.That(output, Does.Contain("Prilozi materijala (2):"));
        Assert.That(output, Does.Contain("- prvi.pdf"));
        Assert.That(output, Does.Contain("- drugi.pdf"));
    }

    [Test]
    public async Task RunAsync_PathInQuotes_IsAccepted()
    {
        var path = CreateSourceFile("sa razmakom.pdf", "x");

        var (results, _) = await RunFormAsync($"\"{path}\"\n\n");

        Assert.That(results!.Single().Saved, Is.True);
        Assert.That(results!.Single().OriginalFileName, Is.EqualTo("sa razmakom.pdf"));
    }

    [Test]
    public async Task RunAsync_MissingPath_IsReportedAndOtherFilesAreSent()
    {
        var path = CreateSourceFile("postoji.pdf", "x");
        var missing = Path.Combine(_sourceDir, "ne-postoji.pdf");

        var (results, output) = await RunFormAsync($"{missing}\n{path}\n\n");

        Assert.That(output, Does.Contain($"Datoteka '{missing}' ne postoji."));
        Assert.That(results!.Select(r => r.OriginalFileName), Is.EqualTo(new[] { "postoji.pdf" }));
    }

    // Granično ponašanje

    [Test]
    public async Task RunAsync_SizesAreShownInReadableUnits()
    {
        var kb = CreateSourceFile("kb.txt", new string('a', 2048));

        var (_, output) = await RunFormAsync($"{kb}\n\n");

        Assert.That(output, Does.Contain("- kb.txt (text/plain, 2 KB)"));
    }

    [Test]
    public async Task RunAsync_UppercaseExtension_IsMappedToContentType()
    {
        var path = CreateSourceFile("SLIKA.PNG", "x");

        await RunFormAsync($"{path}\n\n");

        Assert.That((await _attachments.ListByMaterialAsync(_material.Id)).Single().ContentType,
            Is.EqualTo("image/png"));
    }

    [Test]
    public async Task RunAsync_EmptyFirstLine_SendsNothing()
    {
        var (results, output) = await RunFormAsync("\n");

        Assert.That(results, Is.Null);
        Assert.That(output, Does.Contain("Nije izabrana nijedna datoteka. Ništa nije poslato."));
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [Test]
    public async Task RunAsync_InputEndsBeforeEmptyLine_SendsNothing()
    {
        var path = CreateSourceFile("a.pdf", "x");

        var (results, output) = await RunFormAsync($"{path}\n");

        Assert.That(results, Is.Null);
        Assert.That(output, Does.Contain("Unos je prekinut. Nijedna datoteka nije poslata."));
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    // Negativno ponašanje

    [Test]
    public async Task RunAsync_UnsupportedType_NothingIsSavedAndMessageIsShown()
    {
        var pdf = CreateSourceFile("dobra.pdf", "x");
        var exe = CreateSourceFile("program.exe", "y");

        var (results, output) = await RunFormAsync($"{pdf}\n{exe}\n\n");

        Assert.That(results, Is.Null);
        Assert.That(output, Does.Contain("Datoteka 'program.exe' nije priložena: tip datoteke nije dozvoljen."));
        Assert.That(output, Does.Not.Contain("Prilozi materijala"));
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
    }

    [Test]
    public async Task RunAsync_UserNotStaff_NothingIsSavedAndListIsNotShown()
    {
        await RunFormAsync($"{CreateSourceFile("postojeci.pdf", "x")}\n\n");
        var path = CreateSourceFile("a.pdf", "x");

        var (results, output) = await RunFormAsync($"{path}\n\n", userId: OtherUserId);

        Assert.That(results, Is.Null);
        Assert.That(output, Does.Contain("Datoteke nisu poslate: Samo nastavno osoblje"));
        Assert.That(output, Does.Not.Contain("postojeci.pdf"));
        Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RunAsync_UnknownMaterial_ReportsAndSavesNothing()
    {
        var path = CreateSourceFile("a.pdf", "x");

        var (results, output) = await RunFormAsync($"{path}\n\n", materialId: Guid.NewGuid());

        Assert.That(results, Is.Null);
        Assert.That(output, Does.Contain("Datoteke nisu poslate: materijal ne postoji."));
    }

    [Test]
    public async Task RunAsync_StorageFailsOnSecondFile_ShowsPerFileResultAndOnlyFirstInList()
    {
        var real = new LocalDiskAttachmentFileStorage(_storageDir);
        var storage = new Mock<IAttachmentFileStorage>();
        var calls = 0;
        storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((stream, ct) =>
                ++calls == 1 ? real.SaveAsync(stream, ct) : throw new IOException("Disk je pun."));
        var first = CreateSourceFile("prva.pdf", "1");
        var second = CreateSourceFile("druga.pdf", "2");

        var (results, output) = await RunFormAsync($"{first}\n{second}\n\n", storage: storage.Object);

        Assert.That(results!.Select(r => r.Saved), Is.EqualTo(new[] { true, false }));
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("[sačuvano] Datoteka 'prva.pdf' je sačuvana."));
            Assert.That(output, Does.Contain("[nije sačuvano] Datoteka 'druga.pdf' nije sačuvana"));
            Assert.That(output, Does.Contain("Prilozi materijala (1):"));
            Assert.That(output, Does.Not.Contain("- druga.pdf"));
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var handler = new UploadMaterialAttachmentsHandler(
            _materials, _attachments, new InMemoryOfferingStaffChecker(), Policy,
            new AttachFileToMaterialHandler(_materials, _attachments, new LocalDiskAttachmentFileStorage(_storageDir), Policy));

        Assert.Multiple(() =>
        {
            Assert.That(() => new UploadAttachmentsForm(null!, _attachments, TextReader.Null, TextWriter.Null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new UploadAttachmentsForm(handler, null!, TextReader.Null, TextWriter.Null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new UploadAttachmentsForm(handler, _attachments, null!, TextWriter.Null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new UploadAttachmentsForm(handler, _attachments, TextReader.Null, null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
