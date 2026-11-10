using Application.Materials;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class AttachmentUploadPolicyTests
{
    private const long TwentyMegabytes = 20L * 1024 * 1024;

    [Test]
    public void Constructor_StoresAgreedExtensionsAndMaxSize()
    {
        var extensions = new[] { ".pdf", ".docx", ".pptx", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".zip" };

        var policy = new AttachmentUploadPolicy(extensions, TwentyMegabytes);

        Assert.Multiple(() =>
        {
            Assert.That(policy.AllowedExtensions, Is.EquivalentTo(extensions));
            Assert.That(policy.MaxSizeInBytes, Is.EqualTo(20_971_520));
        });
    }

    [Test]
    public void Constructor_NormalizesExtensionsToTrimmedLowerCase()
    {
        var policy = new AttachmentUploadPolicy(new[] { ".PDF", "  .Docx ", ".JpEg" }, TwentyMegabytes);

        Assert.That(policy.AllowedExtensions, Is.EquivalentTo(new[] { ".pdf", ".docx", ".jpeg" }));
    }

    [Test]
    public void Constructor_MergesDuplicateExtensions()
    {
        var policy = new AttachmentUploadPolicy(new[] { ".pdf", ".PDF", " .pdf " }, TwentyMegabytes);

        Assert.That(policy.AllowedExtensions, Is.EquivalentTo(new[] { ".pdf" }));
    }

    [Test]
    public void Constructor_AcceptsMinimalPositiveMaxSize()
    {
        var policy = new AttachmentUploadPolicy(new[] { ".pdf" }, 1);

        Assert.That(policy.MaxSizeInBytes, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_RejectsNullExtensionList()
    {
        Assert.Throws<ArgumentNullException>(() => new AttachmentUploadPolicy(null!, TwentyMegabytes));
    }

    [Test]
    public void Constructor_RejectsEmptyExtensionList()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new AttachmentUploadPolicy(Array.Empty<string>(), TwentyMegabytes));

        Assert.That(ex!.ParamName, Is.EqualTo("allowedExtensions"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_RejectsBlankExtension(string? extension)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new AttachmentUploadPolicy(new[] { ".pdf", extension! }, TwentyMegabytes));

        Assert.That(ex!.ParamName, Is.EqualTo("allowedExtensions"));
    }

    [TestCase("pdf")]
    [TestCase(".")]
    [TestCase(".p df")]
    public void Constructor_RejectsMalformedExtension(string extension)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new AttachmentUploadPolicy(new[] { extension }, TwentyMegabytes));

        Assert.That(ex!.ParamName, Is.EqualTo("allowedExtensions"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_RejectsNonPositiveMaxSize(long maxSize)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new AttachmentUploadPolicy(new[] { ".pdf" }, maxSize));

        Assert.That(ex!.ParamName, Is.EqualTo("maxSizeInBytes"));
    }
}
