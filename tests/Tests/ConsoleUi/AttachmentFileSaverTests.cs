using System.Text;
using ConsoleUi.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-11 (stavka 3): čuvanje preuzetog priloga pod izvornim nazivom, bezbedno i bez prepisivanja.
/// </summary>
[TestFixture]
public sealed class AttachmentFileSaverTests
{
    private string _root = null!;
    private string _downloads = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-us11-saver-" + Guid.NewGuid().ToString("N"));
        _downloads = Path.Combine(_root, "Preuzimanja");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    // Pozitivno ponašanje

    [Test]
    public async Task SaveAsync_SavesContentUnderOriginalName()
    {
        var path = await new AttachmentFileSaver(_downloads).SaveAsync("skripta.pdf", Content("sadrzaj"));

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo(Path.Combine(_downloads, "skripta.pdf")));
            Assert.That(File.ReadAllText(path), Is.EqualTo("sadrzaj"));
        });
    }

    [Test]
    public async Task SaveAsync_KeepsSpacesAndSerbianLetters()
    {
        var path = await new AttachmentFileSaver(_downloads).SaveAsync("Vežbe 1 – čas.pdf", Content("x"));

        Assert.That(Path.GetFileName(path), Is.EqualTo("Vežbe 1 – čas.pdf"));
    }

    [Test]
    public async Task SaveAsync_CreatesMissingTargetDirectory()
    {
        Assert.That(Directory.Exists(_downloads), Is.False);

        await new AttachmentFileSaver(_downloads).SaveAsync("a.txt", Content("x"));

        Assert.That(Directory.Exists(_downloads), Is.True);
    }

    // Granično ponašanje

    [Test]
    public async Task SaveAsync_ExistingFile_IsNotOverwrittenAndCopyGetsNumber()
    {
        var saver = new AttachmentFileSaver(_downloads);

        var first = await saver.SaveAsync("skripta.pdf", Content("prva"));
        var second = await saver.SaveAsync("skripta.pdf", Content("druga"));
        var third = await saver.SaveAsync("skripta.pdf", Content("treca"));

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(first), Is.EqualTo("prva"));
            Assert.That(Path.GetFileName(second), Is.EqualTo("skripta (2).pdf"));
            Assert.That(File.ReadAllText(second), Is.EqualTo("druga"));
            Assert.That(Path.GetFileName(third), Is.EqualTo("skripta (3).pdf"));
        });
    }

    [TestCase(@"..\..\virus.pdf", "virus.pdf")]
    [TestCase("../../virus.pdf", "virus.pdf")]
    [TestCase(@"C:\Windows\virus.pdf", "virus.pdf")]
    [TestCase("folder/podfolder/skripta.pdf", "skripta.pdf")]
    public async Task SaveAsync_NameWithPath_IsSavedInsideTargetDirectory(string originalName, string expectedName)
    {
        var path = await new AttachmentFileSaver(_downloads).SaveAsync(originalName, Content("x"));

        Assert.Multiple(() =>
        {
            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(_downloads));
            Assert.That(Path.GetFileName(path), Is.EqualTo(expectedName));
        });
    }

    [TestCase("ime:sa*zabranjenim?znakovima.pdf", "ime_sa_zabranjenim_znakovima.pdf")]
    [TestCase("navodnici\"<>|.txt", "navodnici____.txt")]
    [TestCase("tacka na kraju.", "tacka na kraju")]
    [TestCase("", "prilog")]
    [TestCase("   ", "prilog")]
    [TestCase("..", "prilog")]
    [TestCase(null, "prilog")]
    public void ToSafeFileName_ReturnsFileNameWithoutForbiddenCharacters(string? originalName, string expected)
    {
        Assert.That(AttachmentFileSaver.ToSafeFileName(originalName), Is.EqualTo(expected));
    }

    // Negativno ponašanje

    [Test]
    public void SaveAsync_ContentFailsWhileReading_NoPartialFileIsLeft()
    {
        var saver = new AttachmentFileSaver(_downloads);

        Assert.ThrowsAsync<IOException>(() => saver.SaveAsync("skripta.pdf", new FailingStream()));

        Assert.That(Directory.GetFiles(_downloads), Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankDirectory_Throws(string directory)
    {
        Assert.Throws<ArgumentException>(() => new AttachmentFileSaver(directory));
    }

    [Test]
    public void SaveAsync_NullContent_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => new AttachmentFileSaver(_downloads).SaveAsync("a.pdf", null!));
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
}
