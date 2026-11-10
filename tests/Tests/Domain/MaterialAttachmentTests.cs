using Domain.Materials;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class MaterialAttachmentTests
{
    private static readonly Guid AttachmentId = Guid.NewGuid();
    private static readonly Guid MaterialId = Guid.NewGuid();
    private const string StorageKey = "0123456789abcdef0123456789abcdef";

    // Pozitivno ponašanje

    [Test]
    public void Constructor_ValidData_StoresOriginalNameTypeAndSize()
    {
        var attachment = new MaterialAttachment(
            AttachmentId, MaterialId, "Predavanje 1.pdf", "application/pdf", 2048, StorageKey);

        Assert.Multiple(() =>
        {
            Assert.That(attachment.Id, Is.EqualTo(AttachmentId));
            Assert.That(attachment.MaterialId, Is.EqualTo(MaterialId));
            Assert.That(attachment.OriginalFileName, Is.EqualTo("Predavanje 1.pdf"));
            Assert.That(attachment.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(attachment.SizeInBytes, Is.EqualTo(2048));
            Assert.That(attachment.StorageKey, Is.EqualTo(StorageKey));
        });
    }

    [Test]
    public void Constructor_TrimsNameAndType()
    {
        var attachment = new MaterialAttachment(
            AttachmentId, MaterialId, "  vezbe.docx  ", " application/msword ", 10, StorageKey);

        Assert.That(attachment.OriginalFileName, Is.EqualTo("vezbe.docx"));
        Assert.That(attachment.ContentType, Is.EqualTo("application/msword"));
    }

    [Test]
    public void Constructor_KeepsUnicodeOriginalName()
    {
        var attachment = new MaterialAttachment(
            AttachmentId, MaterialId, "Uvod u čvorove – šema.pdf", "application/pdf", 1, StorageKey);

        Assert.That(attachment.OriginalFileName, Is.EqualTo("Uvod u čvorove – šema.pdf"));
    }

    // Granično ponašanje

    [Test]
    public void Constructor_ZeroSize_IsAllowed()
    {
        var attachment = new MaterialAttachment(
            AttachmentId, MaterialId, "prazno.txt", "text/plain", 0, StorageKey);

        Assert.That(attachment.SizeInBytes, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_SizeLargerThanInt32_IsStoredExactly()
    {
        const long size = (long)int.MaxValue + 1;

        var attachment = new MaterialAttachment(
            AttachmentId, MaterialId, "video.mp4", "video/mp4", size, StorageKey);

        Assert.That(attachment.SizeInBytes, Is.EqualTo(size));
    }

    // Negativno ponašanje

    [Test]
    public void Constructor_EmptyId_Throws()
    {
        Assert.That(
            () => new MaterialAttachment(Guid.Empty, MaterialId, "a.pdf", "application/pdf", 1, StorageKey),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("id"));
    }

    [Test]
    public void Constructor_EmptyMaterialId_Throws()
    {
        Assert.That(
            () => new MaterialAttachment(AttachmentId, Guid.Empty, "a.pdf", "application/pdf", 1, StorageKey),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("materialId"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_MissingOriginalFileName_Throws(string? name)
    {
        Assert.That(
            () => new MaterialAttachment(AttachmentId, MaterialId, name!, "application/pdf", 1, StorageKey),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("originalFileName"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_MissingContentType_Throws(string? contentType)
    {
        Assert.That(
            () => new MaterialAttachment(AttachmentId, MaterialId, "a.pdf", contentType!, 1, StorageKey),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("contentType"));
    }

    [TestCase(-1)]
    [TestCase(long.MinValue)]
    public void Constructor_NegativeSize_Throws(long size)
    {
        Assert.That(
            () => new MaterialAttachment(AttachmentId, MaterialId, "a.pdf", "application/pdf", size, StorageKey),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("sizeInBytes"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_MissingStorageKey_Throws(string? key)
    {
        Assert.That(
            () => new MaterialAttachment(AttachmentId, MaterialId, "a.pdf", "application/pdf", 1, key!),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("storageKey"));
    }
}
