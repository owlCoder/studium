using System.Text;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-06 (tačka 3): poruka po datoteci — sačuvano / nije sačuvano.
/// </summary>
[TestFixture]
public sealed class UploadMaterialAttachmentsResultTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();

    private static readonly AttachmentUploadPolicy Policy = new(
        new[] { ".pdf", ".txt" },
        20L * 1024 * 1024);

    private string _root = null!;
    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us06-result-" + Guid.NewGuid().ToString("N"));
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
        IMaterialAttachmentRepository? attachments = null,
        AttachmentUploadPolicy? policy = null)
    {
        storage ??= new LocalDiskAttachmentFileStorage(_root);
        attachments ??= _attachments;
        policy ??= Policy;
        var staff = new InMemoryOfferingStaffChecker(new[] { (TeacherId, _material.CourseOfferingId) });
        var attachFile = new AttachFileToMaterialHandler(_materials, attachments, storage, policy);
        return new UploadMaterialAttachmentsHandler(_materials, attachments, staff, policy, attachFile);
    }

    private static UploadedFile File(string name, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new UploadedFile(name, "application/pdf", bytes.Length, () => new MemoryStream(bytes));
    }

    private UploadMaterialAttachmentsCommand Command(params UploadedFile[] files) =>
        new(TeacherId, _material.Id, files);

    private string[] StoredFiles() =>
        Directory.Exists(_root) ? Directory.GetFiles(_root) : Array.Empty<string>();

    /// <summary>Skladište koje ne uspe za datoteke sa zadatim rednim brojevima (1, 2, ...).</summary>
    private IAttachmentFileStorage StorageFailingOn(params int[] failingCalls)
    {
        var real = new LocalDiskAttachmentFileStorage(_root);
        var storage = new Mock<IAttachmentFileStorage>();
        var calls = 0;
        storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((stream, ct) =>
                failingCalls.Contains(++calls)
                    ? throw new IOException(@"Ne mogu da pišem u C:\skladiste\interno.")
                    : real.SaveAsync(stream, ct));
        storage
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((key, ct) => real.DeleteAsync(key, ct));
        return storage.Object;
    }

    // Pozitivno ponašanje

    [Test]
    public async Task HandleAsync_AllFilesSaved_EachIsReportedAsSavedWithMessage()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Command(File("a.pdf", "1"), File("b.pdf", "2")));

        Assert.Multiple(() =>
        {
            Assert.That(result!.Select(r => r.Saved), Is.EqualTo(new[] { true, true }));
            Assert.That(result!.Select(r => r.Message), Is.EqualTo(new[]
            {
                "Datoteka 'a.pdf' je sačuvana.",
                "Datoteka 'b.pdf' je sačuvana."
            }));
            Assert.That(result!.All(r => r.Attachment is not null), Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_FirstFileFails_NextFileIsStillSavedAndReported()
    {
        var handler = CreateHandler(StorageFailingOn(1));

        var result = await handler.HandleAsync(Command(File("prva.pdf", "1"), File("druga.pdf", "2")));

        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Select(r => r.Saved), Is.EqualTo(new[] { false, true }));
            Assert.That(stored.Select(a => a.OriginalFileName), Is.EqualTo(new[] { "druga.pdf" }));
        });
    }

    // Negativno ponašanje

    [Test]
    public async Task HandleAsync_AllFilesFail_AllAreReportedAsNotSavedAndNothingIsStored()
    {
        var handler = CreateHandler(StorageFailingOn(1, 2));

        var result = await handler.HandleAsync(Command(File("a.pdf", "1"), File("b.pdf", "2")));

        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Select(r => r.Saved), Is.EqualTo(new[] { false, false }));
            Assert.That(result!.All(r => r.Attachment is null), Is.True);
            Assert.That(stored, Is.Empty);
            Assert.That(StoredFiles(), Is.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_StorageError_MessageDoesNotRevealInternalDetails()
    {
        var handler = CreateHandler(StorageFailingOn(1));

        var result = await handler.HandleAsync(Command(File("a.pdf", "1")));

        Assert.That(result![0].Message, Does.Not.Contain("skladiste").And.Not.Contain(@"C:\"));
    }

    [Test]
    public async Task HandleAsync_RecordWriteFailsForSecondFile_SecondNotSavedAndItsContentIsRemoved()
    {
        var attachments = new FailingOnSecondAddRepository(_attachments);
        var handler = CreateHandler(attachments: attachments);

        var result = await handler.HandleAsync(Command(File("prva.pdf", "1"), File("druga.pdf", "2")));

        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Select(r => r.Saved), Is.EqualTo(new[] { true, false }));
            Assert.That(stored.Select(a => a.OriginalFileName), Is.EqualTo(new[] { "prva.pdf" }));
            Assert.That(StoredFiles(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_FileContentCannotBeOpened_IsReportedAsNotSaved()
    {
        var handler = CreateHandler();
        var broken = new UploadedFile("pokvarena.pdf", "application/pdf", 1,
            () => throw new IOException("Deo zahteva nije dostupan."));

        var result = await handler.HandleAsync(Command(File("dobra.pdf", "1"), broken));

        Assert.That(result!.Select(r => r.Saved), Is.EqualTo(new[] { true, false }));
        Assert.That(result![1].Message, Does.StartWith("Datoteka 'pokvarena.pdf' nije sačuvana"));
    }

    [Test]
    public void HandleAsync_Cancelled_IsNotReportedAsFileResultButPropagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = CreateHandler();

        Assert.That(
            () => handler.HandleAsync(Command(File("a.pdf", "1")), cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    // Granično ponašanje

    [Test]
    public async Task HandleAsync_ActualSizeExceedsMaxDespiteDeclaredSize_ReportedWithUs05Message()
    {
        var policy = new AttachmentUploadPolicy(new[] { ".pdf" }, 10);
        var handler = CreateHandler(policy: policy);
        var bytes = Encoding.UTF8.GetBytes("01234567890"); // 11 B, a prijavljeno 5 B
        var lying = new UploadedFile("velika.pdf", "application/pdf", 5, () => new NonSeekableStream(bytes));

        var result = await handler.HandleAsync(Command(File("mala.pdf", "1"), lying));

        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Select(r => r.Saved), Is.EqualTo(new[] { true, false }));
            Assert.That(result![1].Message,
                Is.EqualTo("Datoteka 'velika.pdf' nije priložena: veća je od dozvoljenih 10 B."));
            Assert.That(stored, Has.Count.EqualTo(1));
            Assert.That(StoredFiles(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_ResultOrderFollowsSentOrderWithMixedOutcomes()
    {
        var handler = CreateHandler(StorageFailingOn(2));

        var result = await handler.HandleAsync(Command(
            File("1.pdf", "a"), File("2.pdf", "b"), File("3.pdf", "c")));

        Assert.Multiple(() =>
        {
            Assert.That(result!.Select(r => r.OriginalFileName), Is.EqualTo(new[] { "1.pdf", "2.pdf", "3.pdf" }));
            Assert.That(result!.Select(r => r.Saved), Is.EqualTo(new[] { true, false, true }));
        });
    }

    /// <summary>Repozitorijum priloga čiji drugi upis ne uspe.</summary>
    private sealed class FailingOnSecondAddRepository : IMaterialAttachmentRepository
    {
        private readonly IMaterialAttachmentRepository _inner;
        private int _adds;

        public FailingOnSecondAddRepository(IMaterialAttachmentRepository inner) => _inner = inner;

        public Task AddAsync(MaterialAttachment attachment, CancellationToken cancellationToken = default) =>
            ++_adds == 2
                ? throw new InvalidOperationException("Upis zapisa nije uspeo.")
                : _inner.AddAsync(attachment, cancellationToken);

        public Task<IReadOnlyList<MaterialAttachment>> ListByMaterialAsync(
            Guid materialId, CancellationToken cancellationToken = default) =>
            _inner.ListByMaterialAsync(materialId, cancellationToken);
    }

    /// <summary>Tok bez poznate dužine, kao telo HTTP zahteva.</summary>
    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] bytes) : base(bytes) { }
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
    }
}
