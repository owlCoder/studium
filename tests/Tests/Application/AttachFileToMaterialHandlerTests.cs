using System.Text;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// ST-009, US-06 (tačka 1): prilog se vezuje za materijal tek kada je datoteka uspešno sačuvana.
/// </summary>
[TestFixture]
public sealed class AttachFileToMaterialHandlerTests
{
    private string _root = null!;
    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private Material _material = null!;

    [SetUp]
    public async Task SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us06-" + Guid.NewGuid().ToString("N"));
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

    private AttachFileToMaterialHandler CreateHandler(IAttachmentFileStorage storage) =>
        new(_materials, _attachments, storage);

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    [Test]
    public async Task HandleAsync_SavedFile_IsBoundToMaterialWithStorageKeyAndMetadata()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var handler = CreateHandler(storage);

        var id = await handler.HandleAsync(new AttachFileToMaterialCommand(
            _material.Id, " skripta.pdf ", "application/pdf", Content("sadrzaj")));

        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.That(stored, Has.Count.EqualTo(1));
        var attachment = stored[0];
        await using var saved = await storage.OpenReadAsync(attachment.StorageKey);
        using var reader = new StreamReader(saved!);
        Assert.Multiple(() =>
        {
            Assert.That(attachment.Id, Is.EqualTo(id));
            Assert.That(attachment.MaterialId, Is.EqualTo(_material.Id));
            Assert.That(attachment.OriginalFileName, Is.EqualTo("skripta.pdf"));
            Assert.That(attachment.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(attachment.SizeInBytes, Is.EqualTo(7));
            Assert.That(reader.ReadToEnd(), Is.EqualTo("sadrzaj"));
        });
    }

    [Test]
    public void HandleAsync_StorageFails_NoAttachmentIsBound()
    {
        var storage = new Mock<IAttachmentFileStorage>();
        storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk je pun."));
        var handler = CreateHandler(storage.Object);

        Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(new AttachFileToMaterialCommand(
            _material.Id, "skripta.pdf", "application/pdf", Content("sadrzaj"))));

        Assert.That(_attachments.ListByMaterialAsync(_material.Id).Result, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_ContentFailsWhileReading_NoAttachmentAndNoFileLeft()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var handler = CreateHandler(storage);

        Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(new AttachFileToMaterialCommand(
            _material.Id, "skripta.pdf", "application/pdf", new FailingStream())));

        Assert.Multiple(async () =>
        {
            Assert.That(await _attachments.ListByMaterialAsync(_material.Id), Is.Empty);
            Assert.That(Directory.GetFiles(_root), Is.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_UnknownMaterial_ReturnsNullAndDoesNotSaveFile()
    {
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        var handler = CreateHandler(storage.Object);
        var unknownMaterialId = Guid.NewGuid();

        var id = await handler.HandleAsync(new AttachFileToMaterialCommand(
            unknownMaterialId, "skripta.pdf", "application/pdf", Content("sadrzaj")));

        Assert.Multiple(async () =>
        {
            Assert.That(id, Is.Null);
            Assert.That(await _attachments.ListByMaterialAsync(unknownMaterialId), Is.Empty);
        });
        storage.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void HandleAsync_BlankFileName_ThrowsBeforeSavingFile(string fileName)
    {
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        var handler = CreateHandler(storage.Object);

        Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(new AttachFileToMaterialCommand(
            _material.Id, fileName, "application/pdf", Content("sadrzaj"))));

        storage.VerifyNoOtherCalls();
        Assert.That(_attachments.ListByMaterialAsync(_material.Id).Result, Is.Empty);
    }

    [Test]
    public void HandleAsync_BlankContentType_ThrowsBeforeSavingFile()
    {
        var storage = new Mock<IAttachmentFileStorage>(MockBehavior.Strict);
        var handler = CreateHandler(storage.Object);

        Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(new AttachFileToMaterialCommand(
            _material.Id, "skripta.pdf", " ", Content("sadrzaj"))));

        storage.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_NonSeekableContent_SizeIsMeasuredFromSavedBytes()
    {
        var handler = CreateHandler(new LocalDiskAttachmentFileStorage(_root));
        var bytes = new byte[100_000];

        await handler.HandleAsync(new AttachFileToMaterialCommand(
            _material.Id, "slika.png", "image/png", new NonSeekableStream(bytes)));

        var stored = await _attachments.ListByMaterialAsync(_material.Id);
        Assert.That(stored.Single().SizeInBytes, Is.EqualTo(100_000));
    }

    /// <summary>Tok koji pukne posle prvog dela sadržaja (prekinut prenos).</summary>
    private sealed class FailingStream : MemoryStream
    {
        private bool _first = true;

        public FailingStream() : base(new byte[1024]) { }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_first)
                throw new IOException("Prenos je prekinut.");
            _first = false;
            return base.ReadAsync(buffer[..Math.Min(buffer.Length, 10)], cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <summary>Tok bez poznate dužine, kao telo HTTP zahteva.</summary>
    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] bytes) : base(bytes) { }
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
    }
}
