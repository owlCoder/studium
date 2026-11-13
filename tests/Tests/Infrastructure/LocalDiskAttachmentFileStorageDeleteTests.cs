using System.Text;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.Infrastructure;

/// <summary>
/// ST-009, US-06: brisanje sadržaja iz lokalnog skladišta priloga.
/// </summary>
[TestFixture]
public sealed class LocalDiskAttachmentFileStorageDeleteTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "studium-attachments-delete-tests", Guid.NewGuid().ToString("N"));
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
    public async Task DeleteAsync_ExistingKey_RemovesContentAndReturnsTrue()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var key = await storage.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("sadrzaj")));

        var deleted = await storage.DeleteAsync(key);

        var reopened = await storage.OpenReadAsync(key);
        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(reopened, Is.Null);
            Assert.That(Directory.GetFiles(_root), Is.Empty);
        });
    }

    [Test]
    public async Task DeleteAsync_DeletesOnlyGivenKey()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var first = await storage.SaveAsync(new MemoryStream(new byte[] { 1 }));
        var second = await storage.SaveAsync(new MemoryStream(new byte[] { 2 }));

        await storage.DeleteAsync(first);

        await using var remaining = await storage.OpenReadAsync(second);
        Assert.That(remaining, Is.Not.Null);
    }

    // Granično ponašanje

    [Test]
    public async Task DeleteAsync_CalledTwice_SecondReturnsFalse()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);
        var key = await storage.SaveAsync(new MemoryStream(new byte[] { 1 }));
        await storage.DeleteAsync(key);

        Assert.That(await storage.DeleteAsync(key), Is.False);
    }

    // Negativno ponašanje

    [Test]
    public async Task DeleteAsync_UnknownKey_ReturnsFalse()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        Assert.That(await storage.DeleteAsync(Guid.NewGuid().ToString("N")), Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("..")]
    [TestCase("../outside")]
    [TestCase("..\\outside")]
    public async Task DeleteAsync_InvalidKey_ReturnsFalse(string? key)
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        Assert.That(await storage.DeleteAsync(key!), Is.False);
    }

    [Test]
    public async Task DeleteAsync_PathTraversal_DoesNotDeleteFileOutsideRoot()
    {
        var storage = new LocalDiskAttachmentFileStorage(Path.Combine(_root, "skladiste"));
        var outside = Path.Combine(_root, "van-skladista.txt");
        await File.WriteAllTextAsync(outside, "ne sme biti obrisano");

        var deleted = await storage.DeleteAsync("../van-skladista.txt");

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.False);
            Assert.That(File.Exists(outside), Is.True);
        });
    }

    [Test]
    public void DeleteAsync_WhenCancelled_Throws()
    {
        var storage = new LocalDiskAttachmentFileStorage(_root);

        Assert.ThrowsAsync<OperationCanceledException>(
            () => storage.DeleteAsync(Guid.NewGuid().ToString("N"), new CancellationToken(canceled: true)));
    }
}
