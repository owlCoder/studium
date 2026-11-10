using System.Text;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class LocalDiskAttachmentFileStorageTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-attachments-tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    // Pozitivno ponašanje

    [Test]
    public async Task SaveAsync_ThenOpenReadAsync_ReturnsSameContent()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var bytes = Encoding.UTF8.GetBytes("Predavanje 1 - uvod");

        var key = await storage.SaveAsync(new MemoryStream(bytes));
        await using var stream = await storage.OpenReadAsync(key);

        Assert.That(stream, Is.Not.Null);
        using var copy = new MemoryStream();
        await stream!.CopyToAsync(copy);
        Assert.That(copy.ToArray(), Is.EqualTo(bytes));
    }

    [Test]
    public async Task SaveAsync_SameContentTwice_ReturnsDifferentKeys()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        var first = await storage.SaveAsync(new MemoryStream(new byte[] { 1, 2, 3 }));
        var second = await storage.SaveAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(2));
    }

    [Test]
    public void Constructor_CreatesMissingRootDirectory()
    {
        _ = new LocalDiskAttachmentFileStorage(_root);

        Assert.That(Directory.Exists(_root), Is.True);
    }

    // Granično ponašanje

    [Test]
    public async Task SaveAsync_EmptyStream_IsStoredAsEmptyFile()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        var key = await storage.SaveAsync(new MemoryStream());
        await using var stream = await storage.OpenReadAsync(key);

        Assert.That(stream, Is.Not.Null);
        Assert.That(stream!.Length, Is.EqualTo(0));
    }

    [Test]
    public async Task SaveAsync_StreamNotAtStart_StoresRemainingContent()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var source = new MemoryStream(new byte[] { 9, 8, 7, 6 }) { Position = 2 };

        var key = await storage.SaveAsync(source);
        await using var stream = await storage.OpenReadAsync(key);

        Assert.That(stream!.Length, Is.EqualTo(2));
    }

    // Negativno ponašanje

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_WithoutRootPath_Throws(string? rootPath)
    {
        Assert.That(
            () => new LocalDiskAttachmentFileStorage(rootPath!),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void SaveAsync_NullStream_Throws()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        Assert.That(
            () => storage.SaveAsync(null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void SaveAsync_WhenReadingSourceFails_LeavesNoFileBehind()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        Assert.That(
            () => storage.SaveAsync(new FailingStream(bytesBeforeFailure: 10)),
            Throws.TypeOf<IOException>());
        Assert.That(Directory.GetFiles(_root), Is.Empty);
    }

    [Test]
    public void SaveAsync_WhenCancelled_ThrowsAndLeavesNoFileBehind()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => storage.SaveAsync(new MemoryStream(new byte[] { 1 }), source.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(Directory.GetFiles(_root), Is.Empty);
    }

    [Test]
    public async Task OpenReadAsync_UnknownKey_ReturnsNull()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        var stream = await storage.OpenReadAsync(Guid.NewGuid().ToString("N"));

        Assert.That(stream, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("..\\..\\secret.txt")]
    [TestCase("../../secret.txt")]
    [TestCase("not-a-storage-key")]
    public async Task OpenReadAsync_InvalidKey_ReturnsNull(string? key)
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        var stream = await storage.OpenReadAsync(key!);

        Assert.That(stream, Is.Null);
    }

    [Test]
    public async Task OpenReadAsync_FileOutsideRootWithValidLookingName_IsNotReachable()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var outsideKey = Guid.NewGuid().ToString("N");
        var outsidePath = Path.Combine(Path.GetDirectoryName(_root)!, outsideKey);
        await File.WriteAllTextAsync(outsidePath, "van skladista");

        try
        {
            var stream = await storage.OpenReadAsync(outsideKey);
            Assert.That(stream, Is.Null);
        }
        finally
        {
            File.Delete(outsidePath);
        }
    }

    private sealed class FailingStream : Stream
    {
        private readonly int _bytesBeforeFailure;
        private int _read;

        public FailingStream(int bytesBeforeFailure) => _bytesBeforeFailure = bytesBeforeFailure;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_read >= _bytesBeforeFailure)
            {
                throw new IOException("Simuliran prekid prenosa.");
            }

            var n = Math.Min(count, _bytesBeforeFailure - _read);
            Array.Fill(buffer, (byte)0xAB, offset, n);
            _read += n;
            return n;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
